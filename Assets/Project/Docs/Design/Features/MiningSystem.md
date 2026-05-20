# ⛏️ MINING SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** MiningSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Đập đá/quặng
> 2. Chặt cây
> 3. Hái thảo mộc
> 4. Quặng theo tier
> 5. Tài nguyên respawn
> 6. Mine scene

---

## INPUT ROUTING (áp dụng cho toàn bộ MiningSystem)

Mining dùng PrimaryAction (chuột trái) với tool phù hợp:

```
Player cầm Pickaxe/Axe trên hotbar → Chuột Trái
    → PrimaryActionEvent → ActionRuntime forward → DamageToolRuntime.Handle()
        → Validate() → ToolActionBridge.Request() → animation
        → AnimStrikeEvent → Execute() → TakeDamageEvent tới resource entity

Hái thảo mộc dùng SecondaryAction (E/chuột phải):
    → SecondaryActionEvent → ActionRuntime → nearest herb → pickup
```

---

## 1. ĐẬP ĐÁ/QUẶNG

### Mô tả
Người chơi cầm Pickaxe trên hotbar, đứng gần đá/quặng, nhấn chuột trái. Mỗi hit trừ HP của đá. Khi HP <= 0, đá vỡ và drop ore/stone. Dùng DamageToolRuntime (toolType=Pickaxe).

### Điều kiện
- Player đang cầm Pickaxe
- Có Rock/Ore entity trong range
- Player có đủ stamina (≥ pickaxe cost)
- ToolActionBridge không busy

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → PickaxeRuntime.Handle()
    (PickaxeRuntime extends DamageToolRuntime, toolType=Pickaxe)
    → Validate(): return true
    → ToolActionBridge.Request() → animation "Pickaxe"
    → AnimStrikeEvent → Execute():
        1. EntityScanSystem.GetAll(actorGO, range) → targets
        2. FindNearest() → rock/ore entity
        3. target.TriggerEvent(new TakeDamageEvent(actor, damage, Pickaxe))
    
Rock/Ore entity nhận TakeDamageEvent:
    → HealthRuntime.Handle(TakeDamageEvent):
        1. Check toolType == requiredTool (Pickaxe)? → YES
        2. currentHP -= damage
        3. Visual: shake sprite, particle
        4. if currentHP <= 0:
            → DieEvent
            → DropRuntime → spawn ore/stone drops
            → MortalRuntime → destroy rock
            → RespawnRuntime → schedule respawn
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| DamageToolRuntime (Pickaxe) | Đã có — gây damage |
| HealthRuntime (rock) | HP tracking, tool check |
| DropRuntime | Spawn drops |
| MortalRuntime | Destroy rock |
| RespawnRuntime 🆕 | Schedule respawn |
| ExpRewardRuntime | Grant EXP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Pickaxe EntityData | ScriptableObject | ToolModule(toolType=Pickaxe), stats(Attack=2, Range=1.5, StaminaCost=4) |
| Rock EntityData | ScriptableObject | HealthModule(HP=5, requiredTool=Pickaxe), DropModule(drops[{stone,100%,1-3}]) |
| CopperOre EntityData | ScriptableObject | HealthModule(HP=8), DropModule(drops[{copper_ore,100%,1-2},{stone,50%,1}]) |
| IronOre EntityData | ScriptableObject | HealthModule(HP=15), DropModule(drops[{iron_ore,100%,1-2}]) |

### UI
- Rock HP bar (hiện khi bị hit)
- Drop items scatter trên ground
- Particle effect khi hit/destroy

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → PickaxeRuntime |
| AnimStrikeEvent | ToolActionBridge | PickaxeRuntime |
| TakeDamageEvent | PickaxeRuntime | HealthRuntime (rock) |
| DieEvent | HealthRuntime | DropRuntime, MortalRuntime, RespawnRuntime |

### Scene Objects
- Rock/Ore prefabs (EntityRoot, HealthRuntime, DropRuntime, SpriteRenderer, Collider2D)
- Drop item prefabs

---

## 2. CHẶT CÂY

