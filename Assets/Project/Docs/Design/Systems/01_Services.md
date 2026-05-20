# 01 — SERVICES (System Architecture)

> Mỗi service: Responsibility → Owner → Public API → Events → Data Owned → Rules

---

## ✅ EntityService

**Responsibility:** CRUD entities, amount mutation, stack rules — single source of truth cho entity lifecycle.

**Owner:** `GameManager` (instance, injected `EntityRegistry`)

**Public API:**
```
SetAmount(EntityRuntime entity, int value) → bool (true if depleted)
    - Clamp value [0, MaxStack]
    - If result == 0 → Unregister from registry
    - ALL amount changes MUST go through this

AddAmount(EntityRuntime entity, int delta) → bool (true if depleted)
    - Calls SetAmount(entity, entity.Amount + delta)

CanStack(EntityRuntime a, EntityRuntime b) → bool [static]
    - Check: same entityData.id, maxStack > 1, same stats, same modules
    - Pure function, no mutation

Create(EntityData data, int amount = 1) → EntityRuntime
    - Instantiate new EntityRuntime
    - Register in EntityRegistry

Clone(EntityRuntime source) → EntityRuntime
    - Deep copy via ToSaveData → LoadFromSave
    - Register clone in registry

Split(EntityRuntime source, int splitAmount) → EntityRuntime
    - Remove splitAmount from source (may Unregister if depleted)
    - Create new entity with splitAmount

Merge(EntityRuntime dst, EntityRuntime src) → int (amount merged)
    - Check CanStack → transfer min(src.Amount, dst.FreeSpace)
    - May Unregister src if depleted

TryConsume(EntityRuntime entity, int amount) → bool (true if depleted)
    - AddAmount(entity, -amount)

Destroy(EntityRuntime entity) → void
    - Remove from Owner container
    - Unregister from registry

Move(EntityRuntime entity, IEntityContainer target) → void
    - Remove from current Owner, Add to target

Get(string id) → EntityRuntime
GetAll() → IEnumerable<EntityRuntime>

SaveData(string filename, bool saveToFile = true) → void
LoadData(Func<string, EntityData> resolver, string filename, bool fromFile = true) → void
RestoreAllInventories() → void
```

**Events Published:** None directly (mutations are synchronous, callers publish)

**Events Subscribed:** None

**Data Owned:** EntityRegistry (all live EntityRuntime instances)

**Rules:**
- ANY code that changes entity Amount MUST call SetAmount/AddAmount
- No direct writes to entity.Amount outside EntityService
- InventoryService, ShopService, CraftingService all go through EntityService for mutations

---

## ✅ InventoryService

**Responsibility:** Pickup, transfer, consume, sort, swap — manages entity placement within inventory containers.

**Owner:** `GameManager` (instance, injected `EntityService`)

**Public API:**
```
Pickup(EntityRuntime pickupEntity, EntityRuntime receiverEntity) → int (amount received)
    - Try merge into existing stacks first
    - Then place in first empty slot
    - Returns amount actually picked up

Transfer(EntityRuntime entity, EntityRuntime fromEntity, EntityRuntime toEntity, int amount = -1) → int
    - Move entity (or partial) from one owner to another
    - Handles split if partial amount

Consume(EntityRuntime entity, EntityRuntime ownerEntity, int amount = 1) → bool
    - EntityService.TryConsume wrapper with ownership validation

Remove(EntityRuntime entity, EntityRuntime ownerEntity) → bool
    - Remove entity from owner's inventory entirely

Remove(string entityDataId, int amount, EntityRuntime ownerEntity) → bool
    - Remove by data ID + amount (searches all inventories)

Sort(EntityRuntime ownerEntity, InventoryType inventoryType) → void
    - Sort inventory slots by category/name

SwapSlots(EntityRuntime ownerA, InventoryType typeA, int slotA,
          EntityRuntime ownerB, InventoryType typeB, int slotB) → void
    - Swap two inventory slots (same or different owners)

Contains(EntityRuntime ownerEntity, EntityRuntime entity) → bool
CanReceive(EntityRuntime receiverEntity, EntityRuntime entity, int amount = -1) → int
CountEntity(EntityRuntime ownerEntity, string entityDataId) → int
GetInventory(EntityRuntime ownerEntity, InventoryType type) → InventoryRuntime
```

