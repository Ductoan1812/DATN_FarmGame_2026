# 🌾 FARMING SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** FarmingSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Cuốc đất
> 2. Gieo hạt
> 3. Tưới nước
> 4. Cây phát triển
> 5. Cây héo/chết
> 6. Thu hoạch
> 7. Chất lượng nông sản
> 8. Phân bón
> 9. Sprinkler (tự động tưới)
> 10. Đất chất lượng
> 11. Cây tái thu hoạch

---

## INPUT ROUTING (áp dụng cho toàn bộ FarmingSystem)

Tất cả tool farming dùng chung 1 luồng input:

```
Player nhấn Chuột Trái
    → PlayerControler.HandleActions() [Input.GetMouseButtonDown(0)]
        → playerEntity.TriggerEvent(new PrimaryActionEvent(playerEntity))
            → ActionRuntime.Handle(PrimaryActionEvent)
                → Tìm item đang cầm trên Hotbar (InventoryRuntime.SelectedEntity)
                → Forward PrimaryActionEvent sang item entity
                    → Item entity có ToolModule → ToolRuntime subclass nhận event
                        → ToolRuntime.Handle(PrimaryActionEvent)
                            → Validate() → nếu OK → ToolActionBridge.Request()
                                → Play animation → AnimStrikeEvent
                                    → Execute()
```

**Nhiều tool cùng dùng chuột trái** → phân biệt bằng: item nào đang cầm trên hotbar. Mỗi item có ToolModule với toolType khác nhau → tạo runtime khác nhau (HoeRuntime, WateringCanRuntime, etc.)

**Chuột phải / phím E** → SecondaryAction → dùng cho interact NPC, thu hoạch tay, cho vật nuôi ăn. Routing qua ActionRuntime → EntityScanSystem.GetClosest() → forward sang target.

---

## 1. CUỐC ĐẤT ✅ Đã có

### Mô tả
Người chơi trang bị cuốc (Hoe) trên hotbar, đứng trước ô đất trống (không phải cỏ, không có vật cản), nhấn chuột trái để cuốc. Ô đất chuyển thành đất đã cày (plowed), sẵn sàng gieo hạt.

### Điều kiện
- Player đang cầm item có ToolModule (toolType = Hoe)
- Ô trước mặt player (theo hướng nhìn) phải là tillable (TileRegistry)
- Ô đó không có entity nào chiếm (Plant, Furniture, Ground layer)
- Player có đủ stamina (≥ 4)
- ToolActionBridge không đang busy (animation trước chưa xong)

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → HoeRuntime.Handle()
    → HoeRuntime.Validate(actorGO, e):
        1. GridSystem.GetCellInFrontOf(actorGO) → targetCell
        2. WorldEntityService.HasBlockerAt(cell, Ground/Plant/Furniture) → phải false
        3. WorldEntityService.IsTillable(cell) → phải true
        4. TileData.plowedTile != null
        → Nếu tất cả OK → return true
    → ToolActionBridge.Request(actor, item, "Hoe") → play animation
    → Animation frame "Strike" → AnimStrikeEvent
    → HoeRuntime.Execute(actorGO, actor, item):
        1. WorldEntityService.SetGround(cell, plowedTile)
        2. Trừ stamina: actor.stats.Set(Stamina, current - 4)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Nhận input chuột trái |
| ActionRuntime | Route event tới item đang cầm |
| GridSystem | Tính ô trước mặt |
| WorldEntityService | Check tillable, set ground tile |
| SpatialEntityRegistry | Check blocker |
| TileRegistry | Track tile changes (save/load) |
| ToolActionBridge | Play animation, fire AnimStrikeEvent |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Hoe EntityData | ScriptableObject | ToolModule(toolType=Hoe, animTrigger="Hoe"), baseStats(Stamina cost) |
| TileData | ScriptableObject | plowedTile (TileBase reference) |

### UI
Không cần UI riêng. Feedback = tile visual thay đổi + animation player.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → HoeRuntime |
| AnimStrikeEvent | ToolActionBridge | HoeRuntime |

