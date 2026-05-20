# 02 — RUNTIMES (Module Runtime Architecture)

> Mỗi runtime: Created by → Events Handled → Logic Summary → Dependencies → State → Save/Load

---

## ✅ ActionRuntime

**Created by:** `ActionModule` (on Player EntityData)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- PrimaryAction: Get hotbar selected item → forward PrimaryActionEvent to item entity
- SecondaryAction: EntityScanSystem.GetClosest() → create InteractionContext → forward to target → collect options → publish InteractionOptionsReadyPublish if multiple
- If no item held (primary): forward to self (unarmed)
- If no target (secondary): do nothing

**Dependencies:** InventoryRuntime (hotbar), EntityScanSystem, InteractionContext, EventBus

**State:** None (stateless router)

**Save/Load:** Nothing to save

---

## ✅ AttackRuntime (EnemyAttackRuntime)

**Created by:** `AttackModule` (on Enemy EntityData)

**Events Handled:**
- `IHandleEvent<AnimStrikeEvent>` (from EnemyObject animation)

**Logic Summary:**
- On AnimStrike: get attack damage from stats → find player in range → TakeDamageEvent to player
- Damage = entity.stats.Get(Attack)
- ToolType = None (enemy melee)

**Dependencies:** EntityScanSystem, target HealthRuntime

**State:** None

**Save/Load:** Nothing

---

## ✅ WeaponRuntime

**Created by:** `WeaponModule` (on weapon EntityData)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- PrimaryAction: Validate (stamina check) → ToolActionBridge.Request(animTrigger)
- AnimStrike: EntityScanSystem.GetAll(range) → filter enemies → foreach: TakeDamageEvent
- Spend stamina after execution
- Hit all targets in range (AoE melee)

**Dependencies:** ToolActionBridge, EntityScanSystem, target HealthRuntime, stats (Stamina)

**State:** None

**Save/Load:** Nothing

---

## ✅ ToolRuntime (Base)

**Created by:** `ToolModule` (abstract base for all tool runtimes)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- PrimaryAction: Validate() [subclass] → if OK → ToolActionBridge.Request()
- AnimStrike: Execute() [subclass] → perform tool action + spend stamina
- Template method pattern: Validate → Request → Execute

**Dependencies:** ToolActionBridge, GridSystem, stats (Stamina)

**State:** None

**Save/Load:** Nothing

---

## ✅ HoeRuntime

**Created by:** `ToolModule` (toolType = Hoe)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>` (via ToolRuntime)
- `IHandleEvent<AnimStrikeEvent>` (via ToolRuntime)

**Logic Summary:**
- Validate: GetCellInFrontOf → check IsTillable + no blocker + plowedTile exists
- Execute: WorldEntityService.SetGround(cell, plowedTile) → spend stamina (4)

**Dependencies:** GridSystem, WorldEntityService, TileData, ToolActionBridge

**State:** None

**Save/Load:** Nothing (tile state saved by WorldEntityService)

---

## ✅ ScytheRuntime

**Created by:** `ToolModule` (toolType = Scythe)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>` (via DamageToolRuntime)
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: always true (miss = no target)
- Execute: scan targets in range → TakeDamageEvent with toolType=Scythe
- Used for: harvesting crops (via HarvestRuntime gate), cutting grass

**Dependencies:** EntityScanSystem, ToolActionBridge, target HealthRuntime

**State:** None

**Save/Load:** Nothing

---

## ✅ AxeRuntime

**Created by:** `ToolModule` (toolType = Axe)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>` (via DamageToolRuntime)
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: always true
- Execute: scan targets → TakeDamageEvent with toolType=Axe
- Targets: Tree entities (HealthRuntime checks requiredTool=Axe)

**Dependencies:** EntityScanSystem, ToolActionBridge, target HealthRuntime

**State:** None

**Save/Load:** Nothing

---

## ✅ PickaxeRuntime

**Created by:** `ToolModule` (toolType = Pickaxe)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>` (via DamageToolRuntime)
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: always true
- Execute: scan targets → TakeDamageEvent with toolType=Pickaxe
- Targets: Rock/Ore entities (HealthRuntime checks requiredTool=Pickaxe)

