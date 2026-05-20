# 🎮 INPUT ROUTING — Thiết kế chi tiết

> **Hệ thống:** InputRouting
> **Mục đích:** Giải thích cách TẤT CẢ inputs được route từ PlayerControler tới các systems khác nhau.
> **Chức năng nhỏ:**
> 1. Mouse Left → PrimaryAction
> 2. Mouse Right / E → SecondaryAction
> 3. 1-8 keys → Hotbar selection
> 4. Scroll → Cycle hotbar
> 5. Shift → Dodge
> 6. F5 → Save
> 7. Multi-feature input sharing
> 8. SecondaryAction conflict resolution

---

## TỔNG QUAN

Tất cả player input đi qua **PlayerControler** (MonoBehaviour). PlayerControler KHÔNG chứa gameplay logic — chỉ detect input và publish events. Gameplay logic nằm trong EntityRuntime modules.

**Nguyên tắc:** 1 input → 1 event → routing system quyết định ai xử lý.

---

## 1. MOUSE LEFT → PRIMARY ACTION

### Mô tả
Chuột trái = hành động chính. Dùng cho: tool use (hoe, water, pickaxe, axe), weapon attack, seed placement, building placement. Hành động cụ thể phụ thuộc vào item đang cầm trên hotbar.

### Điều kiện
- Input.GetMouseButtonDown(0)
- PlayerControler.InputEnabled == true
- ToolActionBridge.IsBusy == false (không đang animation)
- Player không đang dodge

### Flow
```
Input.GetMouseButtonDown(0)
    → PlayerControler.HandleActions()
        → playerEntity.TriggerEvent(new PrimaryActionEvent(playerEntity))
            → ActionRuntime.Handle(PrimaryActionEvent):
                1. if e.item != null → return (đã forward, tránh loop)
                2. Tìm item đang cầm: hotbar.SelectedEntity
                3. if selectedItem != null:
                    → selectedItem.TriggerEvent(new PrimaryActionEvent(actor, selectedItem))
                    → Item entity nhận event → module runtime xử lý:
                        ├── ToolModule → ToolRuntime subclass
                        │       ├── HoeRuntime (toolType=Hoe)
                        │       ├── WateringCanRuntime (toolType=WateringCan)
                        │       ├── PickaxeRuntime (toolType=Pickaxe)
                        │       ├── AxeRuntime (toolType=Axe)
                        │       └── ScytheRuntime (toolType=Scythe)
                        ├── WeaponModule → WeaponRuntime
                        ├── PlacementModule → PlacementRuntime (seeds, buildings)
                        └── ConsumableModule → ConsumableRuntime (eat food)
                4. if selectedItem == null:
                    → actor.TriggerEvent(new PrimaryActionEvent(actor, actor))
                    → Unarmed action (punch? nothing?)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Detect mouse left |
| ActionRuntime | Route to held item |
| InventoryRuntime (Hotbar) | Provide selected item |
| ToolRuntime subclasses | Handle tool actions |
| WeaponRuntime | Handle weapon attacks |
| PlacementRuntime | Handle placement |
| ToolActionBridge | Animation gating |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Player EntityData | ScriptableObject | ActionModule (routes PrimaryAction) |
| Item EntityData | ScriptableObject | ToolModule/WeaponModule/PlacementModule (receives forwarded event) |

### UI
Không trực tiếp. Hotbar highlight cho biết item nào đang active.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent (lần 1) | PlayerControler → EntityRuntime | ActionRuntime |
| PrimaryActionEvent (lần 2, forwarded) | ActionRuntime | ToolRuntime/WeaponRuntime/PlacementRuntime |
| AnimStrikeEvent | ToolActionBridge | ToolRuntime.Execute() |

### Scene Objects
- Player prefab (PlayerControler, EntityRoot, ToolActionBridge)

---

## 2. MOUSE RIGHT / E → SECONDARY ACTION

### Mô tả
Chuột phải hoặc phím E = tương tác. Dùng cho: NPC dialogue, shop, quest, animal feed/collect, harvest by hand, pickup items, portal, bed. Target = entity gần nhất có IInteractable module.

### Điều kiện
- Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(1)
- PlayerControler.InputEnabled == true
- Không đang busy/dodge

### Flow
```
Input.GetKeyDown(E) || Input.GetMouseButtonDown(1)
    → PlayerControler.HandleActions()
        → playerEntity.TriggerEvent(new SecondaryActionEvent(playerEntity))
            → ActionRuntime.Handle(SecondaryActionEvent):
                1. EntityScanSystem.GetClosest(actorGO, 1f) → target entity
                2. if target == null → "Không có target" → return
                3. Create InteractionContext(actor, target)
                4. target.TriggerEvent(new SecondaryActionEvent(actor, target, context))
                5. Target entity modules nhận event:
                    ├── DialogueRuntime → context.AddOption("dialogue", ...)
                    ├── ShopRuntime → context.AddOption("shop", ...)
                    ├── QuestRuntime → context.AddOption("quest", ...)
                    ├── AnimalRuntime → feed/collect logic
                    ├── HarvestRuntime → harvest by hand
                    ├── HerbPickupRuntime → pickup herb
                    ├── ScenePortalRuntime → context.AddOption("portal", ...)
                    ├── BedRuntime → context.AddOption("sleep", ...)
                    ├── CraftingNPCRuntime → context.AddOption("craft", ...)
                    ├── LoreItemRuntime → context.AddOption("read"/"pickup", ...)
                    └── UnlockNPCRuntime → context.AddOption("unlock", ...)
                6. if context.HasOptions:
                    → EventBus.Publish(InteractionOptionsReadyPublish(actor, target, options))
                    → InteractionUI hiện menu cho player chọn
                7. else: warning log
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Detect E / right-click |
| ActionRuntime | Find target, create context, forward |
| EntityScanSystem | GetClosest() — nearest interactable |
| InteractionContext | Collect options from modules |
| InteractionUI | Display options menu |
| All IInteractable modules | Add options to context |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Target EntityData | ScriptableObject | Modules that handle SecondaryActionEvent |