### Mô tả
Người chơi cầm Axe, đứng gần cây (Tree entity), nhấn chuột trái. Mỗi hit trừ HP. Khi HP <= 0, cây đổ và drop wood/sap. Cùng pattern với đập đá, khác toolType.

### Điều kiện
- Player đang cầm Axe
- Có Tree entity trong range
- Player có đủ stamina

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → AxeRuntime.Handle()
    (AxeRuntime extends DamageToolRuntime, toolType=Axe)
    → Validate(): return true
    → ToolActionBridge.Request() → animation "Axe"
    → AnimStrikeEvent → Execute():
        → target.TriggerEvent(new TakeDamageEvent(actor, damage, Axe))
    
Tree entity nhận TakeDamageEvent:
    → HealthRuntime: check toolType == Axe → trừ HP
    → HP <= 0 → DieEvent
        → DropRuntime → spawn wood/sap
        → MortalRuntime → destroy tree (with fall animation)
        → RespawnRuntime → schedule respawn
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| DamageToolRuntime (Axe) | Gây damage |
| HealthRuntime (tree) | HP, tool check |
| DropRuntime | Spawn drops |
| RespawnRuntime | Respawn after X days |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Axe EntityData | ScriptableObject | ToolModule(toolType=Axe), stats(Attack=2, Range=1.5, StaminaCost=4) |
| Tree EntityData | ScriptableObject | HealthModule(HP=10, requiredTool=Axe), DropModule(drops[{wood,100%,2-4},{sap,30%,1}]) |
| HardwoodTree EntityData | ScriptableObject | HealthModule(HP=20), DropModule(drops[{hardwood,100%,2-3}]) |

### UI
- Tree HP bar
- Fall animation khi chết
- Drop items

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| TakeDamageEvent | AxeRuntime | HealthRuntime (tree) |
| DieEvent | HealthRuntime | DropRuntime, MortalRuntime, RespawnRuntime |

### Scene Objects
- Tree prefabs (EntityRoot, HealthRuntime, DropRuntime, SpriteRenderer)

---

## 3. HÁI THẢO MỘC

### Mô tả
Người chơi đứng gần herb (thảo mộc), nhấn E/chuột phải. Không cần tool — pickup trực tiếp. Herb biến mất, item vào inventory. Respawn sau X ngày.

### Điều kiện
- Herb entity trong range interact (≤ 1f)
- Player nhấn E/chuột phải
- Player inventory có slot trống

### Flow
```
E / Chuột Phải → SecondaryActionEvent → ActionRuntime
    → EntityScanSystem.GetClosest() → Herb entity
    → Forward SecondaryActionEvent sang Herb
    → HerbPickupRuntime.Handle(SecondaryActionEvent):
        1. InventoryService.TryAdd(player, herbEntityData, 1) → grant herb item
        2. if success:
            → entity.TriggerEvent(new DieEvent(entity, initiator))
            → MortalRuntime → destroy herb visual
            → RespawnRuntime → schedule respawn (3 days)
            → ProgressionService.GrantMasteryExp(player, 2, GatherSource)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| HerbPickupRuntime 🆕 | Handle SecondaryAction → pickup |
| InventoryService | Add herb to player |
| MortalRuntime | Destroy herb |
| RespawnRuntime | Respawn timer |
| ProgressionService | Small EXP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| RedHerb EntityData | ScriptableObject | HerbPickupModule, RespawnModule(days=3), category=Herb |
| BlueHerb EntityData | ScriptableObject | HerbPickupModule, RespawnModule(days=4), category=Herb |

### UI
- "+1 Red Herb" popup
- Herb visual disappears

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | HerbPickupRuntime |
| DieEvent | HerbPickupRuntime | MortalRuntime, RespawnRuntime |

### Scene Objects
- Herb prefabs (EntityRoot, SpriteRenderer, Collider2D)

---

## 4. QUẶNG THEO TIER

### Mô tả
Quặng chia 4 tier, mỗi tier HP cao hơn, drop quý hơn, xuất hiện ở vùng sâu hơn trong mine. Pickaxe tier thấp vẫn đập được tier cao nhưng rất chậm (damage thấp so với HP).

### Điều kiện
- Ore entity thuộc tier tương ứng
- Player có Pickaxe (bất kỳ tier)
- Tier cao hơn = HP nhiều hơn = cần pickaxe mạnh hơn để hiệu quả

### Flow
```
(Giống flow Đập đá/quặng)
Phân biệt bằng EntityData config:

