# 🐔 ANIMAL SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** AnimalSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Cho gà ăn
> 2. Thu sản phẩm gà
> 3. Bò (cho ăn + thu sữa)
> 4. Cừu (cho ăn + thu lông)
> 5. Xây chuồng
> 6. Động vật bệnh/chết

---

## INPUT ROUTING (áp dụng cho toàn bộ AnimalSystem)

Tất cả tương tác vật nuôi dùng SecondaryAction:

```
Player nhấn E / Chuột Phải
    → PlayerControler.HandleActions()
        → playerEntity.TriggerEvent(new SecondaryActionEvent(playerEntity))
            → ActionRuntime.Handle(SecondaryActionEvent)
                → EntityScanSystem.GetClosest(actorGO, 1f) → tìm animal entity
                → Forward SecondaryActionEvent sang animal
                    → AnimalRuntime nhận event
                        → Check state + held item → thực hiện action phù hợp
```

**Phân biệt hành động:** dựa vào state hiện tại của animal (Hungry/Fed/ProductReady) + item player đang cầm (feedItem hoặc tay không).

---

## 1. CHO GÀ ĂN

### Mô tả
Người chơi cầm thức ăn (feedItem) trên hotbar, đứng gần gà (Chicken entity), nhấn E/chuột phải. Gà chuyển từ state Hungry → Fed. Thức ăn bị trừ 1 từ inventory.

### Điều kiện
- Player đang cầm item có tag FeedItem (hoặc feedItemId khớp với animal yêu cầu)
- Animal entity ở state Hungry
- Player đứng trong range interact (≤ 1f)
- Animal thuộc chuồng đã xây (có pen entity)

### Flow
```
E / Chuột Phải → SecondaryActionEvent → ActionRuntime
    → EntityScanSystem.GetClosest() → tìm Chicken entity
    → Forward SecondaryActionEvent sang Chicken
    → AnimalRuntime.Handle(SecondaryActionEvent):
        1. Check state == Hungry? → YES
        2. Check context.initiator hotbar có feedItem? → YES
        3. EntityService.AddAmount(feedItem, -1) → trừ thức ăn
        4. SetState(AnimalState.Fed)
        5. Start timer: sau X giờ game → SetState(ProductReady)
        6. Play animation ăn (optional)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Nhận input E/chuột phải |
| ActionRuntime | Route SecondaryAction tới target |
| EntityScanSystem | Tìm animal gần nhất |
| AnimalRuntime | State machine (Hungry→Fed→ProductReady) |
| EntityService | Trừ feedItem |
| InventoryService | Check hotbar item |
| TimeManager | GameHourChangedPublish → timer product ready |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Chicken EntityData | ScriptableObject | AnimalModule(animalType=Chicken, feedItemId, productItemId, hoursToProduct=12) |
| ChickenFeed EntityData | ScriptableObject | tag=FeedItem, stackable |
| Egg EntityData | ScriptableObject | sellPrice, category=AnimalProduct |

### UI
Không cần UI riêng. Feedback = animation gà ăn + feedItem count giảm. State indicator (icon nhỏ trên đầu: hungry/happy/product).

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | AnimalRuntime |
| GameHourChangedPublish | TimeManager | AnimalRuntime (check timer) |
| DayChangedPublish | TimeManager | AnimalRuntime (reset hungry) |

### Scene Objects
- Chicken prefab (EntityRoot, SpriteRenderer, Collider2D, AnimalStateIndicator)
- Pen area (chuồng)

---

## 2. THU SẢN PHẨM GÀ

### Mô tả
Khi gà ở state ProductReady, người chơi đứng gần và nhấn E/chuột phải (tay không hoặc bất kỳ item). Gà drop trứng vào inventory player. Gà quay về state Hungry.

### Điều kiện
- Animal entity ở state ProductReady
- Player đứng trong range interact (≤ 1f)
- Player inventory có slot trống

### Flow
```
E / Chuột Phải → SecondaryActionEvent → ActionRuntime
    → Forward sang Chicken entity
    → AnimalRuntime.Handle(SecondaryActionEvent):
        1. Check state == ProductReady? → YES
        2. InventoryService.TryAdd(player, productEntityData, 1) → grant Egg
        3. SetState(AnimalState.Hungry)
        4. Play animation thu hoạch (optional)
        5. EventBus.Publish(AnimalProductCollectedPublish(animalType, productId))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AnimalRuntime | Check state, grant product |