### UI
- InteractionUI: popup menu with options (nếu nhiều option)
- Direct action (nếu chỉ 1 option hoặc module xử lý trực tiếp)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent (lần 1) | PlayerControler → EntityRuntime | ActionRuntime |
| SecondaryActionEvent (lần 2, forwarded) | ActionRuntime | Target modules |
| InteractionOptionsReadyPublish | ActionRuntime | InteractionUI |

### Scene Objects
- Player prefab
- All interactable entities (NPCs, animals, portals, beds, items)

---

## 3. 1-8 KEYS → HOTBAR SELECTION

### Mô tả
Phím 1-8 chọn slot tương ứng trên hotbar. Slot được chọn = item active cho PrimaryAction. Chỉ 1 slot active tại 1 thời điểm.

### Điều kiện
- Input.GetKeyDown(KeyCode.Alpha1 + i)
- PlayerInventory != null

### Flow
```
Input.GetKeyDown(KeyCode.Alpha1) → i = 0
    → PlayerInventory.SelectSlot(0)
        → InventoryRuntime (Hotbar).SetSelectedIndex(0)
        → EventBus.Publish(HotbarSelectionChangedPublish(slotIndex, itemEntity))
        → HotbarUI highlight slot 0
        → Nếu item có visual (weapon sprite) → update player visual
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Detect 1-8 keys |
| PlayerInventory | SelectSlot() |
| InventoryRuntime (Hotbar) | Track selected index |
| HotbarUI | Visual highlight |

### Data
Không cần data riêng.

### UI
- Hotbar UI: 8 slots, selected slot highlighted (border glow)
- Item icon + count in each slot

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| HotbarSelectionChangedPublish | InventoryRuntime | HotbarUI, PlayerVisual |

### Scene Objects
- HotbarUI (Canvas)
- Player prefab (PlayerInventory)

---

## 4. SCROLL → CYCLE HOTBAR

### Mô tả
Scroll wheel lên/xuống cycle qua hotbar slots. Scroll up = slot trước, scroll down = slot sau. Wrap around (slot 8 → slot 1).

### Điều kiện
- Input.GetAxis("Mouse ScrollWheel") != 0
- PlayerInventory != null

### Flow
```
float scroll = Input.GetAxis("Mouse ScrollWheel")
    → if scroll > 0: PlayerInventory.CycleHotbar(-1) → previous slot
    → if scroll < 0: PlayerInventory.CycleHotbar(+1) → next slot
        → InventoryRuntime.CycleSelected(direction)
            → selectedIndex = (selectedIndex + direction + 8) % 8
            → EventBus.Publish(HotbarSelectionChangedPublish(...))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Detect scroll |
