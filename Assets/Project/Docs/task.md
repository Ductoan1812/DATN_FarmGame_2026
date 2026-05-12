Tôi đã đọc xong toàn bộ hệ thống. Dưới đây là phân tích đầy đủ **tất cả các vấn đề** bạn đang gặp và cần giải quyết:

---

## TỔNG QUAN CÁC VẤN ĐỀ

### Vấn đề 1: Cầu nối Entity ↔ HeroEditor4D Sprite

**Hiện trạng:**
- Hệ thống của bạn: `EntityData` (ScriptableObject) → `EntityRuntime` → `EquipModule` (chứa `EquipSlot`) → `EquipmentRuntime` (container trang bị)
- HeroEditor4D: `SpriteCollection` → `ItemSprite` (chứa `Id` string + `List<Sprite>`) → `Character4D.Equip(ItemSprite, EquipmentPart)`

**Hai hệ thống hoàn toàn tách biệt.** EntityData dùng `EquipSlot` enum (Hand, Head, Armor, Body, Legs, Boots, Accessory), còn HeroEditor4D dùng `EquipmentPart` enum (Helmet, Armor, Vest, Bracers, Leggings, MeleeWeapon1H...).

**Giải pháp — Thêm mapping data vào EntityData:**

Bạn cần 1 module mới hoặc 1 field mới trên `EquipModule` để lưu **sprite ID của HeroEditor4D**:

```csharp
// Thêm vào EquipModule.cs
[System.Serializable]
public class EquipModule : IModuleData
{
    public EquipSlot equipSlot;
    
    [Header("HeroEditor4D Visual")]
    public string heroEditorSpriteId;           // VD: "FantasyHeroes.Basic.Armor.BanditArmor"
    public EquipmentPart heroEditorEquipPart;   // VD: EquipmentPart.Armor
    
    public override IModuleRuntime CreateRuntime()
    {
        return new EquipRuntime(this);
    }
}
```

Rồi trong `PlayerEquipment.cs`, khi equip thành công → gọi `Character4D.Equip()`:

```csharp
// Sau khi equip entity vào EquipmentRuntime:
var equipModule = entity.GetModule<EquipRuntime>();
var spriteId = equipModule.HeroEditorSpriteId;
var part = equipModule.HeroEditorEquipPart;

if (!string.IsNullOrEmpty(spriteId))
{
    var spriteItem = character4D.SpriteCollection.GetAllSprites()
        .Find(s => s.Id == spriteId);
    character4D.Equip(spriteItem, part);
}
```

**Mapping EquipSlot → EquipmentPart:**

| EquipSlot (của bạn) | EquipmentPart (HeroEditor4D) |
|---------------------|------------------------------|
| Hand | MeleeWeapon1H / MeleeWeapon2H / Bow... (tùy item) |
| Head | Helmet |
| Armor | Armor |
| Body | Vest |
| Legs | Leggings |
| Boots | Leggings (HeroEditor4D không có Boots riêng) |
| Accessory | Earrings / Mask / Wings... |

---

### Vấn đề 2: Item không phù hợp game nông trại

**Hiện trạng:** HeroEditor4D chủ yếu có sprite RPG chiến đấu (kiếm, giáp, cung...). Game nông trại cần: cuốc, liềm, bình tưới, cần câu, rìu, mũ nông dân, quần áo thường...

**Giải pháp:**

1. **Công cụ nông trại (Hoe, WateringCan, Scythe, Axe, FishingRod):** Dùng slot `PrimaryWeapon` của HeroEditor4D. Bạn cần **vẽ/import sprite** công cụ nông trại theo format của HeroEditor4D (1 sprite cho mỗi hướng: Front, Back, Side). Rồi thêm vào `SpriteCollection` → `MeleeWeapon1H`.

2. **Trang phục nông dân:** Tương tự — vẽ sprite theo format rồi thêm vào SpriteCollection → Armor.

3. **Tạm thời:** Dùng sprite vũ khí có sẵn làm placeholder. Ví dụ: cuốc = dùng sprite rìu, liềm = dùng sprite kiếm cong.

4. **Không cần hiển thị visual:** Nhiều item nông trại (hạt giống, nông sản, đồ ăn) không cần hiển thị trên nhân vật → không cần mapping HeroEditor4D, chỉ cần icon trong inventory.

---

### Vấn đề 3: Animation riêng cho nông trại

**Hiện trạng:** HeroEditor4D có: Idle, Walk, Run, Slash1H, Slash2H, ShotBow, Jab, Die, Hit, Block, Climb, Jump, Dance. **Không có:** Cuốc đất, Tưới nước, Gieo hạt, Thu hoạch, Câu cá.

**Giải pháp — 2 cách tiếp cận:**