| InventoryService | Add product to player |
| ProgressionService | Grant Mastery EXP (small amount) |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Egg EntityData | ScriptableObject | sellPrice=30, category=AnimalProduct |

### UI
- Popup "+1 Egg" (fade out)
- State indicator trên gà đổi về hungry icon

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | AnimalRuntime |
| AnimalProductCollectedPublish | AnimalRuntime | ProgressionService (grant EXP) |

### Scene Objects
- Chicken prefab (state indicator thay đổi)

---

## 3. BÒ (CHO ĂN + THU SỮA)

### Mô tả
Cùng pattern với gà. Bò ăn Hay (cỏ khô), sau 24h game → ProductReady. Thu hoạch = Milk. Bò cần chuồng lớn hơn (2x2 cells per animal).

### Điều kiện
- Giống gà, thay feedItem = Hay, product = Milk
- Chuồng bò (CowBarn) đã xây
- hoursToProduct = 24

### Flow
```
(Giống flow Cho gà ăn / Thu sản phẩm gà)
AnimalRuntime xử lý chung, phân biệt bằng AnimalModule config:
    - animalType = Cow
    - feedItemId = "hay"
    - productItemId = "milk"
    - hoursToProduct = 24
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AnimalRuntime | Shared logic, config-driven |
| InventoryService | Add/remove items |
| TimeManager | Timer |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Cow EntityData | ScriptableObject | AnimalModule(animalType=Cow, feedItemId="hay", productItemId="milk", hoursToProduct=24) |
| Hay EntityData | ScriptableObject | tag=FeedItem, buyPrice=20 |
| Milk EntityData | ScriptableObject | sellPrice=80, category=AnimalProduct |

### UI
Giống gà. State indicator + popup khi thu.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | AnimalRuntime |
| AnimalProductCollectedPublish | AnimalRuntime | ProgressionService |

### Scene Objects
- Cow prefab (larger sprite, same component structure)
- CowBarn building

---

## 4. CỪU (CHO ĂN + THU LÔNG)

### Mô tả
Cùng pattern. Cừu ăn Hay, sau 48h game → ProductReady. Thu hoạch = Wool. Wool giá cao hơn nhưng chậm hơn.

### Điều kiện
- feedItem = Hay, product = Wool
- Chuồng cừu (SheepPen) đã xây
- hoursToProduct = 48

### Flow
```
(Giống flow chung AnimalRuntime)
    - animalType = Sheep
    - feedItemId = "hay"
    - productItemId = "wool"
    - hoursToProduct = 48
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AnimalRuntime | Shared logic |
| InventoryService | Add/remove items |
| TimeManager | Timer |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Sheep EntityData | ScriptableObject | AnimalModule(animalType=Sheep, feedItemId="hay", productItemId="wool", hoursToProduct=48) |
| Wool EntityData | ScriptableObject | sellPrice=120, category=AnimalProduct |

### UI
Giống gà/bò.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | AnimalRuntime |
| AnimalProductCollectedPublish | AnimalRuntime | ProgressionService |

### Scene Objects
- Sheep prefab
- SheepPen building

---

## 5. XÂY CHUỒNG

### Mô tả
Người chơi mua blueprint chuồng từ shop hoặc craft, sau đó đặt vào farm. Chuồng chiếm multi-cell (2x2 hoặc 3x3), có capacity limit (số con tối đa). Khi đặt xong, có thể mua/nhận vật nuôi.

### Điều kiện
- Player có building item (ChickenCoop/CowBarn/SheepPen) trong inventory
- Vùng đặt đủ rộng (multi-cell check), không có blocker
- Player có đủ tiền (nếu mua từ shop)