### Scene Objects
- Tilemap "Ground" — nơi tile thay đổi
- Player prefab (có PlayerControler, ToolActionBridge, EntityRoot)

---

## 2. GIEO HẠT ✅ Đã có

### Mô tả
Người chơi trang bị hạt giống (Seed) trên hotbar, đứng trước ô đất đã cày (plowed), nhấn chuột trái để gieo. Một cây con xuất hiện trên ô đó. Hạt giống bị trừ 1 từ inventory.

### Điều kiện
- Player đang cầm item có PlacementModule
- Ô trước mặt phải là plowed (đã cuốc)
- Ô đó không có plant entity nào
- Player có ≥ 1 seed trong stack
- Player có đủ stamina (≥ 1)

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → PlacementRuntime.Handle()
    → PlacementRuntime.Validate():
        1. GridSystem.GetCellInFrontOf(actorGO) → targetCell
        2. PlacementValidator.CanPlace(entityData, cell) → check rules
           - Ô phải có tag Plantable (plowed tile)
           - Không có entity trên layer Plant
        → Nếu OK → return true
    → ToolActionBridge.Request() → animation
    → AnimStrikeEvent → PlacementRuntime.Execute():
        1. EventBus.Publish(SpawnRequestPublish(worldPos, objectType, entityData))
        2. SpawnSystem nhận → Instantiate plant prefab → EntityRoot.Add()
        3. Plant entity nhận SpawnedEvent → StageRuntime init sprite stage 0
        4. Trừ 1 seed từ inventory (EntityService.AddAmount(seed, -1))
        5. Trừ stamina (1)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlacementValidator | Check placement rules |
| SpawnSystem | Instantiate plant prefab |
| WorldEntityService | Register spawn position |
| EntityService | Trừ seed amount |
| InventoryService | Quản lý hotbar slot |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Seed EntityData | ScriptableObject | PlacementModule(objectType=PlantXX, placedEntityData=PlantData) |
| Plant EntityData | ScriptableObject | StageModule(stages[]), HarvestModule, DropModule, HealthModule |
| Plant Prefab | GameObject | EntityRoot, SpriteRenderer, Collider2D |

### UI
Không. Feedback = cây con xuất hiện + seed count giảm trên hotbar.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → PlacementRuntime |
| AnimStrikeEvent | ToolActionBridge | PlacementRuntime |
| SpawnRequestPublish | PlacementRuntime | SpawnSystem |
| SpawnedEvent | SpawnSystem | StageRuntime (init sprite) |

### Scene Objects
- Plant prefab spawned trên tilemap
- Tilemap Ground (check plowed)

---

## 3. TƯỚI NƯỚC 🆕

### Mô tả
Người chơi trang bị bình tưới (WateringCan) trên hotbar, đứng trước ô đất đã cày hoặc có cây, nhấn chuột trái để tưới. Một tile "watered" được đặt lên **Tilemap Watered** (layer riêng, nằm trên Tilemap Ground). Ô đó được đánh dấu "watered" cho ngày hôm nay. Mỗi đầu ngày mới, toàn bộ Tilemap Watered bị clear (xóa hết tiles).

### Thiết kế Tilemap Layer

```
Tilemap Stack (từ dưới lên):
    ├── tmGround         ← đất cơ bản (grass, dirt, plowed)
    ├── tmWatered 🆕     ← CHỈ chứa wateredTile, nằm TRÊN ground
    ├── tmGroundDetail   ← chi tiết trang trí
    ├── tmCollision      ← collision
    ├── tmDecoration     ← decoration
    └── tmOverlay        ← overlay effects
```

**Tại sao dùng tilemap riêng?**
- Không cần sửa tile Ground khi tưới (plowed vẫn là plowed)
- Reset dễ: `tmWatered.ClearAllTiles()` — 1 dòng code, không cần track từng cell
- Visual rõ ràng: watered tile = overlay đậm hơn trên plowed
- Không ảnh hưởng TileRegistry/save của ground tiles