| PlayerInventory | CycleHotbar() |
| InventoryRuntime | Cycle logic |

### Data
Không cần.

### UI
- Hotbar highlight moves

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| HotbarSelectionChangedPublish | InventoryRuntime | HotbarUI |

### Scene Objects
- (Shared with hotbar)

---

## 5. SHIFT → DODGE

### Mô tả
Shift = dodge/dash. Player di chuyển nhanh theo hướng input (hoặc lastMoveDirection). Tốn stamina, có invincibility frames. Đã implement trong PlayerControler.

### Điều kiện
- Input.GetKeyDown(dodgeKey = LeftShift)
- !isDodging && !IsActionBusy
- Stamina >= dodgeStaminaCost (12)

### Flow
```
Input.GetKeyDown(LeftShift)
    → PlayerControler.TryStartDodge():
        1. Check conditions (not busy, not dodging)
        2. TrySpendStamina(entity, 12) → trừ stamina
        3. ReadInputDirection() → direction
        4. StartCoroutine(DodgeRoutine(direction)):
            - isDodging = true
            - (Set invincible on HealthRuntime)
            - Lerp position over dodgeDuration (0.16s)
            - Move dodgeDistance (1.25f) in direction
            - isDodging = false
            - (Clear invincible after short delay)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Dodge logic (đã có) |
| EntityRuntime.stats | Stamina |
| HealthRuntime | Invincibility flag |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| PlayerControler (serialized) | | dodgeKey=LeftShift, dodgeDistance=1.25, dodgeDuration=0.16, dodgeStaminaCost=12 |

### UI
- Stamina bar decrease
- Optional: dash trail visual

### Events
Không có event riêng — logic nội bộ PlayerControler + coroutine.

### Scene Objects
- Player prefab

---

## 6. F5 → SAVE

### Mô tả
F5 = quick save. Publish SaveGameRequestPublish → SaveSystem xử lý. Không block gameplay.

### Điều kiện
- Input.GetKeyDown(KeyCode.F5)
- EventBus != null

### Flow
```
Input.GetKeyDown(F5)
    → PlayerControler.HandleActions()
        → eventBus.Publish(new SaveGameRequestPublish())
        → SaveSystem.Handle(SaveGameRequestPublish):
            1. Collect all save data (entities, inventory, progression, time, weather, quests)
            2. Serialize to JSON/binary
            3. Write to file
            4. Show notification: "Game saved!"
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Detect F5 |
| EventBus | Publish save request |
| SaveSystem | Handle save logic |

### Data
Không cần config riêng.

### UI
- "Game saved!" notification (fade, 2s)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SaveGameRequestPublish | PlayerControler (via EventBus) | SaveSystem |

### Scene Objects
- SaveSystem (singleton)

---

## 7. MULTI-FEATURE INPUT SHARING

### Mô tả
Nhiều features dùng cùng 1 input (chuột trái). Phân biệt bằng **item đang cầm trên hotbar**. ActionRuntime forward event tới item entity → item entity có module nào thì module đó xử lý.