### Flow
```
Player cầm Building Item trên hotbar → Chuột Trái
    → PrimaryActionEvent → ActionRuntime forward → BuildingPlacementRuntime.Handle()
        → BuildingPlacementRuntime.Validate():
            1. GridSystem.GetMultiCellArea(actorGO, buildingSize) → cells[]
            2. Foreach cell: WorldEntityService.HasBlockerAt(cell) → phải false
            3. Check all cells tillable/buildable
            → OK → return true
        → ToolActionBridge.Request() → animation
        → AnimStrikeEvent → BuildingPlacementRuntime.Execute():
            1. EventBus.Publish(SpawnRequestPublish(worldPos, Building, buildingEntityData))
            2. SpawnSystem → Instantiate building prefab
            3. BuildingRuntime.Init(capacity, animalType)
            4. WorldEntityService.RegisterMultiCell(cells[], building)
            5. EntityService.AddAmount(buildingItem, -1)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| BuildingPlacementRuntime 🆕 | Multi-cell placement logic |
| GridSystem | Multi-cell area calculation |
| WorldEntityService | Register building, check blockers |
| SpawnSystem | Instantiate building |
| EntityService | Remove building item |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ChickenCoop EntityData | ScriptableObject | BuildingModule(size=2x2, capacity=4, animalType=Chicken) |
| CowBarn EntityData | ScriptableObject | BuildingModule(size=3x3, capacity=2, animalType=Cow) |
| SheepPen EntityData | ScriptableObject | BuildingModule(size=2x3, capacity=3, animalType=Sheep) |
| Building Item EntityData | ScriptableObject | PlacementModule(buildingSize, placedEntityData) |

### UI
- Ghost preview khi đang đặt (green = OK, red = blocked)
- Capacity indicator trên building (2/4 con)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | BuildingPlacementRuntime |
| SpawnRequestPublish | BuildingPlacementRuntime | SpawnSystem |

### Scene Objects
- Building prefabs (ChickenCoop, CowBarn, SheepPen) — multi-cell footprint
- Ghost preview object (transparent sprite)

---

## 6. ĐỘNG VẬT BỆNH/CHẾT

### Mô tả
Nếu vật nuôi không được cho ăn trong 3 ngày liên tiếp, nó chết và biến mất. Đơn giản, không có bệnh phức tạp — chỉ đếm ngày không ăn.

### Điều kiện
- Animal entity tồn tại
- Ngày mới + animal vẫn ở state Hungry (không được cho ăn)

### Flow
```
TimeManager → DayChangedPublish → AnimalRuntime nhận
    → AnimalRuntime.HandleNewDay():
        1. if state == Hungry:
            - daysNotFed++
            - if daysNotFed == 2:
                → Show warning icon (sick sprite)
                → Debug.Log("Vật nuôi đang đói, cần cho ăn!")
            - if daysNotFed >= 3:
                → entity.TriggerEvent(new DieEvent(entity))
                → MortalRuntime → DestroyEntityRequestPublish
                → Animal biến mất
                → BuildingRuntime.currentCount--
        2. if state == Fed || state == ProductReady:
            - daysNotFed = 0
        3. if state == Fed:
            - SetState(Hungry) (reset cho ngày mới, cần cho ăn lại)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| TimeManager | DayChangedPublish |
| AnimalRuntime | Track daysNotFed |
| MortalRuntime | Destroy animal |
| BuildingRuntime | Update capacity count |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| AnimalModule | 🔧 thêm | sickSprite (Sprite), maxDaysNotFed=3 |

### UI
- Warning icon trên đầu animal khi daysNotFed >= 2
- Notification "Gà của bạn đã chết vì đói!" (nếu chết)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DayChangedPublish | TimeManager | AnimalRuntime |
| DieEvent | AnimalRuntime | MortalRuntime |
| DestroyEntityRequestPublish | MortalRuntime | SpawnSystem |
| AnimalDiedPublish 🆕 | AnimalRuntime | BuildingRuntime, NotificationUI |

### Scene Objects
- Animal prefabs (sprite thay đổi khi sick)
- Building (capacity tracking)

---

## TỔNG KẾT: ANIMAL SYSTEM DEPENDENCY

```
PlayerControler (Input E / Chuột Phải)
    │
    ▼
ActionRuntime (Route SecondaryAction → nearest animal)
    │
    ▼
AnimalRuntime (State Machine: Hungry → Fed → ProductReady)
    │
    ├── Cho ăn: Check feedItem → EntityService.AddAmount(-1) → SetState(Fed)
    ├── Thu sản phẩm: InventoryService.TryAdd(product) → SetState(Hungry)
    └── Chết: daysNotFed >= 3 → DieEvent → MortalRuntime
    
TimeManager
    │
    ▼
DayChangedPublish
    ├── AnimalRuntime.HandleNewDay() → check daysNotFed
    └── AnimalRuntime: Fed → Hungry (reset daily)

GameHourChangedPublish
    └── AnimalRuntime: check timer → Fed → ProductReady

BuildingPlacementRuntime (Chuột Trái + Building Item)
    │
    ▼
SpawnSystem → Building prefab → capacity tracking
    │
    ▼
Animal spawn (mua từ shop) → register vào building
```
