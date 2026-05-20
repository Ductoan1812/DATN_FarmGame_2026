# 04 — SCENE OBJECTS (Prefab & Object Architecture)

> Mỗi object: Components → Required References → EntityData → Modules → Scene Placement

---

## Player

**Components:**
- `EntityRoot` (MonoBehaviour — bridge between GameObject and EntityRuntime)
- `PlayerControler` (MonoBehaviour — input detection, movement, dodge)
- `ToolActionBridge` (MonoBehaviour — animation request/response, AnimStrikeEvent)
- `PlayerInventory` (MonoBehaviour — hotbar selection, cycle)
- `Animator` (Unity — character animations)
- `Rigidbody2D` (Unity — physics movement)
- `Collider2D` (Unity — interaction trigger)
- `SpriteRenderer` / HeroEditor4D character layers
- `EntityScanSystem` (MonoBehaviour — scan nearby entities)

**Required References (Serialized Fields):**
- `PlayerControler`: dodgeKey, dodgeDistance, dodgeDuration, dodgeStaminaCost, moveSpeed
- `ToolActionBridge`: animator reference, strike event frame config
- `EntityRoot`: EntityData reference
- `EntityScanSystem`: scanRadius, layerMask

**EntityData:** `PlayerEntityData`

**Modules:**
- ✅ `ActionModule` → ActionRuntime (route input)
- ✅ `InventoryModule` (Hotbar, 8 slots) → InventoryRuntime
- ✅ `InventoryModule` (Backpack, 20+ slots) → InventoryRuntime
- ✅ `EquipmentModule` (5 slots) → EquipmentRuntime
- ✅ `HealthModule` (baseHP=100) → HealthRuntime
- ✅ `StatsModule` (base stats) → StatsRuntime
- ✅ `QuestLogModule` → QuestLogRuntime
- ✅ `AppearanceModule` → AppearanceRuntime

**Scene Placement:** Farm scene — player house spawn point, persists across scenes

---

## NPC — Shop Merchant

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger — interaction range)
- `Animator` (idle animation)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `MerchantEntityData` (e.g., "Cô Mai", "Blacksmith")

**Modules:**
- ✅ `ShopModule` (stock items, buy/sell categories) → ShopRuntime
- ✅ `DialogueModule` (greeting dialogue) → DialogueRuntime
- ✅ `InventoryModule` (merchant stock) → InventoryRuntime

**Scene Placement:** Farm scene — shop area, fixed position

---

## NPC — Craft Station

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger)
- `Animator`

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `BlacksmithEntityData`, `ChefEntityData`, `AlchemistEntityData`

**Modules:**
- ✅ `CraftingModule` (recipes[], recipeCategory) → CraftingRuntime
- ✅ `DialogueModule` → DialogueRuntime

**Scene Placement:** Farm scene — workshop area (Blacksmith, Kitchen, Alchemy lab)

---

## NPC — Quest Giver

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger)
- `Animator`
- Quest indicator icon (! / ? above head)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `QuestNPCEntityData`

**Modules:**
- ✅ `QuestModule` (questGraphs[]) → QuestRuntime
- ✅ `DialogueModule` → DialogueRuntime

**Scene Placement:** Farm scene — village area, various locations

---

## NPC — Scholar (Research)

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger)
- `Animator`

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `ScholarEntityData`

**Modules:**
- 🆕 `ResearchNPCModule` (available research list) → ResearchNPCRuntime
- ✅ `DialogueModule` → DialogueRuntime

**Scene Placement:** Farm scene — library/lab building

---

## Enemy — Slime

**Components:**
- `EntityRoot`
- `EnemyObject` (MonoBehaviour — AI state machine: Idle/Chase/Attack/Cooldown)
- `SpriteRenderer`
- `Animator`
- `Rigidbody2D`
- `Collider2D`

**Required References:**
- `EnemyObject`: aggroRange, attackRange, attackCooldown, moveSpeed, player reference (found at runtime)
- `EntityRoot`: EntityData reference

**EntityData:** `SlimeEntityData`

**Modules:**
- ✅ `HealthModule` (HP=10) → HealthRuntime
- ✅ `AttackModule` (damage via AnimStrike) → AttackRuntime
- ✅ `DropModule` (drops[{slime_drop,80%,1-2},{gold,50%,5-15}]) → DropRuntime
- ✅ `MortalModule` → MortalRuntime
- ✅ `ExpRewardModule` (exp=2) → ExpRewardRuntime
- ✅ `RespawnModule` (days=3) → RespawnRuntime

**Scene Placement:** Farm edge (Phase 3+), Mine floors 1-3, Clear zones

---

## Enemy — Goblin

**Components:** Same as Slime (different EntityData/sprites)

**EntityData:** `GoblinEntityData`