### Điều kiện
- Player đang cầm item có ToolModule (toolType = WateringCan)
- Ô trước mặt phải là plowed (ground tile = plowedTile) HOẶC có plant entity
- Ô đó chưa có tile trên tmWatered (chưa tưới hôm nay)
- Player có đủ stamina (≥ 2)
- ToolActionBridge không busy

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → WateringCanRuntime.Handle()
    → WateringCanRuntime.Validate(actorGO, e):
        1. GridSystem.GetCellInFrontOf(actorGO) → targetCell
        2. Check: tmGround tại cell là plowedTile? HOẶC có plant entity tại cell?
           → Dùng WorldEntityService hoặc TileRegistry check
        3. Check: tmWatered.GetTile(cell) == null? (chưa tưới)
           → Nếu đã có tile = đã tưới → return false
        → Nếu OK → return true
    → ToolActionBridge.Request() → animation "WateringCan"
    → AnimStrikeEvent → WateringCanRuntime.Execute():
        1. WateredTileTracker.SetWatered(cell)
           → Bên trong: tmWatered.SetTile(cell, wateredTile)
        2. Trừ stamina: actor.stats.Set(Stamina, current - 2)
```

### WateredTileTracker — Thiết kế mới (dùng Tilemap riêng)

```csharp
// WateredTileTracker KHÔNG cần HashSet nữa
// Thay vào đó dùng trực tiếp Tilemap tmWatered làm source of truth

public class WateredTileTracker
{
    private Tilemap _tmWatered;      // reference từ GameManager
    private TileBase _wateredTile;   // tile asset dùng để đặt

    // Tưới 1 ô
    SetWatered(Vector2Int cell) → void
        → _tmWatered.SetTile(new Vector3Int(cell.x, cell.y, 0), _wateredTile)

    // Check ô đã tưới chưa
    IsWatered(Vector2Int cell) → bool
        → return _tmWatered.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null

    // Reset toàn bộ (gọi mỗi đầu ngày mới)
    ResetAll() → void
        → _tmWatered.ClearAllTiles()

    // Tưới tất cả ô outdoor (khi mưa)
    WaterAllPlowedCells() → void
        → Lấy tất cả cells từ tmGround có tile == plowedTile
        → Foreach cell: SetWatered(cell)

    // Đếm (cho AI Assistant)
    GetWateredCount() → int
        → Dùng tmWatered.GetUsedTilesCount() hoặc BoundsInt iteration
}
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| WateredTileTracker 🆕 | Quản lý tmWatered tilemap |
| GridSystem | Tính ô trước mặt |
| WorldEntityService | Check ground tile (plowed?) |
| TileData | wateredTile reference |
| TimeManager | DayChangedPublish → ResetAll() |
| Tilemap tmWatered 🆕 | Layer riêng chứa watered tiles |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| WateringCan EntityData | ScriptableObject | ToolModule(toolType=WateringCan, animTrigger="WateringCan") |
| TileData | ScriptableObject | 🔧 thêm field: `wateredTile` (TileBase — sprite đất ướt/đậm) |

### UI
Không cần UI riêng. Feedback = tile watered xuất hiện trên tmWatered (visual đậm hơn).

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → WateringCanRuntime |
| AnimStrikeEvent | ToolActionBridge | WateringCanRuntime |
| DayChangedPublish | TimeManager | WateredTileTracker.ResetAll() |
| WeatherChangedPublish | WeatherSystem | WateredTileTracker.WaterAllPlowedCells() (nếu mưa) |

### Scene Objects
- Tilemap "tmWatered" 🆕 (GameObject con của Grid, sorting order giữa Ground và GroundDetail)
- Player prefab (không thay đổi)