Tier 1 — Copper:  HP=8,  drops=[copper_ore], zone=Mine Floor 1-3
Tier 2 — Iron:    HP=15, drops=[iron_ore],   zone=Mine Floor 4-6
Tier 3 — Gold:    HP=25, drops=[gold_ore],   zone=Mine Floor 7-9
Tier 4 — Mythril: HP=40, drops=[mythril_ore], zone=Mine Floor 10+

Damage calculation:
    - Basic Pickaxe: Attack=2 → Copper(8HP) = 4 hits, Iron(15HP) = 8 hits
    - Iron Pickaxe: Attack=4 → Iron(15HP) = 4 hits, Gold(25HP) = 7 hits
    - Gold Pickaxe: Attack=7 → Gold(25HP) = 4 hits, Mythril(40HP) = 6 hits
    - Mythril Pickaxe: Attack=10 → Mythril(40HP) = 4 hits
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| DamageToolRuntime | Damage calculation |
| HealthRuntime | HP per tier |
| DropRuntime | Drops per tier |
| Mine scene | Floor-based spawning |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| CopperOre EntityData | ScriptableObject | HP=8, drops[{copper_ore,100%,1-2}], tier=1 |
| IronOre EntityData | ScriptableObject | HP=15, drops[{iron_ore,100%,1-2}], tier=2 |
| GoldOre EntityData | ScriptableObject | HP=25, drops[{gold_ore,100%,1-2}], tier=3 |
| MythrilOre EntityData | ScriptableObject | HP=40, drops[{mythril_ore,100%,1-2}], tier=4 |

### UI
- Ore HP bar (color-coded by tier)
- Ore sprite khác nhau theo tier

### Events
(Giống Đập đá/quặng — không có event riêng cho tier)

### Scene Objects
- Ore prefabs (4 tiers, different sprites/HP)
- Mine floors (spawn rules per floor)

---

## 5. TÀI NGUYÊN RESPAWN

### Mô tả
Khi resource (rock, tree, ore, herb) bị destroy, RespawnRuntime ghi nhận vị trí + ngày destroy. Sau X ngày game, resource respawn lại tại cùng vị trí. Tracked qua save data.

### Điều kiện
- Resource entity bị destroy (DieEvent)
- RespawnModule configured (daysToRespawn > 0)
- Ngày mới → check respawn queue

### Flow
```
Resource bị destroy → DieEvent → RespawnRuntime.Handle(DieEvent):
    1. Ghi respawn data: {entityDataId, position, dayDestroyed, daysToRespawn}
    2. RespawnRegistry.Add(respawnData)
    3. Entity bị destroy (MortalRuntime)

TimeManager → DayChangedPublish → RespawnRegistry.OnNewDay():
    1. currentDay = TimeManager.CurrentDay
    2. foreach entry in respawnQueue:
        - if currentDay - entry.dayDestroyed >= entry.daysToRespawn:
            → EventBus.Publish(SpawnRequestPublish(entry.position, entry.entityData))
            → Remove from queue
    3. SpawnSystem → Instantiate resource tại vị trí cũ
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| RespawnRuntime 🆕 | Ghi respawn data khi die |
| RespawnRegistry 🆕 | Track queue, check daily |
| TimeManager | DayChangedPublish |
| SpawnSystem | Re-instantiate resource |
| SaveSystem | Persist respawn queue |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| RespawnModule | IModuleData | daysToRespawn (int) |
| Rock: daysToRespawn=3 | | |
| Tree: daysToRespawn=5 | | |
| Ore: daysToRespawn=4 | | |
| Herb: daysToRespawn=3 | | |

### UI
Không. Resource xuất hiện lại tự nhiên.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DieEvent | HealthRuntime | RespawnRuntime |
| DayChangedPublish | TimeManager | RespawnRegistry |
| SpawnRequestPublish | RespawnRegistry | SpawnSystem |

### Scene Objects
- Resource prefabs (respawn tại vị trí cũ)

---

## 6. MINE SCENE

### Mô tả
Mine là scene riêng biệt, player vào qua portal (ScenePortalRuntime). Mine có nhiều floor, càng sâu → enemy mạnh hơn + ore tier cao hơn. Mỗi floor có exit portal xuống floor tiếp.

### Điều kiện
- Player tương tác Mine entrance (portal) ở farm scene
- ScenePortalRuntime → SceneTransitionService.RequestTransition()
- Mine scene load → player spawn tại entry point

### Flow
```
Farm Scene:
    Player → E gần Mine Entrance → SecondaryActionEvent
        → ScenePortalRuntime.Handle():
            → context.AddOption("scene.portal", "Vào mỏ")
            → Player chọn → SceneTransitionService.RequestTransition("MineScene", "floor1_entry")