**Events Published:**
- `InventoryChangedPublish` (via PublishChanged — after any mutation)

**Events Subscribed:** None

**Data Owned:** None (operates on EntityRuntime.InventoryRuntime slots)

**Rules:**
- Only service allowed to move entities between inventory containers
- UI must NOT directly modify inventory slots — always go through InventoryService
- ShopService, CraftingService call InventoryService for item grants

---

## ✅ ShopService

**Responsibility:** Buy/sell transactions — validate stock, gold, inventory space, execute trade.

**Owner:** Static class (no instance needed)

**Public API:**
```
Open(EntityRuntime customer, EntityRuntime merchant, ShopModule shop) → void
    - Build ShopViewData
    - Publish ShopViewPublish for UI

TryBuy(EntityRuntime customer, EntityRuntime merchant, EntityRuntime stockItem, int amount) → ShopTransactionResult
    - Check: merchant has stock (stockItem.Amount >= amount)
    - Check: customer has gold (stats.Money >= totalPrice)
    - Check: customer has inventory space
    - Mutate: customer.Money -= price, stockItem.Amount -= amount, customer inventory += item
    - Publish: ShopTransactionResultPublish

TryBuy(EntityRuntime customer, EntityRuntime merchant, ShopItemViewData stockItem, int amount) → ShopTransactionResult
    - Overload for view-data based purchase

TryBuyInfinite(EntityRuntime customer, EntityRuntime merchant, EntityData itemData, int amount) → ShopTransactionResult
    - Buy from infinite stock (no merchant depletion)
    - Check: gold, inventory space
    - Create new entity, grant to customer

TrySell(EntityRuntime seller, EntityRuntime merchant, EntityRuntime item, int amount, ShopModule shop) → ShopTransactionResult
    - Check: merchant accepts item category (CanMerchantBuy)
    - Check: seller has item amount
    - Mutate: seller inventory -= item, seller.Money += sellPrice
    - Publish: ShopTransactionResultPublish

CanMerchantBuy(ShopModule shop, EntityRuntime item) → bool
CanMerchantBuy(ShopModule shop, EntityData itemData) → bool
```

**Events Published:**
- `ShopViewPublish` (when Open called)
- `ShopTransactionResultPublish` (after buy/sell)

**Events Subscribed:** None

**Data Owned:** None (reads ShopModule on merchant entity)

**Rules:**
- All gold changes go through ShopService (or direct stats.Set for quest rewards)
- UI calls TryBuy/TrySell, never directly modifies gold
- Static — no state, pure transaction logic

---

## ✅ QuestService

**Responsibility:** Quest state management — accept, complete, validate objectives, grant rewards.

**Owner:** Static class

**Public API:**
```
CreateInteractionOption(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph, int priority) → InteractionOptionRuntime
    - Check visibility requirement (UnlockService)
    - Return appropriate option based on quest state (offer/view/complete)

AcceptQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph) → bool
    - Set state InProgress on QuestLogRuntime
    - Publish QuestStateChangedPublish
    - Show quest dialogue

CompleteQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph) → bool
    - Check CanComplete (all objectives met)
    - Consume required items from inventory
    - Set state Completed
    - ApplyRewards (money, EXP, items)
    - Publish QuestStateChangedPublish

CanComplete(EntityRuntime player, QuestGraphData graph) → bool
    - Check all objectives: player has required items/amounts

ShowQuest(EntityRuntime player, EntityRuntime questOwner, QuestGraphData graph) → void
    - Build QuestViewData
    - Publish QuestViewPublish
```

**Events Published:**
- `QuestStateChangedPublish(playerId, questId, state)`
- `QuestViewPublish(QuestViewData)`

**Events Subscribed:** None

**Data Owned:** None (reads QuestLogRuntime on player, QuestGraphData assets)

**Rules:**
- Quest state lives on player's QuestLogRuntime module
- Only QuestService may transition quest states
- Rewards granted via ProgressionService.GrantExp + InventoryService

---