### Mối liên hệ với chức năng khác
- **Cây phát triển (4):** StageRuntime gọi `WateredTileTracker.IsWatered(cell)` để check grow
- **Cây héo (5):** Nếu `IsWatered(cell) == false` → daysWithoutWater++
- **Sprinkler (9):** SprinklerRuntime gọi `WateredTileTracker.SetWatered(cells[])` mỗi NextDay
- **Weather mưa:** WeatherSystem gọi `WateredTileTracker.WaterAllPlowedCells()`
- **AI Assistant:** Đọc `tmWatered.GetUsedTilesCount()` để nhắc "còn X cây chưa tưới"
- **Save/Load:** KHÔNG CẦN save watered state — vì reset mỗi ngày, save chỉ xảy ra khi ngủ (sau reset)

---

## FILES CẦN ĐỤNG VÀO (Sprint 1 - Task Tưới nước)

```
SỬA:
├── Systems/GameManager.cs
│   └── Thêm: [SerializeField] private Tilemap tmWatered;
│   └── Thêm: public Tilemap TmWatered => tmWatered;
│   └── Thêm: khởi tạo WateredTileTracker trong Awake/Init
│
├── Data/Structs/TileData.cs
│   └── Thêm: public TileBase wateredTile;
│
├── Data/Runtime/StageRuntime.cs
│   └── Sửa Handle(NextDayEvent): check WateredTileTracker.IsWatered(cell)
│
├── Core/SystemEvents.cs
│   └── (Không cần sửa nếu dùng DayChangedPublish đã có)

TẠO MỚI:
├── Core/Service/WateredTileTracker.cs
│   └── Class mới: SetWatered, IsWatered, ResetAll, WaterAllPlowedCells
│
├── Data/Runtime/WateringCanRuntime.cs
│   └── Extends ToolRuntime: Validate + Execute

SCENE (Unity Editor):
├── FarmScene
│   └── Grid/
│       ├── tmGround (đã có)
│       ├── tmWatered 🆕 (tạo Tilemap mới, sorting order = Ground + 1)
│       ├── tmGroundDetail (đã có)
│       └── ...
│
├── GameManager Inspector
│   └── Gán reference tmWatered vào field mới

ASSET:
├── TileData ScriptableObject
│   └── Gán wateredTile (tạo tile asset mới — sprite đất ướt)
│
├── WateringCan EntityData (ScriptableObject mới)
│   └── ToolModule(toolType=WateringCan, animTrigger="WateringCan")
│   └── baseStats: (không cần stats đặc biệt)
```

### Sơ đồ thay đổi

```
TRƯỚC (hiện tại):
    Chuột Trái + Tool → ToolRuntime → chỉ có Hoe/Scythe/Axe/Pickaxe
    Tilemap: tmGround (plowed tile thay đổi trực tiếp)
    Không có watered tracking

SAU (sau Sprint 1.1-1.4):
    Chuột Trái + WateringCan → WateringCanRuntime (MỚI)
        → WateredTileTracker.SetWatered(cell)
            → tmWatered.SetTile(cell, wateredTile)  ← TILEMAP RIÊNG
    
    Mỗi NextDay:
        → WateredTileTracker.ResetAll()
            → tmWatered.ClearAllTiles()  ← 1 DÒNG, SẠCH SẼ
    
    StageRuntime.Handle(NextDayEvent):
        → WateredTileTracker.IsWatered(myCell)?
            → YES: grow
            → NO: wilt counter++
```

---

## 4. CÂY PHÁT TRIỂN 🔧

### Mô tả
Mỗi đầu ngày mới, cây đã được tưới trong ngày hôm trước sẽ phát triển lên stage tiếp theo. Cây không được tưới sẽ không lớn. Mỗi stage có sprite riêng. Stage cuối = sẵn sàng thu hoạch.

### Điều kiện
- Cây entity tồn tại trong world
- Ngày mới bắt đầu (DayChangedPublish)
- Ô cây đã được tưới trong ngày hôm trước (WateredTileTracker)