**Modules:**
- ✅ `HealthModule` (HP=25) → HealthRuntime
- ✅ `AttackModule` (damage=6) → AttackRuntime
- ✅ `DropModule` (drops[{goblin_ear,70%,1},{gold,60%,10-25}]) → DropRuntime
- ✅ `MortalModule` → MortalRuntime
- ✅ `ExpRewardModule` (exp=5) → ExpRewardRuntime
- ✅ `RespawnModule` (days=4) → RespawnRuntime

**Scene Placement:** Mine floors 4-6, Clear zones (Phase 4+)

---

## Plant (Crop Entity)

**Components:**
- `EntityRoot`
- `SpriteRenderer` (changes per growth stage)
- `Collider2D` (trigger — for interaction/harvest)
- `StageObject` (MonoBehaviour — receives DayChangedPublish, forwards NextDayEvent)

**Required References:**
- `EntityRoot`: EntityData reference
- `StageObject`: TimeManager subscription

**EntityData:** `TomatoPlantData`, `CarrotPlantData`, `WheatPlantData`, etc.

**Modules:**
- ✅ `StageModule` (stages[{sprite, daysToGrow, canHarvest}], 🔧 regrowable, wiltSprite) → StageRuntime
- ✅ `HealthModule` (HP=1, requiredTool=Scythe) → HealthRuntime
- ✅ `HarvestModule` (harvestTool=Scythe or None) → HarvestRuntime
- ✅ `DropModule` (drops[{crop_item, 100%, 1-3}]) → DropRuntime
- ✅ `MortalModule` → MortalRuntime
- ✅ `ExpRewardModule` (exp=10-20) → ExpRewardRuntime
- 🆕 `QualityModule` (maxQuality=3) → QualityRuntime

**Scene Placement:** Farm scene — spawned dynamically on plowed tiles via PlacementRuntime

---

## Ore Node (Copper, Iron, Gold, Mythril)

**Components:**
- `EntityRoot`
- `SpriteRenderer` (tier-colored)
- `Collider2D`
- HP bar (world-space canvas, show on hit)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `CopperOreData`, `IronOreData`, `GoldOreData`, `MythrilOreData`

**Modules:**
- ✅ `HealthModule` (HP=8/15/25/40, requiredTool=Pickaxe) → HealthRuntime
- ✅ `DropModule` (drops[{ore_item, 100%, 1-2}]) → DropRuntime
- ✅ `MortalModule` → MortalRuntime
- ✅ `ExpRewardModule` (exp=3-5) → ExpRewardRuntime
- ✅ `RespawnModule` (days=4) → RespawnRuntime

**Scene Placement:** Mine scene — floor-based (Copper: F1-3, Iron: F4-6, Gold: F7-9, Mythril: F10+)

---

## Tree Node

**Components:**
- `EntityRoot`
- `SpriteRenderer` (with fall animation on death)
- `Collider2D`
- HP bar (world-space)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `TreeData`, `HardwoodTreeData`

**Modules:**
- ✅ `HealthModule` (HP=10/20, requiredTool=Axe) → HealthRuntime
- ✅ `DropModule` (drops[{wood,100%,2-4},{sap,30%,1}]) → DropRuntime
- ✅ `MortalModule` → MortalRuntime
- ✅ `ExpRewardModule` (exp=2-4) → ExpRewardRuntime
- ✅ `RespawnModule` (days=5) → RespawnRuntime

**Scene Placement:** Farm scene (edges), Wilderness, Clear zones

---

## Herb Node

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `RedHerbData`, `BlueHerbData`

**Modules:**
- 🆕 `HerbPickupModule` → HerbPickupRuntime (handle SecondaryAction → pickup)
- ✅ `MortalModule` → MortalRuntime
- ✅ `RespawnModule` (days=3) → RespawnRuntime

**Scene Placement:** Wilderness areas, cleared zones, mine floors

---