**Dependencies:** EntityScanSystem, ToolActionBridge, target HealthRuntime

**State:** None

**Save/Load:** Nothing

---

## ✅ PlacementRuntime

**Created by:** `PlacementModule` (on seed/placeable EntityData)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: GetCellInFrontOf → PlacementValidator.CanPlace(entityData, cell) → check plantable tag + no blocker
- Execute: Publish SpawnRequestPublish → SpawnSystem instantiates → EntityService.AddAmount(seed, -1) → spend stamina (1)

**Dependencies:** GridSystem, PlacementValidator, SpawnSystem, EntityService, WorldEntityService

**State:** None

**Save/Load:** Nothing (spawned entity saved by WorldEntityService)

---

## ✅ HarvestRuntime

**Created by:** `HarvestModule` (on plant EntityData)

**Events Handled:**
- `IHandleEvent<TakeDamageEvent>`
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- TakeDamage: check harvestTool matches → check IsHarvestable (stage.canHarvest) → enable HealthRuntime damage
- SecondaryAction: check harvestTool == None (hand harvest) → check IsHarvestable → trigger DieEvent
- Gates damage: only allows HP reduction when harvest conditions met
- 🔧 Regrow: if regrowable → reset StageRuntime instead of MortalRuntime

**Dependencies:** StageRuntime (check stage), HealthRuntime (enable damage), QualityRuntime 🆕

**State:** None

**Save/Load:** Nothing

---

## ✅ StageRuntime

**Created by:** `StageModule` (on plant EntityData)

**Events Handled:**
- `IHandleEvent<NextDayEvent>`
- `IHandleEvent<SpawnedEvent>`

**Logic Summary:**
- SpawnedEvent: init sprite to stage[0]
- NextDayEvent: check WateredTileTracker.IsWatered(cell)
  - YES: daysInCurrentStage++ → if >= daysToGrow → advance stage → update sprite
  - NO: daysWithoutWater++ → if 1 → wilt sprite → if >=2 → DieEvent
- 🔧 Fertilized bonus: daysInCurrentStage += extra
- 🔧 Regrow support: reset to regrowToStage on harvest

**Dependencies:** WateredTileTracker, SoilQualityTracker (optional), SpriteRenderer

**State:** `currentStageIndex`, `daysInCurrentStage`, `daysWithoutWater`, `fertilized`

**Save/Load:**
- ToSaveData: {currentStageIndex, daysInCurrentStage, daysWithoutWater, fertilized}
- ApplySaveData: restore all + set correct sprite

---

## ✅ HealthRuntime

**Created by:** `HealthModule` (on any damageable entity)

**Events Handled:**
- `IHandleEvent<TakeDamageEvent>`

**Logic Summary:**
- Check requiredTool (if set, only matching toolType deals damage)
- Check isInvincible (i-frames)
- Calculate finalDamage = damage - defense (min 1)
- currentHP -= finalDamage
- Set i-frames timer
- If HP <= 0 → trigger DieEvent
- Publish PlayerDamagedPublish (if player)

**Dependencies:** Stats (Defense), EquipmentRuntime (defense bonus)

**State:** `currentHP`, `maxHP`, `isInvincible`, `iFrameTimer`

**Save/Load:**
- ToSaveData: {currentHP, maxHP}
- ApplySaveData: restore HP values

---

## ✅ DropRuntime

**Created by:** `DropModule` (on any entity that drops items on death)

**Events Handled:**
- `IHandleEvent<DieEvent>`

**Logic Summary:**
- On DieEvent: iterate drops[] → random chance check → random quantity → SpawnRequestPublish per drop
- Drop items scatter with DropMotionObject animation
- 🔧 Quality: set quality on drop item from QualityRuntime (if plant)