### Điều kiện
- Cùng input (chuột trái) nhưng khác item → khác behavior

### Flow
```
CÙNG INPUT: Chuột Trái
    │
    ├── Cầm Hoe → HoeRuntime → cuốc đất
    ├── Cầm WateringCan → WateringCanRuntime → tưới nước
    ├── Cầm Pickaxe → PickaxeRuntime → đập đá
    ├── Cầm Axe → AxeRuntime → chặt cây
    ├── Cầm Scythe → ScytheRuntime → cắt cỏ/harvest
    ├── Cầm Sword → WeaponRuntime → tấn công
    ├── Cầm Seed → PlacementRuntime → gieo hạt
    ├── Cầm Building → BuildingPlacementRuntime → đặt building
    ├── Cầm Fertilizer → FertilizerRuntime → bón phân
    ├── Cầm Food → ConsumableRuntime → ăn (optional, hoặc dùng menu)
    └── Tay không → nothing (hoặc punch)

RESOLUTION: Không có conflict vì mỗi item entity chỉ có 1 module xử lý PrimaryActionEvent.
    - HoeEntityData có ToolModule(Hoe) → tạo HoeRuntime
    - SeedEntityData có PlacementModule → tạo PlacementRuntime
    - SwordEntityData có WeaponModule → tạo WeaponRuntime
    
Nếu item có NHIỀU modules handle PrimaryAction (hiếm):
    → EntityRuntime.TriggerEvent() gọi tất cả handlers theo thứ tự modules[]
    → Module đầu tiên Validate() == true sẽ request animation
    → ToolActionBridge chỉ accept 1 request (IsBusy gate)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ActionRuntime | Forward to correct item |
| InventoryRuntime | Provide selected item |
| EntityRuntime | Module dispatch |
| ToolActionBridge | Single-action gate |

### Data
Mỗi item EntityData chỉ có 1 "action module" (ToolModule OR PlacementModule OR WeaponModule).

### UI
- Hotbar: item icon cho biết action sẽ xảy ra
- Cursor change (optional): tool cursor khi cầm tool

### Events
(Shared with PrimaryAction flow)

### Scene Objects
- (Shared)

---

## 8. SECONDARY ACTION CONFLICT RESOLUTION

### Mô tả
Khi player nhấn E gần nhiều interactable entities, cần resolve: ai được interact? Dùng **nearest entity** (EntityScanSystem.GetClosest) + **InteractionContext priority system**.

### Điều kiện
- Nhiều entities trong range 1f
- Player nhấn E/chuột phải

### Flow
```
STEP 1: Target Selection (EntityScanSystem.GetClosest)
    - Scan all entities within range (1f)
    - Filter: entity phải có ít nhất 1 module handle SecondaryActionEvent
    - Sort by distance
    - Return nearest → đây là target

STEP 2: Option Collection (InteractionContext)
    - Forward SecondaryActionEvent tới target
    - Target có thể có NHIỀU modules handle event:
        ├── DialogueRuntime → AddOption("dialogue", priority=10)
        ├── ShopRuntime → AddOption("shop", priority=20)
        └── QuestRuntime → AddOption("quest", priority=30)
    - Mỗi module AddOption() vào context với priority

STEP 3: Resolution
    - if context.Options.Count == 0: warning, nothing happens
    - if context.Options.Count == 1: execute directly (no menu)
    - if context.Options.Count > 1:
        → InteractionOptionsReadyPublish → InteractionUI
        → Show menu: player chọn option
        → Execute selected option's callback

PRIORITY SYSTEM:
    - Higher priority = hiện trước trong menu
    - Urgent options (quest turn-in) có priority cao
    - Default options (dialogue) có priority thấp
    - Priority chỉ ảnh hưởng thứ tự hiển thị, không auto-select