### Flow
```
TimeManager → DayChangedPublish → StageObject nhận
    → Forward NextDayEvent tới plant EntityRuntime
        → StageRuntime.Handle(NextDayEvent):
            1. Lấy cell position của entity (Owner.GameObject.transform)
            2. Check WateredTileTracker.IsWatered(cell)
                → YES (đã tưới):
                    - daysWithoutWater = 0
                    - daysInCurrentStage++
                    - Check fertilized? → bonus: daysInCurrentStage += 1 (grow x2)
                    - if daysInCurrentStage >= stage.daysToGrow:
                        - currentStageIndex++
                        - daysInCurrentStage = 0
                        - Đổi sprite → stages[currentStageIndex].sprite
                → NO (không tưới):
                    - Xem chức năng 5 (Cây héo)

SAU KHI tất cả entity xử lý NextDayEvent:
    → WateredTileTracker.ResetAll()
    → Tất cả watered tiles revert visual về plowed
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| TimeManager | Publish DayChangedPublish |
| WateredTileTracker | Check ô đã tưới |
| StageRuntime | Logic grow |
| SoilQualityTracker | Bonus grow speed (optional) |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| StageModule | IModuleData | stages[]: {sprite, daysToGrow, canHarvest} |

### UI
Không. Feedback = sprite cây thay đổi mỗi sáng.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DayChangedPublish | TimeManager | StageObject → NextDayEvent |
| NextDayEvent | StageObject | StageRuntime |

### Scene Objects
- Plant entities (sprite thay đổi)

---

## 5. CÂY HÉO/CHẾT 🆕

### Mô tả
Nếu cây không được tưới trong 1 ngày, cây hiển thị trạng thái "héo" (sprite cảnh báo). Nếu không tưới 2 ngày liên tiếp, cây chết và biến mất. Người chơi mất vụ.

### Điều kiện
- Cây entity tồn tại
- Ngày mới + ô KHÔNG được tưới hôm trước

### Flow
```
StageRuntime.Handle(NextDayEvent):
    → Check WateredTileTracker.IsWatered(cell)
        → NO:
            - daysWithoutWater++
            - if daysWithoutWater == 1:
                → Đổi sprite → wiltSprite (cây héo, vàng úa)
                → Debug.Log("Cây đang héo, cần tưới!")
            - if daysWithoutWater >= 2:
                → entity.TriggerEvent(new DieEvent(entity))
                → MortalRuntime → DestroyEntityRequestPublish
                → Cây biến mất khỏi world
                → Player mất vụ (không drop gì)
        → YES:
            - daysWithoutWater = 0
            - Nếu đang hiển thị wiltSprite → revert về sprite stage hiện tại
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| WateredTileTracker | Check tưới |
| StageRuntime | Logic héo |
| MortalRuntime | Destroy khi chết |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| StageModule | 🔧 thêm | wiltSprite (Sprite) |

### UI
Không. Feedback = sprite héo (visual warning rõ ràng).

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| NextDayEvent | StageObject | StageRuntime |
| DieEvent | StageRuntime | MortalRuntime |
| DestroyEntityRequestPublish | MortalRuntime | SpawnSystem |

---

## 6. THU HOẠCH 🔧

### Mô tả
Khi cây đạt stage cuối (canHarvest = true), người chơi có thể thu hoạch bằng tool phù hợp (liềm) hoặc bằng tay (nhấn E/chuột phải). Cây drop nông sản vào world, player nhặt vào inventory. Cây có thể tái thu hoạch hoặc biến mất.

### Điều kiện
- Cây ở stage có canHarvest = true
- Nếu harvestTool != None → player phải dùng đúng tool (chuột trái + tool)
- Nếu harvestTool == None → player dùng tay (chuột phải/E gần cây)
- Player có đủ stamina (≥ 2)