**Dependencies:** SpawnSystem (via EventBus), DropModule config

**State:** None

**Save/Load:** Nothing

---

## ✅ MortalRuntime

**Created by:** `MortalModule` (on any destroyable entity)

**Events Handled:**
- `IHandleEvent<DieEvent>`

**Logic Summary:**
- On DieEvent: publish DestroyEntityRequestPublish → entity removed from world
- Skipped if entity is regrowable and harvest triggered the die (HarvestRuntime handles)

**Dependencies:** SpawnSystem, WorldEntityService

**State:** None

**Save/Load:** Nothing

---

## ✅ RespawnRuntime

**Created by:** `RespawnModule` (on resources that respawn)

**Events Handled:**
- `IHandleEvent<DieEvent>`

**Logic Summary:**
- On DieEvent: record {position, entityDataId, dayDestroyed, daysToRespawn}
- Register in RespawnRegistry
- On DayChanged (via registry): countdown → if ready → SpawnRequestPublish at original position

**Dependencies:** TimeManager (DayChangedPublish), SpawnSystem, RespawnRegistry

**State:** Respawn entry data (position, timer)

**Save/Load:**
- ToSaveData: {entityDataId, position, dayDestroyed, daysToRespawn}
- ApplySaveData: re-register in RespawnRegistry

---

## ✅ ExpRewardRuntime

**Created by:** `ExpRewardModule` (on enemies, resources)

**Events Handled:**
- `IHandleEvent<DieEvent>`

**Logic Summary:**
- On DieEvent: get killer from event → ProgressionService.GrantExp(killer, expAmount, source)
- Source type derived from entity category (Combat, Mining, Gathering)

**Dependencies:** ProgressionService

**State:** None

**Save/Load:** Nothing

---

## ✅ InventoryRuntime

**Created by:** `InventoryModule` (on player, NPCs, chests)

**Events Handled:**
- `IHandleEvent<SpawnedEvent>` (init slots)

**Logic Summary:**
- Manages slot array (EntityRuntime[] slots)
- Provides: Add, Remove, GetSlot, SetSlot, GetFirstEmpty, FindEntity
- Hotbar variant: tracks selectedIndex, publishes HotbarSelectionChangedPublish

**Dependencies:** EntityService (for stack operations)

**State:** `slots[]` (EntityRuntime references), `selectedIndex` (hotbar), `inventoryType`

**Save/Load:**
- ToSaveData: {slotEntityIds[], selectedIndex, inventoryType}
- ApplySaveData: restore slot references via EntityRegistry lookup

---

## ✅ EquipmentRuntime

**Created by:** `EquipmentModule` (on player)

**Events Handled:**
- `IHandleEvent<EquipRequestEvent>`

**Logic Summary:**
- Equip: place item in equipment slot → apply stat bonuses to owner
- Unequip: remove from slot → revert stat bonuses → return to inventory
- Slots: Head, Body, Legs, Accessory, Weapon

**Dependencies:** InventoryService (return to inventory), Stats (apply bonuses)

**State:** `equipSlots[]` (EntityRuntime per slot)

**Save/Load:**
- ToSaveData: {slotEntityIds[]}
- ApplySaveData: restore equipment references + reapply stat bonuses

---

## ✅ DialogueRuntime

**Created by:** `DialogueModule` (on NPCs)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- On SecondaryAction: context.AddOption("dialogue", textKey, priority, callback)
- Callback: DialogueService.StartDialogue(speaker, listener, graph)

**Dependencies:** DialogueService, InteractionContext

**State:** None

**Save/Load:** Nothing

---

## ✅ ShopRuntime

**Created by:** `ShopModule` (on merchant NPCs)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- On SecondaryAction: context.AddOption("shop", textKey, priority, callback)
- Callback: ShopService.Open(customer, merchant, shopModule)