## ✅ DialogueService

**Responsibility:** Dialogue graph traversal — navigate nodes, evaluate conditions, execute actions.

**Owner:** Static class

**Public API:**
```
StartDialogue(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph) → void
    - Find start node
    - Publish DialogueViewPublish with first node content

AdvanceDialogue(EntityRuntime speaker, EntityRuntime listener, string choiceId) → void
    - Navigate to next node based on choice
    - Evaluate conditions on edges
    - Execute node actions (grant item, set flag, etc.)
    - Publish DialogueViewPublish or DialogueEndPublish

EndDialogue() → void
    - Publish DialogueEndPublish
```

**Events Published:**
- `DialogueViewPublish(DialogueViewData)`
- `DialogueEndPublish`

**Events Subscribed:** None

**Data Owned:** Current dialogue state (transient, not saved)

**Rules:**
- Max 64 node hops (prevent infinite loops)
- Conditions evaluated via UnlockService/InventoryService
- No gameplay logic in dialogue — actions are simple (grant item, set flag)

---

## ✅ CraftingService

**Responsibility:** Recipe validation, ingredient consumption, output granting.

**Owner:** `GameManager` (instance, injected EntityService + InventoryService + EventBus)

**Public API:**
```
Open(EntityRuntime crafter, EntityRuntime station, IReadOnlyList<RecipeData> recipes) → void
    - Build CraftingViewData
    - Publish CraftingViewPublish

TryCraft(EntityRuntime crafter, RecipeData recipe, int times = 1) → CraftingResult
    - Check: HasIngredients (all ingredients × times)
    - Check: CanReceiveOutputs (inventory space for outputs)
    - Mutate: ConsumeIngredients → GrantOutputs
    - Publish: CraftingResultPublish
    - Returns: CraftingResult(success, failReason, timesCrafted)

BuildView(EntityRuntime crafter, EntityRuntime station, IReadOnlyList<RecipeData> recipes) → CraftingViewData
    - Build recipe list with ingredient status (has/needs)
```

**Events Published:**
- `CraftingViewPublish(CraftingViewData)`
- `CraftingResultPublish(crafter, recipe, result)`

**Events Subscribed:** None

**Data Owned:** None (reads RecipeData assets, operates on inventories)

**Rules:**
- CraftingService does NOT grant EXP — caller (CraftingRuntime) calls ProgressionService separately
- Recipe unlock state managed by UnlockService/RecipeRegistry
- UI calls TryCraft, never directly consumes ingredients

---

## ✅ WorldEntityService

**Responsibility:** Spatial management — tile operations, entity position tracking, placement validation.

**Owner:** `GameManager` (instance, injected SpatialEntityRegistry + TileRegistry)

**Public API:**
```
SetGround(Vector2Int cell, TileBase tile) → void
    - Change ground tilemap tile at cell

SetGroundByName(Vector2Int cell, string tileName) → void
GetGround(Vector2Int cell) → TileBase

TryRegisterSpawn(EntityPosition ep, PlacementRule rule) → SpawnResult
    - Validate placement (no blocker, correct layer)
    - Register in SpatialEntityRegistry

ForceRegisterSpawn(EntityPosition ep) → void
TryUnregister(string idRuntime) → bool
MoveEntity(string idRuntime, Vector2 newPos, Vector2Int[] newCells = null) → bool
UpdateEntityId(string oldId, string newId) → void

CanPlaceAt(PlacementRule rule, Vector2Int cell, out string reason) → bool
HasBlockerAt(Vector2Int cell, EntityLayer layer) → bool
IsTillable(Vector2Int cell) → bool

GetEntitiesAt(Vector2Int cell) → IEnumerable<string>
GetEntityPosition(string idRuntime) → EntityPosition
HasPersistentId(string persistentId) → bool
FindByPersistentId(string persistentId) → EntityPosition

BuildOccupiedCells(Vector2Int origin, EntityRuntime runtime) → Vector2Int[] [static]

Save(string filename) → void
Load(string filename, Action<EntityPosition> onEntityLoaded = null) → void
```

**Events Published:** None directly

**Events Subscribed:** None directly (called by runtimes)