### Flow — Thu hoạch bằng tool (chuột trái)
```
Chuột Trái + Scythe → DamageToolRuntime → TakeDamageEvent tới plant
    → HarvestRuntime.Handle(TakeDamageEvent):
        1. Check harvestTool == e.toolType? → YES
        2. Check IsHarvestable() (stage.canHarvest)? → YES
        3. Set HealthRuntime.CanTakeDamage = true
    → HealthRuntime nhận TakeDamageEvent → trừ HP → HP <= 0
    → DieEvent triggered
    → DropRuntime.Handle(DieEvent):
        - Tính quality (QualityRuntime)
        - Spawn drop items (nông sản) với quality tag
    → ExpRewardRuntime.Handle(DieEvent):
        - ProgressionService.GrantMasteryExp(killer, amount, Harvest)
    → IF regrowable:
        - KHÔNG trigger MortalRuntime
        - StageRuntime reset: currentStage = regrowToStage, days = 0
        - Sprite đổi về stage thấp
    → IF NOT regrowable:
        - MortalRuntime → DestroyEntityRequestPublish → cây biến mất
```

### Flow — Thu hoạch bằng tay (chuột phải/E)
```
Chuột Phải/E → SecondaryActionEvent → ActionRuntime
    → EntityScanSystem.GetClosest() → tìm plant entity gần nhất
    → Forward SecondaryActionEvent sang plant
    → HarvestRuntime.Handle(SecondaryActionEvent):
        1. Check harvestTool == None? → YES (hái tay)
        2. Check IsHarvestable()? → YES
        3. entity.TriggerEvent(new DieEvent(entity, initiator))
    → (Tiếp tục giống flow trên từ DieEvent)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| HarvestRuntime | Gate harvest bằng tool + stage |
| HealthRuntime | HP system (cho tool harvest) |
| DropRuntime | Spawn drops |
| QualityRuntime 🆕 | Tính quality nông sản |
| ExpRewardRuntime | Grant Mastery EXP |
| MortalRuntime | Destroy nếu không regrow |
| StageRuntime | Reset nếu regrow |
| ProgressionService | Nhận EXP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Plant EntityData | ScriptableObject | HarvestModule(harvestTool), DropModule(drops[]), StageModule(regrowable) |
| Crop EntityData | ScriptableObject | Nông sản drop (sellPrice, category=Crop) |

### UI
- Popup nhỏ "+15 Mastery" (fade out) khi thu hoạch
- Drop item hiển thị trên ground (DropMotionObject)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| TakeDamageEvent | DamageToolRuntime | HarvestRuntime, HealthRuntime |
| SecondaryActionEvent | ActionRuntime | HarvestRuntime |
| DieEvent | HealthRuntime/HarvestRuntime | DropRuntime, ExpRewardRuntime, MortalRuntime |
| SpawnRequestPublish | DropRuntime | SpawnSystem (spawn drop items) |

---

## 7-11: (Tiếp tục trong phần sau)

> File này đã dài. Các chức năng 7-11 (Quality, Phân bón, Sprinkler, Đất chất lượng, Tái thu hoạch) sẽ được viết tiếp khi bạn confirm format này OK.

---

## TỔNG KẾT: FARMING SYSTEM DEPENDENCY

```
PlayerControler (Input)
    │
    ▼
ActionRuntime (Route theo item đang cầm)
    │
    ├── HoeRuntime ──────────→ WorldEntityService, TileRegistry
    ├── WateringCanRuntime ──→ WateredTileTracker, WorldEntityService
    ├── PlacementRuntime ────→ SpawnSystem, PlacementValidator
    ├── FertilizerRuntime ───→ StageRuntime (set fertilized)
    └── DamageToolRuntime ───→ HealthRuntime → HarvestRuntime
                                                    │
                                                    ▼
                                              DropRuntime → InventoryService (pickup)
                                              ExpRewardRuntime → ProgressionService
                                              StageRuntime (regrow) hoặc MortalRuntime (destroy)

TimeManager
    │
    ▼
DayChangedPublish
    │
    ├── StageRuntime ←── đọc WateredTileTracker
    ├── WateredTileTracker.ResetAll()
    ├── WeatherSystem → WateredTileTracker (nếu mưa)
    └── SprinklerRuntime → WateredTileTracker (auto-water)
```