**Dependencies:** ShopService, InteractionContext

**State:** Shop stock (items in merchant inventory)

**Save/Load:** Merchant inventory saved as normal EntityRuntime

---

## ✅ QuestRuntime

**Created by:** `QuestModule` (on quest-giver NPCs)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- On SecondaryAction: QuestService.CreateInteractionOption() → add to context
- Handles multiple quests per NPC (iterate questGraphs[])

**Dependencies:** QuestService, InteractionContext, UnlockService

**State:** None (quest state on player's QuestLogRuntime)

**Save/Load:** Nothing

---

## ✅ QuestLogRuntime

**Created by:** `QuestLogModule` (on player)

**Events Handled:** None directly (called by QuestService)

**Logic Summary:**
- Stores quest states: Dictionary<string, QuestState>
- GetState(questId), SetState(questId, state)
- States: NotStarted, InProgress, Completed

**Dependencies:** None

**State:** `questStates` dictionary

**Save/Load:**
- ToSaveData: {questId → state} pairs
- ApplySaveData: restore dictionary

---

## ✅ CraftingRuntime

**Created by:** `CraftingModule` (on crafting NPCs/stations)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- On SecondaryAction: context.AddOption("craft", textKey, priority, callback)
- Callback: CraftingService.Open(crafter, station, recipes)

**Dependencies:** CraftingService, InteractionContext

**State:** None

**Save/Load:** Nothing

---

## ✅ AnimalRuntime

**Created by:** `AnimalModule` (on animal entities)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`
- `IHandleEvent<NextDayEvent>`
- `IHandleEvent<GameHourChangedEvent>` (product timer)

**Logic Summary:**
- SecondaryAction: check state
  - Hungry + player has feedItem → consume feed → SetState(Fed) → start timer
  - ProductReady → InventoryService.TryAdd(product) → SetState(Hungry)
- NextDayEvent: if Hungry → daysNotFed++ → if >=3 → DieEvent
  - if Fed → SetState(Hungry) (daily reset)
- GameHour: check timer → if elapsed → SetState(ProductReady)

**Dependencies:** InventoryService, EntityService, TimeManager, ProgressionService

**State:** `animalState` (Hungry/Fed/ProductReady), `daysNotFed`, `productTimer`

**Save/Load:**
- ToSaveData: {animalState, daysNotFed, productTimerHours}
- ApplySaveData: restore state + timer

---

## ✅ AppearanceRuntime

**Created by:** `AppearanceModule` (on player — HeroEditor4D integration)

**Events Handled:**
- `IHandleEvent<EquipmentChangedEvent>`

**Logic Summary:**
- On equipment change: update character sprite layers (weapon, armor, helmet visuals)
- Reads equipment slot data for sprite references

**Dependencies:** HeroEditor4D system, EquipmentRuntime

**State:** Current appearance config

**Save/Load:**
- ToSaveData: appearance settings (colors, base sprites)
- ApplySaveData: restore visual

---

## ✅ ScenePortalRuntime

**Created by:** `ScenePortalModule` (on portal entities)

**Events Handled:**
- `IHandleEvent<SecondaryActionEvent>`

**Logic Summary:**
- On SecondaryAction: context.AddOption("portal", textKey, priority, callback)
- Callback: SceneTransitionService.RequestTransition(targetScene, spawnPointId)

**Dependencies:** SceneTransitionService

**State:** None (config only: targetScene, spawnPointId)

**Save/Load:** Nothing

---

## ✅ StatsRuntime

**Created by:** `StatsModule` (on player, enemies — base stat initialization)

**Events Handled:**
- `IHandleEvent<SpawnedEvent>`

**Logic Summary:**
- On Spawned: initialize stats from EntityData base values (HP, MaxHP, Stamina, MaxStamina, Attack, Defense, etc.)
- Stats stored on EntityRuntime.stats (StatBlock)

**Dependencies:** None

**State:** StatBlock (all stat key-value pairs)

**Save/Load:**
- ToSaveData: full stat dictionary
- ApplySaveData: restore all stats

---

## 🆕 WateringCanRuntime

**Created by:** `ToolModule` (toolType = WateringCan)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>` (via ToolRuntime)
- `IHandleEvent<AnimStrikeEvent>` (via ToolRuntime)

**Logic Summary:**
- Validate: GetCellInFrontOf → check ground is plowed/watered OR has plant → check not already watered today
- Execute: WateredTileTracker.SetWatered(cell) → WorldEntityService.SetGround(cell, wateredTile) → spend stamina (2)

**Dependencies:** GridSystem, WateredTileTracker, WorldEntityService, TileData, ToolActionBridge

**State:** None

**Save/Load:** Nothing (watered state saved by WateredTileTracker)

---

## 🆕 FertilizerRuntime

**Created by:** `FertilizerModule` (on fertilizer items — extends ToolRuntime or PlacementRuntime pattern)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: GetCellInFrontOf → check has plant entity → check plant not already fertilized
- Execute: Set plant.StageRuntime.fertilized = true → SoilQualityTracker.IncrementQuality(cell) → EntityService.AddAmount(fertilizer, -1) → spend stamina (2)

**Dependencies:** GridSystem, WorldEntityService, StageRuntime (target plant), SoilQualityTracker, EntityService

**State:** None

**Save/Load:** Nothing (fertilized flag saved on StageRuntime)

---

## 🆕 QualityRuntime

**Created by:** `QualityModule` (on plant entities)

**Events Handled:**
- `IHandleEvent<DieEvent>` (calculate quality at harvest time)

**Logic Summary:**
- On DieEvent (harvest): calculate quality based on:
  - waterRatio = daysActuallyWatered / totalGrowDays
  - fertilized bonus (+1)
  - soilQuality bonus (+1 if quality >= 2)
  - quality = clamp(1 + bonuses + (ratio >= 0.8 ? 1 : 0), 1, 3)
- Set quality on drop items (affects sell price multiplier: ×1.0 / ×1.5 / ×2.0)

**Dependencies:** WateredTileTracker (historical), SoilQualityTracker, StageRuntime (fertilized flag)

**State:** `daysWatered` (incremented each day plant was watered)

**Save/Load:**
- ToSaveData: {daysWatered}
- ApplySaveData: restore counter

---

## 🆕 SprinklerRuntime

**Created by:** `SprinklerModule` (on sprinkler entities)

**Events Handled:**
- `IHandleEvent<NextDayEvent>`

**Logic Summary:**
- On NextDayEvent (early, before StageRuntime): get cells in configured pattern (cross/3×3)
- For each cell: WateredTileTracker.SetWatered(cell)
- Pattern determined by SprinklerModule config (T1=1 cell, T2=5 cross, T3=9 3×3)

**Dependencies:** WateredTileTracker, GridSystem

**State:** None (position is spatial, pattern is config)

**Save/Load:** Nothing (entity position saved by WorldEntityService)

---

## 🆕 BuildingPlacementRuntime

**Created by:** `BuildingPlacementModule` (on building items — extends PlacementRuntime)

**Events Handled:**
- `IHandleEvent<PrimaryActionEvent>`
- `IHandleEvent<AnimStrikeEvent>`

**Logic Summary:**
- Validate: GetMultiCellArea(buildingSize) → check ALL cells: no blocker, buildable
- Execute: SpawnRequestPublish(building prefab) → WorldEntityService.RegisterMultiCell → EntityService.AddAmount(buildingItem, -1)
- Building entity has capacity for animals

**Dependencies:** GridSystem, WorldEntityService, SpawnSystem, EntityService, PlacementValidator (multi-cell)

**State:** None

**Save/Load:** Nothing (building entity saved by WorldEntityService)