**Data Owned:** SpatialEntityRegistry (position → entity mapping), TileRegistry (tile state)

**Rules:**
- All tile changes go through WorldEntityService
- All entity spatial registration goes through TryRegisterSpawn
- HoeRuntime, WateringCanRuntime, PlacementRuntime all call WorldEntityService

---

## 🔧 ProgressionService

**Responsibility:** EXP/Mastery tracking, level up, stat growth — single progression API.

**Owner:** `GameManager` (instance, injected EventBus)

**Public API:**
```
RequiredExp(int level) → int [static]
    - Formula: 100 + 30*level + 4*level² (rounded to 10s)

EnsureInitialized(EntityRuntime target) → void
    - Set Level=1, MaxExp, Exp=0 if missing

GrantExp(EntityRuntime target, int amount, ExpSourceType source, EntityRuntime sourceEntity = null) → bool
    - Add EXP, check level up loop
    - Per level: ApplyLevelUpStats (+MaxHP, +MaxStamina, +Attack/Defense)
    - Publish: LevelUpPublish per level gained
    - Publish: ProgressionChangedPublish
    - Returns: true if leveled up

🔧 GrantMasteryExp(EntityRuntime target, int amount, ExpSourceType source) → bool [RENAME]
    - Same as GrantExp but semantically "Mastery" focused
    - EXP sources weighted: farming=high, combat=low

🆕 GetCurrentLevel(EntityRuntime target) → int
    - Read stats.Get(StatType.Level)

🆕 CheckUnlockTable(int newLevel) → List<UnlockEntry>
    - Return unlocks available at this level
    - Called by OnLevelUp to trigger UnlockService
```

**Events Published:**
- `LevelUpPublish(target, newLevel)`
- `ProgressionChangedPublish(target, source, sourceEntity, amount, oldLevel, newLevel, exp, maxExp)`

**Events Subscribed:** None

**Data Owned:** None (reads/writes target.stats: Level, Exp, MaxExp, MaxHp, MaxStamina, Attack, Defense)

**Rules:**
- ALL EXP grants go through ProgressionService.GrantExp
- Stat growth formula is internal — no external writes to Level/MaxExp
- 🔧 Needs: unlock table integration, mastery rename, weighted EXP sources

---

## 🔧 UnlockService

**Responsibility:** Level/quest/material-based unlock checks — gate features, recipes, areas.

**Owner:** Static class (existing) → 🔧 needs expansion

**Public API:**
```
✅ IsUnlocked(EntityRuntime player, UnlockRequirement requirement) → bool
    - Check requirement type (level, quest, item, money)
    - Returns true if all conditions met

🆕 TryUnlock(string unlockId, EntityRuntime player) → bool
    - Check all requirements for unlockId
    - If material-based: consume materials via EntityService
    - If gold-based: spend gold via ShopService
    - Mark unlocked in player state
    - Publish: UnlockCompletedPublish

🆕 IsFeatureUnlocked(string featureId) → bool
    - Check FeatureFlag registry

🆕 UnlockFeature(string featureId) → void
    - Enable feature flag
    - Publish: UnlockCompletedPublish

🆕 UnlockRecipe(string recipeId) → void
    - Mark recipe as available in RecipeRegistry
```

**Events Published:**
- `UnlockCompletedPublish(unlockId, type)` 🆕

**Events Subscribed:**
- `LevelUpPublish` → check unlock table for new level
- `QuestStateChangedPublish` → check quest-gated unlocks

**Data Owned:** Unlock state (which unlocks are active), FeatureFlags

**Rules:**
- UnlockService is the ONLY gate for feature/recipe availability
- CraftingService checks UnlockService before showing recipes
- 🔧 Needs: material consumption path, gold unlock path, feature flag system

---

## ✅ InteractionPreviewService

**Responsibility:** Preview data for UI hints — show interaction options before player commits.

**Owner:** Static class

**Public API:**
```
CreateQuestOption(EntityRuntime player, EntityRuntime npc, QuestGraphData graph, int priority) → InteractionOptionRuntime
CreateShopOption(EntityRuntime player, EntityRuntime npc, ShopModule shop, int priority) → InteractionOptionRuntime
CreateCraftOption(EntityRuntime player, EntityRuntime npc, CraftingModule craft, int priority) → InteractionOptionRuntime
CreateDialogueOption(EntityRuntime player, EntityRuntime npc, DialogueModule dialogue, int priority) → InteractionOptionRuntime
```