**Cách A: Tái sử dụng animation có sẵn (nhanh, tạm thời)**
| Hành động nông trại | Dùng animation có sẵn |
|---------------------|----------------------|
| Cuốc đất | `Slash2H` (chém 2 tay — giống động tác cuốc) |
| Liềm/Thu hoạch | `Slash1H` (chém 1 tay) |
| Tưới nước | `Jab` (đâm — giống động tác nghiêng bình) |
| Gieo hạt | `Throw` (ném) |
| Câu cá | `ShotBow` (kéo cung — giống kéo cần) |
| Rìu chặt cây | `Slash2H` |

**Cách B: Tạo animation mới (đúng chuẩn, lâu dài)**
1. Duplicate `.anim` gần nhất (ví dụ `Slash2H.anim` → `HoeU.anim`)
2. Chỉnh keyframe trong Animation window
3. Thêm state mới vào `Controller.controller`
4. Thêm trigger mới vào Animator
5. Thêm method vào `AnimationManager.cs`

---

### Vấn đề 4: Trigger animation đúng theo hành động

**Hiện trạng:** `PlayerControler` chỉ có di chuyển. Khi click chuột trái → `PrimaryActionEvent` → `ActionRuntime` → forward sang `ToolRuntime` (HoeRuntime, ScytheRuntime) → thực hiện logic. **Nhưng không ai báo cho Character4D phát animation.**

Luồng hiện tại:
```
Click chuột → PlayerControler.HandleActions()
  → entity.TriggerEvent(PrimaryActionEvent)
    → ActionRuntime.Handle() → tìm item đang cầm
      → item.TriggerEvent(PrimaryActionEvent(actor, item))
        → HoeRuntime.Execute() → cuốc đất (logic)
        → ScytheRuntime.Execute() → gây damage (logic)
        
// ❌ Không có bước nào gọi character4D.AnimationManager.Slash1H()
```

**Giải pháp — Thêm animation callback vào luồng action:**

Có 2 cách:

**Cách A: PlayerControler lắng nghe kết quả action (đơn giản)**

```csharp
// Trong PlayerControler.cs, sau khi trigger PrimaryAction:
if (Input.GetMouseButtonDown(0))
{
    // Xác định animation TRƯỚC khi trigger logic
    var handItem = GetComponent<PlayerEquipment>()?.GetHandItem();
    var toolModule = handItem?.entityData?.modules
        .Find(m => m is ToolModule) as ToolModule;
    
    if (toolModule != null && character4D != null)
    {
        switch (toolModule.toolType)
        {
            case ToolType.Hoe:
            case ToolType.Axe:
            case ToolType.Pickaxe:
                character4D.AnimationManager.Slash2H(); // Cuốc/rìu = chém 2 tay
                break;
            case ToolType.Scythe:
                character4D.AnimationManager.Slash1H(); // Liềm = chém 1 tay
                break;
            case ToolType.WateringCan:
                character4D.AnimationManager.Jab();     // Tưới = đâm
                break;
            case ToolType.FishingRod:
                character4D.AnimationManager.ShotBow(); // Câu = kéo cung
                break;
        }
    }
    
    playerEntity.TriggerEvent(new PrimaryActionEvent(playerEntity));
}
```

**Cách B: Dùng AnimationEvents để trigger logic SAU animation (chính xác hơn)**

Thay vì thực hiện logic ngay khi click, đợi animation chạy đến frame "Hit" rồi mới thực hiện:

```csharp
// 1. Click → phát animation
// 2. Animation chạy → đến frame "Hit" → AnimationEvents.OnEvent("Hit")
// 3. Lúc đó mới trigger PrimaryActionEvent

// Trong PlayerControler:
private bool _pendingAction;

void Start()
{
    character4D.GetComponent<AnimationEvents>().OnEvent += OnAnimEvent;
}

void OnAnimEvent(string name)
{
    if (name == "Hit" && _pendingAction)
    {
        _pendingAction = false;
        _entityRoot.GetEntity().TriggerEvent(new PrimaryActionEvent(...));
    }
}
```

---

### Vấn đề 5 (bạn chưa nêu): Hướng nhân vật ↔ Hướng hành động

**Hiện trạng:** `GridSystem.GetCellInFrontOf(actorGO)` dùng `PlayerControler.LastMoveDirection` để xác định ô trước mặt. Nhưng `Character4D.Direction` là riêng biệt.

**Vấn đề:** Nếu player đứng yên rồi click chuột, `LastMoveDirection` có thể không khớp với hướng Character4D đang hiển thị. Hoặc player di chuyển chéo nhưng Character4D chỉ hiển thị 4 hướng chính.

**Giải pháp:** Đồng bộ `LastMoveDirection` với `Character4D.Direction`:
```csharp
// Trong PlayerControler, thêm property:
public Vector2 FacingDirection => character4D != null ? character4D.Direction : 
    new Vector2(lastMoveDirection.x, lastMoveDirection.y);
```

---

### Vấn đề 6 (bạn chưa nêu): Khóa input khi đang animation action

**Hiện trạng:** Player có thể di chuyển trong khi đang chém/cuốc. Animation action bị ngắt giữa chừng.