EDGE CASES:
    - Animal (Hungry) + Player cầm feed: AnimalRuntime xử lý trực tiếp (không qua option menu)
    - Herb pickup: HerbPickupRuntime xử lý trực tiếp
    - Bed: BedRuntime add option "Đi ngủ" → confirm popup
    - NPC với nhiều roles: tất cả modules add options → player chọn
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| EntityScanSystem | GetClosest() — target selection |
| InteractionContext | Collect + prioritize options |
| InteractionUI | Display option menu |
| All SecondaryAction modules | Add options |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| InteractionContext | Runtime class | options[], AddOption(id, textKey, priority, callback) |

### UI
- InteractionUI: vertical menu popup near target
- Options sorted by priority (highest first)
- Player click/key to select

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | Target modules |
| InteractionOptionsReadyPublish | ActionRuntime | InteractionUI |

### Scene Objects
- InteractionUI (Canvas, world-space or screen-space popup)
- All interactable entities

---

## TỔNG KẾT: INPUT ROUTING COMPLETE MAP

```
┌─────────────────────────────────────────────────────────────────┐
│                     PlayerControler                               │
│                                                                   │
│  Update() → if InputEnabled && !IsActionBusy && !isDodging:      │
│                                                                   │
│  ┌─── Shift ──────────────────────────────────────────────────┐  │
│  │ TryStartDodge() → stamina check → DodgeRoutine            │  │
│  │ (i-frames, lerp position, 0.16s)                           │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─── HandleMovement() ──────────────────────────────────────┐  │
│  │ WASD/Arrows → move + update direction + animation         │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─── HandleActions() ───────────────────────────────────────┐  │
│  │                                                            │  │
│  │  Mouse Left (0) ──→ PrimaryActionEvent                    │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  ActionRuntime → Hotbar.SelectedEntity                    │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  Forward to item → Module handles:                        │  │
│  │       ├── ToolRuntime (Hoe/Water/Pick/Axe/Scythe)        │  │
│  │       ├── WeaponRuntime (Sword/Spear)                     │  │
│  │       ├── PlacementRuntime (Seed/Building)                │  │
│  │       └── ConsumableRuntime (Food)                        │  │
│  │                                                            │  │
│  │  E / Mouse Right (1) ──→ SecondaryActionEvent             │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  ActionRuntime → EntityScanSystem.GetClosest()            │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  Forward to target → Modules add options:                 │  │
│  │       ├── DialogueRuntime                                  │  │
│  │       ├── ShopRuntime                                      │  │
│  │       ├── QuestRuntime                                     │  │
│  │       ├── AnimalRuntime                                    │  │
│  │       ├── ScenePortalRuntime                               │  │
│  │       ├── BedRuntime                                       │  │
│  │       ├── CraftingNPCRuntime                               │  │
│  │       └── HerbPickupRuntime                                │  │
│  │       │                                                    │  │
│  │       ▼                                                    │  │
│  │  InteractionContext → InteractionUI (if multiple options) │  │
│  │                                                            │  │
│  │  1-8 Keys ──→ PlayerInventory.SelectSlot(i)               │  │
│  │       → HotbarSelectionChangedPublish → HotbarUI          │  │
│  │                                                            │  │
│  │  Scroll ──→ PlayerInventory.CycleHotbar(±1)              │  │
│  │       → HotbarSelectionChangedPublish → HotbarUI          │  │
│  │                                                            │  │
│  │  F5 ──→ EventBus.Publish(SaveGameRequestPublish)          │  │
│  │       → SaveSystem handles                                 │  │
│  │                                                            │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

KEY PRINCIPLES:
    1. PlayerControler = input detection ONLY (no gameplay logic)
    2. ActionRuntime = routing layer (forward to correct entity)
    3. Item on hotbar = determines PrimaryAction behavior
    4. Nearest entity = determines SecondaryAction target
    5. InteractionContext = resolves multi-option conflicts
    6. ToolActionBridge = gates 1 action at a time (animation lock)
    7. Events = decoupled communication between systems
```