**Events Published:** None
**Events Subscribed:** None
**Data Owned:** None

**Rules:**
- Pure helper — builds InteractionOptionRuntime objects
- Called by various module runtimes during SecondaryActionEvent handling

---

## ✅ StarterLoadoutService

**Responsibility:** Initial items for new player — bootstrap inventory on new game.

**Owner:** Static class

**Public API:**
```
Apply(EntityRuntime player, StarterLoadoutData loadout, EntityService entityService, InventoryService inventoryService) → void
    - Create each item from loadout.items[]
    - Pickup into player inventory
    - Set initial stats (gold, stamina, etc.)
```

**Events Published:** None
**Events Subscribed:** None
**Data Owned:** None (reads StarterLoadoutData asset)

**Rules:**
- Called once on new game start
- Does NOT check inventory space (assumes empty)

---

## ✅ SceneTransitionService

**Responsibility:** Scene loading with spawn points — manage scene transitions and player positioning.

**Owner:** Static class (nested in GameManager)

**Public API:**
```
RequestTransition(string sceneName, string spawnPointId) → void
    - Store pending spawn point
    - Load target scene (async)

GetPendingSpawnPoint() → string
    - Return and clear pending spawn point ID

ClearPending() → void
```

**Events Published:** None directly (scene load triggers Unity events)

**Events Subscribed:** None

**Data Owned:** `pendingSpawnPointId` (transient between scenes)

**Rules:**
- ScenePortalRuntime calls RequestTransition
- Target scene's SpawnPointManager reads GetPendingSpawnPoint on load
- Only 1 pending transition at a time

---

## ✅ WateredTileTracker

**Responsibility:** Track ô đã tưới trong ngày — dùng Tilemap riêng (`tmWatered`) làm source of truth. Reset mỗi đầu ngày.

**Owner:** `GameManager` (instance, khởi tạo trong `InitWateredTileTracker()`)

**Implementation:** Dùng Tilemap riêng thay vì HashSet. Tilemap `tmWatered` nằm trên `tmGround`, chỉ chứa `wateredTile`. Check = `GetTile() != null`. Reset = `ClearAllTiles()`.

**Public API:**
```
SetWatered(Vector2Int cell) → void
    - _tmWatered.SetTile(cell, wateredTile)
    - Đặt tile visual lên tilemap watered layer

IsWatered(Vector2Int cell) → bool
    - return _tmWatered.GetTile(cell) != null
    - Có tile = đã tưới, null = chưa tưới

ResetAll() → void
    - _tmWatered.ClearAllTiles()
    - Xóa toàn bộ tiles — 1 dòng, O(1)

WaterAllPlowedCells() → void
    - Iterate tmGround bounds → tìm cells có plowedTile
    - Foreach: SetWatered(cell)
    - Dùng khi trời mưa (WeatherSystem)

GetWateredCount() → int
    - Iterate tmWatered bounds, đếm cells có tile
    - Dùng cho AI Assistant / UI
```

**Events Published:** None

**Events Subscribed:**
- `DayChangedPublish` → `ResetAll()` (subscribe trong GameManager.InitWateredTileTracker)

**Data Owned:** Tilemap `tmWatered` (visual = data, không cần struct riêng)

**Rules:**
- WateringCanRuntime, SprinklerRuntime, WeatherSystem ghi via `SetWatered()`
- StageRuntime, AIAssistantService đọc via `IsWatered()`
- KHÔNG CẦN save/load — reset mỗi ngày, save xảy ra sau khi ngủ (đã reset)
- Tilemap `tmWatered` phải nằm TRÊN `tmGround` trong sorting order

---

## 🆕 SoilQualityTracker

**Responsibility:** Quality per cell — track and modify soil quality affecting crop growth/quality.

**Owner:** `GameManager` or singleton service