**Giải pháp:**
```csharp
// Trong PlayerControler.HandleMovement():
if (character4D != null && character4D.AnimationManager.IsAction)
    return; // Không cho di chuyển khi đang action
```

---

### Vấn đề 7 (bạn chưa nêu): Scale và Collider không khớp

**Hiện trạng:** Human prefab có bounds ~7x6 units, Player cũ có bounds ~1.3x1.3 units. CapsuleCollider2D trên Human (offset 0,1 size 2,2.5) sẽ xung đột với BoxCollider2D trên Player.

**Giải pháp:**
1. Scale Human child xuống (khoảng 0.2-0.25)
2. Disable CapsuleCollider2D trên Human (giữ BoxCollider2D trên Player root)
3. Hoặc xóa BoxCollider2D trên Player, dùng CapsuleCollider2D của Human (nhưng cần adjust size)

---

### Vấn đề 8 (bạn chưa nêu): SpriteRenderer cũ trên Player

**Hiện trạng:** Player root vẫn còn `SpriteRenderer` cũ (sprite Vampire) + `Animator` cũ (Player.controller). Chúng sẽ xung đột với Human child.

**Giải pháp:** Xóa `SpriteRenderer` và `Animator` cũ trên Player root. Chỉ giữ Human child làm visual.

---

### Vấn đề 9 (bạn chưa nêu): Save/Load ngoại hình

**Hiện trạng:** `CharacterAppearance` của HeroEditor4D có `ToJson()`/`FromJson()`. Nhưng hệ thống save/load của bạn (`EntitySaveData`, `ModuleSaveData`) không biết về ngoại hình nhân vật.

**Giải pháp:** Thêm module mới `AppearanceModule` lưu JSON ngoại hình:
```csharp
public class AppearanceRuntime : IModuleRuntime
{
    private string _appearanceJson;
    
    public ModuleSaveData ToSaveData() => new ModuleSaveData 
    { 
        moduleType = "Appearance", 
        dataJson = _appearanceJson 
    };
}
```

---

### Vấn đề 10 (bạn chưa nêu): Sorting Order khi di chuyển

**Hiện trạng:** Trong game top-down 2D, nhân vật đứng phía trên (y cao hơn) phải render phía sau nhân vật đứng phía dưới (y thấp hơn).

**Giải pháp:** Cập nhật `SortingGroup.sortingOrder` theo position.y:
```csharp
// Thêm script trên Player:
void LateUpdate()
{
    sortingGroup.sortingOrder = -(int)(transform.position.y * 100);
}
```

---

### Vấn đề 11 (bạn chưa nêu): WeaponType không phù hợp nông trại

**Hiện trạng:** HeroEditor4D có `WeaponType`: Melee1H, Melee2H, Bow, Crossbow, Firearm... Animation phụ thuộc vào WeaponType. Nhưng `ToolType` của bạn (Hoe, WateringCan, Scythe...) không map 1:1.

**Giải pháp — Mapping:**

| ToolType (của bạn) | WeaponType (HeroEditor4D) | Lý do |
|--------------------|-----------------------------|-------|
| Hoe | Melee2H | Cuốc cầm 2 tay |
| Axe | Melee2H | Rìu cầm 2 tay |
| Pickaxe | Melee2H | Cuốc chim 2 tay |
| Scythe | Melee1H | Liềm cầm 1 tay |
| WateringCan | Melee1H | Bình tưới 1 tay |
| FishingRod | Melee2H | Cần câu 2 tay |
| None (tay không) | Melee1H | Mặc định |

Khi equip tool → set `character4D.AnimationManager.SetWeaponType(mappedType)`.

---

## TÓM TẮT DANH SÁCH VIỆC CẦN LÀM

| # | Vấn đề | Độ ưu tiên | Độ khó |
|---|--------|-----------|--------|
| 1 | Cầu nối Entity ↔ HeroEditor4D (EquipModule + mapping) | 🔴 Cao | Trung bình |
| 2 | Sprite nông trại (vẽ/import) | 🟡 Trung bình | Cao (cần artist) |
| 3 | Animation nông trại (tạm dùng có sẵn → sau tạo mới) | 🟡 Trung bình | Trung bình |
| 4 | Trigger animation đúng hành động | 🔴 Cao | Thấp |
| 5 | Đồng bộ hướng nhân vật ↔ hướng action | 🔴 Cao | Thấp |
| 6 | Khóa input khi đang action | 🔴 Cao | Thấp |
| 7 | Scale + Collider | 🔴 Cao | Thấp |
| 8 | Xóa SpriteRenderer/Animator cũ | 🔴 Cao | Thấp |
| 9 | Save/Load ngoại hình | 🟢 Thấp | Trung bình |
| 10 | Sorting Order theo Y | 🟡 Trung bình | Thấp |
| 11 | Mapping ToolType → WeaponType | 🔴 Cao | Thấp |

Bạn muốn tôi bắt tay implement vấn đề nào trước?