## Animal — Chicken

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Animator` (idle, eat, product-ready animations)
- `Collider2D` (trigger)
- State indicator icon (bubble above head)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `ChickenEntityData`

**Modules:**
- ✅ `AnimalModule` (feedItem="chicken_feed", productItem="egg", hoursToProduct=12, maxDaysNotFed=3) → AnimalRuntime

**Scene Placement:** Farm scene — inside ChickenCoop building area

---

## Animal — Cow

**Components:** Same structure as Chicken (larger sprite)

**EntityData:** `CowEntityData`

**Modules:**
- ✅ `AnimalModule` (feedItem="hay", productItem="milk", hoursToProduct=24, maxDaysNotFed=3) → AnimalRuntime

**Scene Placement:** Farm scene — inside CowBarn building area

---

## Animal — Sheep

**Components:** Same structure as Chicken

**EntityData:** `SheepEntityData`

**Modules:**
- ✅ `AnimalModule` (feedItem="hay", productItem="wool", hoursToProduct=48, maxDaysNotFed=3) → AnimalRuntime

**Scene Placement:** Farm scene — inside SheepPen building area

---

## Building — ChickenCoop

**Components:**
- `EntityRoot`
- `SpriteRenderer` (multi-tile sprite)
- `Collider2D` (solid — blocks movement except entrance)
- `BuildingAreaMarker` (MonoBehaviour — defines animal containment area)

**Required References:**
- `EntityRoot`: EntityData reference
- `BuildingAreaMarker`: area bounds

**EntityData:** `ChickenCoopEntityData`

**Modules:**
- 🆕 `BuildingModule` (size=2×2, capacity=4, animalType=Chicken) → BuildingRuntime

**Scene Placement:** Farm scene — placed by player via BuildingPlacementRuntime

---

## Building — CowBarn

**Components:** Same as ChickenCoop (larger)

**EntityData:** `CowBarnEntityData`

**Modules:**
- 🆕 `BuildingModule` (size=3×3, capacity=2, animalType=Cow) → BuildingRuntime

**Scene Placement:** Farm scene — placed by player

---

## Building — SheepPen

**Components:** Same as ChickenCoop

**EntityData:** `SheepPenEntityData`

**Modules:**
- 🆕 `BuildingModule` (size=2×3, capacity=3, animalType=Sheep) → BuildingRuntime

**Scene Placement:** Farm scene — placed by player

---

## Sprinkler

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `StageObject` (receives DayChangedPublish → forwards NextDayEvent)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `BasicSprinklerData`, `AdvancedSprinklerData`, `PremiumSprinklerData`

**Modules:**
- 🆕 `SprinklerModule` (radius=1/2/3, pattern=cross/3×3) → SprinklerRuntime

**Scene Placement:** Farm scene — placed by player on plowed tiles

---

## Drop Item

**Components:**
- `EntityRoot`
- `SpriteRenderer` (item icon)
- `Collider2D` (trigger — pickup on contact)
- `DropMotionObject` (MonoBehaviour — scatter animation on spawn)
- `PickUpObject` (MonoBehaviour — auto-pickup when player enters trigger)

**Required References:**
- `DropMotionObject`: scatter force, gravity
- `PickUpObject`: pickup delay, player layer

**EntityData:** Any item EntityData (ore, wood, crop, monster drop, etc.)

**Modules:** None (drop items are simple — just EntityData + Amount)

**Scene Placement:** Spawned dynamically by DropRuntime at entity death positions

---

## Portal (Scene Transition)

**Components:**
- `EntityRoot`
- `SpriteRenderer` (portal visual)
- `Collider2D` (trigger)
- Optional: particle effect (glow)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `MineEntrancePortalData`, `FloorExitPortalData`, `FarmReturnPortalData`

**Modules:**
- ✅ `ScenePortalModule` (targetScene, spawnPointId) → ScenePortalRuntime

**Scene Placement:**
- Farm scene: Mine entrance portal
- Mine scene: Floor entry/exit portals per floor
- Any scene: Return-to-farm portals

---

## Bed

**Components:**
- `EntityRoot`
- `SpriteRenderer`
- `Collider2D` (trigger)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `BedEntityData`

**Modules:**
- 🆕 `BedModule` (earlyRestorePercent=1.0, lateRestorePercent=0.75, lateHourThreshold=24) → BedRuntime
- ✅ `DialogueModule` (confirm dialogue "Đi ngủ?") → DialogueRuntime

**Scene Placement:** Farm scene — player house interior

---

## Lore Item

**Components:**
- `EntityRoot`
- `SpriteRenderer` (glowing/special visual)
- `Collider2D` (trigger)

**Required References:**
- `EntityRoot`: EntityData reference

**EntityData:** `LoreItem_MutationOrigin`, `LoreItem_LabNotes`, etc.

**Modules:**
- 🆕 `LoreItemModule` (loreId, dialogueGraphId) → LoreItemRuntime
- ✅ `MortalModule` → MortalRuntime (destroy after pickup)

**Scene Placement:** Cleared zones — spawned after zone clear or by NarrativeService

---

## ClearZone (Zone Tracker)

**Components:**
- `ClearZoneTracker` (MonoBehaviour — track targets in zone)
- `Collider2D` (trigger area — defines zone bounds)
- `SpriteRenderer` or Tilemap (barrier visual — fence/rocks)
- Child objects: enemy spawn points, obstacle spawn points

**Required References:**
- `ClearZoneTracker`: zoneId, totalTargets (auto-counted from children), expReward
- Barrier visual references (to disable on clear)

**EntityData:** None (ClearZoneTracker is MonoBehaviour, not entity-based)

**Modules:** N/A

**Scene Placement:** Farm scene edges — blocked areas with enemies + obstacles inside