**Public API:**
```
GetQuality(Vector2Int cell) → int (1-3, default 1)
SetQuality(Vector2Int cell, int value) → void
IncrementQuality(Vector2Int cell) → void
    - Clamp to [1, 3]
    - Called when fertilizer used repeatedly

GetGrowthBonus(Vector2Int cell) → float
    - quality 1 → 0, quality 2 → 0.25, quality 3 → 0.5

Save() → Dictionary<Vector2Int, int>
Load(Dictionary<Vector2Int, int> data) → void
```

**Events Published:** None
**Events Subscribed:** None (called directly by FertilizerRuntime)

**Data Owned:** `Dictionary<Vector2Int, int> soilQuality`

**Rules:**
- FertilizerRuntime writes (increment on repeated use)
- StageRuntime reads (growth speed bonus)
- QualityRuntime reads (harvest quality bonus)

---

## 🆕 WeatherSystem

**Responsibility:** Random weather generation, forecast, current state.

**Owner:** Singleton MonoBehaviour or service on GameManager

**Public API:**
```
GenerateWeather() → void
    - Random roll: Sunny(60%), Rainy(30%), Stormy(10%)
    - Set currentWeather
    - Pre-roll tomorrow's weather (for forecast)
    - Publish WeatherChangedPublish

GetCurrent() → WeatherType
GetTomorrow() → WeatherType (pre-rolled forecast)
GetSeason() → Season (read from TimeManager)

Save() → WeatherSaveData
Load(WeatherSaveData data) → void
```

**Events Published:**
- `WeatherChangedPublish(WeatherType weather)` 🆕

**Events Subscribed:**
- `DayChangedPublish` → GenerateWeather() (called by sleep/exhaustion flow)

**Data Owned:** `currentWeather`, `tomorrowWeather`, WeatherConfig reference

**Rules:**
- Generated once per day (during AdvanceToNextDay)
- WateredTileTracker subscribes to auto-water on rain
- AIAssistantService reads GetTomorrow() for forecast tips
- DayNightLightController reads GetCurrent() for lighting adjustment

---

## 🆕 ClearZoneTracker

**Responsibility:** Track zone clear progress — count remaining targets, detect completion.

**Owner:** MonoBehaviour per zone OR centralized service

**Public API:**
```
RegisterZone(string zoneId, int totalTargets) → void
OnEntityDestroyed(string zoneId) → void
    - Decrement remaining count
    - If remaining == 0 → MarkCleared(zoneId)

IsCleared(string zoneId) → bool
GetProgress(string zoneId) → (int remaining, int total)

MarkCleared(string zoneId) → void
    - Publish ZoneClearedPublish
    - Grant Mastery EXP
    - Unlock adjacent farmland

Save() → ClearZoneSaveData
Load(ClearZoneSaveData data) → void
```

**Events Published:**
- `ZoneClearedPublish(string zoneId)` 🆕

**Events Subscribed:**
- `DestroyEntityRequestPublish` → check if destroyed entity belongs to a zone

**Data Owned:** `Dictionary<string, ZoneState>` (zoneId → remaining/total/cleared)

**Rules:**
- Enemies in cleared zones do NOT respawn
- WorldEntityService converts cleared zone tiles to tillable
- QuestService can have "clear zone X" objectives

---

## 🆕 ResearchService

**Responsibility:** Research queue, timer countdown, recipe unlock on completion.

**Owner:** `GameManager` or singleton service

**Public API:**
```
GetAvailableResearch() → List<ResearchData>
    - Filter: not yet completed, requirements visible

StartResearch(string researchId, EntityRuntime player) → bool
    - Check: no active research (max 1 slot)
    - Check: player has materials + gold
    - Consume materials + gold
    - Set activeResearch = {researchId, daysRemaining}
    - Publish ResearchStartedPublish

GetActiveResearch() → (string researchId, int daysRemaining)?
    - Return current research or null

OnNewDay() → void
    - If activeResearch: daysRemaining--
    - If daysRemaining <= 0:
        → UnlockService.UnlockRecipe(outputRecipeId)
        → activeResearch = null
        → Publish ResearchCompletedPublish

Save() → ResearchSaveData
Load(ResearchSaveData data) → void
```

