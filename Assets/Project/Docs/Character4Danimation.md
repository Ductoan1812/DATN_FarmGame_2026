Đây là tổng quan cách HeroEditor4D quản lý animation:

---

## Kiến trúc Animation của HeroEditor4D

### 1. Cấu trúc nhân vật (4 hướng)

```
Player
└── Human
    ├── Front (Character)    ← hướng xuống
    ├── Back  (Character)    ← hướng lên
    ├── Left  (Character)    ← hướng trái
    └── Right (Character)    ← hướng phải
```

- **`Character4D`** — controller tổng, quản lý 4 `Character` (Front/Back/Left/Right)
- **`Character`** — 1 hướng, chứa tất cả `SpriteRenderer` cho body parts + equipment
- Khi đổi hướng → `SetDirection()` → ẩn 3 hướng, hiện 1 hướng đúng

### 2. Animation System

| Thành phần | Vai trò |
|---|---|
| **`Animator`** (Unity built-in) | Chạy state machine: Idle, Walk, Run, Jump, Death, Attack... |
| **`AnimationManager`** | Wrapper gọi `Animator.SetTrigger/SetInteger` — expose API đơn giản: `Attack()`, `Die()`, `Hit()`, `SetState()` |
| **`CharacterState` enum** | Idle=0, Ready=1, Walk=2, Run=3, Jump=4, Climb=5, Death=9... |
| **`WeaponType` enum** | Melee1H, Melee2H, Bow, Crossbow, Firearm1H... → quyết định animation attack nào được play |

### 3. Cách animation hoạt động

**Animation KHÔNG thay đổi sprite trực tiếp.** Thay vào đó:

1. **Animator** điều khiển **bone transforms** (vị trí tay, chân, thân...) qua animation clips
2. Mỗi `SpriteRenderer` trên bone hiển thị sprite đã được gán bởi `Character.Initialize()`
3. **`SpriteMapping`** trên mỗi renderer map tên sprite → đúng sprite trong `ItemSprite.Sprites` list

**Ví dụ flow khi equip Armor:**
```
Equip("AdvancedWizardRobe", EquipmentPart.Armor)
  → Character.Armor = itemSprite.Sprites (list sprite: Body, ArmL, ArmR, LegL, LegR...)
  → Character.Initialize()
    → MapSprites(ArmorRenderers, Armor)
      → Mỗi ArmorRenderer có SpriteMapping (SpriteName = "Body", "ArmL"...)
      → FindSprite() tìm sprite có tên match trong list
      → Gán sprite vào renderer
```

### 4. Tóm tắt flow

```
┌─────────────────────────────────────────────────────────┐
│  INPUT (game logic)                                     │
│  AnimationManager.SetState(Walk) / Attack() / Die()     │
└──────────────────────┬──────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────┐
│  ANIMATOR (Unity)                                       │
│  State machine: Idle → Walk → Attack → ...              │
│  Animation clips di chuyển bones (Transform)            │
│  WeaponType param → chọn attack animation phù hợp      │
└──────────────────────┬──────────────────────────────────┘
                       ▼
┌─────────────────────────────────────────────────────────┐
│  SPRITE RENDERERS (trên bones)                          │
│  Sprite đã được gán sẵn bởi Character.Initialize()     │
│  Bones di chuyển → sprite di chuyển theo → animation    │
└─────────────────────────────────────────────────────────┘
```

**Kết luận:** HeroEditor4D dùng **bone-based animation** (Animator điều khiển Transform) + **sprite swapping** (thay sprite trên renderer khi equip). Animation clips không cần biết item nào đang equip — chúng chỉ di chuyển bones, còn sprite nào hiển thị là do `Character.Initialize()` quyết định.