Mine Scene:
    - Player spawn tại floor entry
    - Floor layout: ore nodes + enemies + exit portal
    - Player mine ore, fight enemies
    - Tìm exit portal → interact → load next floor (deeper)
    
    Floor difficulty scaling:
        Floor 1-3: Copper ore, Slime enemies
        Floor 4-6: Iron ore, Goblin enemies
        Floor 7-9: Gold ore, Skeleton enemies
        Floor 10+: Mythril ore, Boss enemies

    Exit mine:
        - Interact entrance portal → back to farm
        - Player chết → PlayerDeathHandler → respawn at home
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ScenePortalRuntime | Đã có — scene transition |
| SceneTransitionService | Load/unload scenes |
| SpawnSystem | Spawn ore + enemies per floor |
| MineFloorManager 🆕 | Track current floor, difficulty scaling |
| PlayerDeathHandler | Respawn if die in mine |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| MineFloorConfig | ScriptableObject | floorNumber, oreTypes[], enemyTypes[], spawnCounts |
| MineEntrance EntityData | ScriptableObject | ScenePortalModule(targetScene="MineScene") |

### UI
- Floor indicator: "Tầng 3"
- Mini-map (optional, defer)
- Exit confirmation popup

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | ScenePortalRuntime |
| SceneLoadedPublish | SceneTransitionService | MineFloorManager |

### Scene Objects
- Mine entrance portal (farm scene)
- Mine scene: floor layouts, ore spawn points, enemy spawn points, exit portals
- Floor entry/exit portals

---

## TỔNG KẾT: MINING SYSTEM DEPENDENCY

```
PlayerControler (Input)
    │
    ├── Chuột Trái + Pickaxe → DamageToolRuntime → TakeDamageEvent
    │       │
    │       ▼
    │   Rock/Ore HealthRuntime → HP -= damage
    │       │
    │       └── HP <= 0 → DieEvent
    │               ├── DropRuntime → spawn ore/stone
    │               ├── ExpRewardRuntime → Mastery EXP
    │               ├── MortalRuntime → destroy
    │               └── RespawnRuntime → schedule respawn
    │
    ├── Chuột Trái + Axe → DamageToolRuntime → TakeDamageEvent
    │       │
    │       ▼
    │   Tree HealthRuntime → same flow as rock
    │
    └── E / Chuột Phải → SecondaryAction
            │
            ├── Herb → HerbPickupRuntime → pickup + destroy + respawn
            └── Mine Portal → ScenePortalRuntime → load mine scene

TimeManager → DayChangedPublish
    └── RespawnRegistry.OnNewDay() → respawn resources

Mine Scene (separate)
    │
    ├── MineFloorManager → difficulty scaling per floor
    ├── Ore tier 1-4 → spawn by floor depth
    ├── Enemies → spawn by floor depth
    └── Portals → navigate between floors
```