**Events Published:**
- `ResearchStartedPublish(researchId, daysRequired)` 🆕
- `ResearchCompletedPublish(researchId, unlockedRecipeId)` 🆕

**Events Subscribed:**
- `DayChangedPublish` → OnNewDay()

**Data Owned:** `activeResearch` (id + daysRemaining), `completedResearch` set

**Rules:**
- Max 1 active research at a time
- Materials consumed on START (not completion)
- Scholar NPC provides UI access

---

## 🆕 AIAssistantService

**Responsibility:** Generate rule-based hints/tips from game state — not real AI.

**Owner:** Singleton service

**Public API:**
```
GetCurrentTip() → string
    - Return highest priority tip currently available

RefreshTips() → void
    - Recalculate all tips based on current game state
    - Publish AITipChangedPublish if tip changed

GenerateWateringTip() → string?
GenerateWeatherTip() → string?
GenerateCropTip() → string?
GenerateAnimalTip() → string?

GetAvailableTipTypes() → List<TipType>
    - Filtered by current mastery level
```

**Events Published:**
- `AITipChangedPublish(string tipText)` 🆕

**Events Subscribed:**
- `GameHourChangedPublish` → RefreshTips()
- `DayChangedPublish` → RefreshTips()
- `LevelUpPublish` → check new tip unlocks

**Data Owned:** Current tip text, tip priority queue

**Rules:**
- Unlock gated by Mastery level (L2=watering, L3=weather, L5=crops, L8=animals)
- Reads from: WateredTileTracker, WeatherSystem, ShopService, WorldEntityService
- Does NOT mutate any game state — read-only analysis
- Priority: urgent (watering) > informational (weather) > suggestion (crops)

---

## 🆕 NarrativeService

**Responsibility:** Story events, phase transitions — check conditions daily, trigger narrative delivery.

**Owner:** Singleton service

**Public API:**
```
OnNewDay(int currentDay) → void
    - Check all pending StoryEventData conditions
    - Trigger matched events (mark as triggered)
    - Check phase transition conditions

TriggerEvent(StoryEventData eventData) → void
    - Execute actions: ShowDiary, SendMessage, ShowNews, UnlockFeature, SpawnEntity
    - Mark triggered (never repeat)

GetCurrentPhase() → int
CheckPhaseTransition() → void
    - Evaluate phase conditions (day + quest + mastery)
    - If transition: execute phase actions

IsEventTriggered(string eventId) → bool

Save() → NarrativeSaveData
Load(NarrativeSaveData data) → void
```

**Events Published:**
- `StoryEventTriggeredPublish(string eventId)` 🆕
- `PhaseTransitionPublish(int newPhase)` 🆕
- `DiaryEntryPublish(string text)` 🆕
- `NewMessagePublish(string sender, string preview)` 🆕
- `NewsBroadcastPublish(string text)` 🆕

**Events Subscribed:**
- `DayChangedPublish` → OnNewDay()

**Data Owned:** `triggeredEvents` set, `currentPhase`, message inbox

**Rules:**
- Events trigger ONCE only (persisted in save)
- Phase transitions are sequential (1→2→3→4→5)
- Does not block gameplay — all delivery is non-modal (popup/notification)

---

## 🆕 DailyTracker

**Responsibility:** Track daily metrics for end-of-day summary — reset each morning.

**Owner:** Singleton service

**Public API:**
```
TrackIncome(int amount) → void
TrackExpGained(int amount) → void
TrackCropsGrown(int count) → void
TrackItemsSold(int count) → void
TrackEnemiesKilled(int count) → void

GetSummary() → DailySummaryData
    - Return all tracked metrics for today

Reset() → void
    - Clear all counters (called on new day start)
```

**Events Published:** None

**Events Subscribed:**
- `ShopTransactionResultPublish` → TrackIncome (sell transactions)
- `ProgressionChangedPublish` → TrackExpGained
- `DayChangedPublish` → Reset()

**Data Owned:** Daily counters (income, exp, crops, items, kills)

**Rules:**
- EndOfDaySummaryUI reads GetSummary() before Reset
- Reset happens at START of new day (after summary shown)
- Not saved (transient per-day data)
