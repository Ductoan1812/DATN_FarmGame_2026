# 05 — EVENT CATALOG (All Events in System)

> Mỗi event: Type → Struct → Payload → Publisher → Subscribers → When Fired

---

## TIME EVENTS

### ✅ GameHourChangedPublish
- **Type:** Global/EventBus
- **Payload:** `int hour`
- **Publisher:** TimeManager
- **Subscribers:** AnimalRuntime (product timer), AIAssistantService (refresh tips), ExhaustionHandler (02:00 check), NPC schedules, DayNightLightController (indirect via NormalizedTime)
- **When fired:** Every game hour change (≈42 real seconds)

### ✅ DayChangedPublish
- **Type:** Global/EventBus
- **Payload:** `int day, Season season`
- **Publisher:** TimeManager (via AdvanceToNextDay)
- **Subscribers:** StageRuntime (crop growth), WateredTileTracker (reset), AnimalRuntime (hunger reset), RespawnRegistry (resource respawn), WeatherSystem (generate weather), ResearchService (countdown), NarrativeService (story check), DailyTracker (reset), QuestService (daily quests), AIAssistantService (refresh)
- **When fired:** After sleep or forced exhaustion — start of new day

### ✅ SeasonChangedPublish
- **Type:** Global/EventBus
- **Payload:** `Season newSeason`
- **Publisher:** TimeManager
- **Subscribers:** Crop availability checks, Visual palette changes, CalendarUI
- **When fired:** Every 28 days (season transition)

---

## BOOT EVENTS

### ✅ SaveGameRequestPublish
- **Type:** Global/EventBus
- **Payload:** (none)
- **Publisher:** PlayerControler (F5 key), SettingsWindowUI
- **Subscribers:** SaveSystem
- **When fired:** Player presses F5 or clicks Save in settings

### ✅ LoadGameRequestPublish
- **Type:** Global/EventBus
- **Payload:** `int slotId` (optional)
- **Publisher:** SettingsWindowUI, MainMenuUI
- **Subscribers:** SaveSystem
- **When fired:** Player clicks Load

### ✅ SpawnRequestPublish
- **Type:** Global/EventBus
- **Payload:** `Vector2 position, ObjectType objectType, EntityData entityData`
- **Publisher:** PlacementRuntime, DropRuntime, BuildingPlacementRuntime, RespawnRegistry, NarrativeService
- **Subscribers:** SpawnSystem
- **When fired:** Any entity needs to be instantiated in world

### ✅ DestroyEntityRequestPublish
- **Type:** Global/EventBus
- **Payload:** `string entityId, Vector2 position`
- **Publisher:** MortalRuntime
- **Subscribers:** SpawnSystem (destroy GO), WorldEntityService (unregister), ClearZoneTracker (zone progress)
- **When fired:** Entity HP reaches 0 and MortalRuntime handles DieEvent

### ✅ SpawnedEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime entity`
- **Publisher:** SpawnSystem (after instantiation)
- **Subscribers:** StageRuntime (init sprite), StatsRuntime (init stats), InventoryRuntime (init slots)
- **When fired:** After entity prefab instantiated and EntityRoot initialized

---

## INVENTORY EVENTS

### ✅ InventoryChangedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime owner, InventoryType type`
- **Publisher:** InventoryService (after any slot change)
- **Subscribers:** HotbarUI, BackpackUI, EquipmentUI, ShopPanelUI (refresh sell list), CraftingPanelUI (refresh ingredients)
- **When fired:** After Pickup, Transfer, Consume, Remove, Sort, Swap

### ✅ HotbarSelectionChangedPublish
- **Type:** Global/EventBus
- **Payload:** `int slotIndex, EntityRuntime itemEntity`
- **Publisher:** InventoryRuntime (Hotbar) via PlayerInventory
- **Subscribers:** HotbarUI (highlight), PlayerVisual (weapon sprite)
- **When fired:** Player presses 1-8 or scrolls

### ✅ EquipmentChangedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime owner, EquipSlot slot, EntityRuntime item`
- **Publisher:** EquipmentRuntime
- **Subscribers:** EquipmentUI, AppearanceRuntime, Stats recalculation
- **When fired:** After equip or unequip

---

## COMBAT EVENTS

### ✅ PrimaryActionEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime actor, EntityRuntime item` (item = null on first dispatch)
- **Publisher:** PlayerControler → EntityRuntime.TriggerEvent
- **Subscribers:** ActionRuntime (route), then forwarded to: ToolRuntime, WeaponRuntime, PlacementRuntime
- **When fired:** Mouse left click

### ✅ SecondaryActionEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime actor, EntityRuntime target, InteractionContext context`
- **Publisher:** PlayerControler → EntityRuntime.TriggerEvent → ActionRuntime forwards to target
- **Subscribers:** DialogueRuntime, ShopRuntime, QuestRuntime, AnimalRuntime, HarvestRuntime, ScenePortalRuntime, BedRuntime, CraftingRuntime, LoreItemRuntime, HerbPickupRuntime
- **When fired:** E key or mouse right click

### ✅ AnimStrikeEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime actor, EntityRuntime item`
- **Publisher:** ToolActionBridge (on animation strike frame)
- **Subscribers:** ToolRuntime.Execute(), WeaponRuntime.Execute(), PlacementRuntime.Execute()
- **When fired:** During tool/weapon animation at the "hit" frame

### ✅ TakeDamageEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime attacker, float damage, ToolType toolType`
- **Publisher:** DamageToolRuntime, WeaponRuntime, EnemyAttackRuntime
- **Subscribers:** HealthRuntime (target), HarvestRuntime (gate check)
- **When fired:** After AnimStrikeEvent execution finds valid target

### ✅ DieEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime entity, EntityRuntime killer`
- **Publisher:** HealthRuntime (HP <= 0), HarvestRuntime (hand harvest), StageRuntime (wilt death)
- **Subscribers:** DropRuntime, MortalRuntime, RespawnRuntime, ExpRewardRuntime, PlayerDeathHandler, AnimalRuntime (death), QualityRuntime
- **When fired:** Entity HP reaches 0 or explicit death trigger

### 🆕 PlayerDamagedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime player, float currentHP, float maxHP, float damage`
- **Publisher:** HealthRuntime (player)
- **Subscribers:** HealthBarUI (update bar), PlayerDamageVisual (flash red)
- **When fired:** Player takes damage (after i-frame check passes)

### 🆕 PlayerRespawnedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime player`
- **Publisher:** PlayerDeathHandler
- **Subscribers:** HUD (reset bars), Camera (reposition)
- **When fired:** After player death → respawn at home

---

## FARMING EVENTS

### ✅ NextDayEvent
- **Type:** Module/EntityRuntime
- **Payload:** `EntityRuntime entity`
- **Publisher:** StageObject (MonoBehaviour, subscribes DayChangedPublish, forwards to EntityRuntime)
- **Subscribers:** StageRuntime (growth), SprinklerRuntime (auto-water), AnimalRuntime (daily reset)
- **When fired:** Start of each new day, per entity

### 🆕 WeatherChangedPublish
- **Type:** Global/EventBus
- **Payload:** `WeatherType weather`
- **Publisher:** WeatherSystem
- **Subscribers:** HudStatusMapUI (icon), WateredTileTracker (auto-water if rain), DayNightLightController (darken), RainParticleSystem (enable/disable)
- **When fired:** After GenerateWeather() on new day

### 🆕 ZoneClearedPublish
- **Type:** Global/EventBus
- **Payload:** `string zoneId`
- **Publisher:** ClearZoneTracker
- **Subscribers:** ProgressionService (grant EXP), QuestService (objective check), WorldEntityService (unlock tiles), NarrativeService (story trigger), MinimapUI (reveal area)
- **When fired:** All targets in a clear zone destroyed

---

## SHOP EVENTS

### ✅ ShopViewPublish
- **Type:** Global/EventBus
- **Payload:** `ShopViewData viewData`
- **Publisher:** ShopService.Open()
- **Subscribers:** ShopPanelUI
- **When fired:** Player selects "Shop" interaction option

### ✅ ShopTransactionResultPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime customer, EntityRuntime merchant, ShopTransactionResult result`
- **Publisher:** ShopService (after TryBuy/TrySell)
- **Subscribers:** ShopPanelUI (refresh), DailyTracker (track income), NotificationUI (success/fail message)
- **When fired:** After every buy/sell attempt (success or fail)

---

## QUEST EVENTS

### ✅ QuestViewPublish
- **Type:** Global/EventBus
- **Payload:** `QuestViewData viewData`
- **Publisher:** QuestService.ShowQuest()
- **Subscribers:** QuestPanelUI
- **When fired:** Player views quest details

### ✅ QuestStateChangedPublish
- **Type:** Global/EventBus
- **Payload:** `string playerId, string questId, QuestState state`
- **Publisher:** QuestService (AcceptQuest, CompleteQuest)
- **Subscribers:** QuestLogWindowUI, QuestPanelUI, UnlockService (quest-gated unlocks), NarrativeService
- **When fired:** Quest accepted or completed

---

## NARRATIVE EVENTS

### 🆕 StoryEventTriggeredPublish
- **Type:** Global/EventBus
- **Payload:** `string eventId`
- **Publisher:** NarrativeService
- **Subscribers:** QuestService (activate quests), FeatureFlagService (enable features), SpawnSystem (spawn entities)
- **When fired:** Story event conditions met on new day

### 🆕 PhaseTransitionPublish
- **Type:** Global/EventBus
- **Payload:** `int newPhase`
- **Publisher:** NarrativeService
- **Subscribers:** FeatureFlagService, SpawnSystem, UI notifications
- **When fired:** Phase transition conditions met

### 🆕 DiaryEntryPublish
- **Type:** Global/EventBus
- **Payload:** `string text, int day`
- **Publisher:** NarrativeService
- **Subscribers:** DiaryUI (show popup)
- **When fired:** Story event with diary delivery, or milestone reached

### 🆕 NewMessagePublish
- **Type:** Global/EventBus
- **Payload:** `string sender, string preview, string fullText`
- **Publisher:** NarrativeService (via MessageService)
- **Subscribers:** MessageNotificationUI (badge + sound)
- **When fired:** Story event with message delivery

### 🆕 NewsBroadcastPublish
- **Type:** Global/EventBus
- **Payload:** `string text, int priority`
- **Publisher:** NarrativeService
- **Subscribers:** NewsBroadcastUI (banner)
- **When fired:** Story event with news delivery

---

## PROGRESSION EVENTS

### ✅ ProgressionChangedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime target, ExpSourceType source, EntityRuntime sourceEntity, int amount, int oldLevel, int newLevel, int exp, int maxExp`
- **Publisher:** ProgressionService.GrantExp()
- **Subscribers:** PlayerInfoHUDUI (EXP bar), DailyTracker (track EXP)
- **When fired:** After any EXP grant (regardless of level up)

### ✅ LevelUpPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime target, int newLevel`
- **Publisher:** ProgressionService (inside GrantExp loop)
- **Subscribers:** UnlockService (check unlock table), PlayerInfoHUDUI (celebration), AIAssistantService (check new tips), NotificationUI ("Level Up!")
- **When fired:** Each time a level is gained (can fire multiple times per GrantExp call)

### 🆕 UnlockCompletedPublish
- **Type:** Global/EventBus
- **Payload:** `string unlockId, UnlockType type` (Recipe/Feature/Area)
- **Publisher:** UnlockService
- **Subscribers:** RecipeRegistry (if recipe), NotificationUI, CraftingPanelUI (refresh)
- **When fired:** After successful unlock (by level, material, gold, or quest)

### 🆕 ResearchStartedPublish
- **Type:** Global/EventBus
- **Payload:** `string researchId, int daysRequired`
- **Publisher:** ResearchService
- **Subscribers:** ResearchUI (show progress), NotificationUI
- **When fired:** Player starts a research project

### 🆕 ResearchCompletedPublish
- **Type:** Global/EventBus
- **Payload:** `string researchId, string unlockedRecipeId`
- **Publisher:** ResearchService
- **Subscribers:** RecipeRegistry (unlock), NotificationUI ("Research complete!")
- **When fired:** Research timer reaches 0

---

## UI EVENTS

### ✅ InteractionOptionsReadyPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime actor, EntityRuntime target, List<InteractionOptionRuntime> options`
- **Publisher:** ActionRuntime (when multiple options collected)
- **Subscribers:** InteractionMenuUI
- **When fired:** SecondaryAction on target with multiple interaction modules

### ✅ CraftingViewPublish
- **Type:** Global/EventBus
- **Payload:** `CraftingViewData viewData`
- **Publisher:** CraftingService.Open()
- **Subscribers:** CraftingPanelUI
- **When fired:** Player selects "Craft" interaction option

### ✅ CraftingResultPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime crafter, RecipeData recipe, CraftingResult result`
- **Publisher:** CraftingService.TryCraft()
- **Subscribers:** CraftingPanelUI (refresh), ProgressionService (grant EXP on success), NotificationUI
- **When fired:** After craft attempt

### ✅ DialogueViewPublish
- **Type:** Global/EventBus
- **Payload:** `DialogueViewData viewData`
- **Publisher:** DialogueService
- **Subscribers:** DialoguePanelUI
- **When fired:** Dialogue node displayed

### ✅ DialogueEndPublish
- **Type:** Global/EventBus
- **Payload:** (none)
- **Publisher:** DialogueService
- **Subscribers:** DialoguePanelUI (close), HUDManager (show HUD)
- **When fired:** Dialogue graph ends or player skips

### 🆕 AITipChangedPublish
- **Type:** Global/EventBus
- **Payload:** `string tipText`
- **Publisher:** AIAssistantService
- **Subscribers:** AIAssistantUI
- **When fired:** New tip generated (hourly refresh or urgent update)

### 🆕 AnimalProductCollectedPublish
- **Type:** Global/EventBus
- **Payload:** `string animalType, string productId`
- **Publisher:** AnimalRuntime
- **Subscribers:** ProgressionService (small EXP), DailyTracker
- **When fired:** Player collects product from animal

### 🆕 AnimalDiedPublish
- **Type:** Global/EventBus
- **Payload:** `string animalType, string buildingId`
- **Publisher:** AnimalRuntime
- **Subscribers:** BuildingRuntime (decrement count), NotificationUI (warning)
- **When fired:** Animal dies from starvation (daysNotFed >= 3)

### 🆕 BuffAppliedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime target, BuffType type, float value, float durationMinutes`
- **Publisher:** BuffRuntime
- **Subscribers:** HUD (show buff icon), Stats recalculation
- **When fired:** Potion consumed, buff applied

### 🆕 BuffExpiredPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime target, BuffType type`
- **Publisher:** BuffRuntime
- **Subscribers:** HUD (remove icon), Stats recalculation (revert)
- **When fired:** Buff timer expires

### 🆕 StaminaChangedPublish
- **Type:** Global/EventBus
- **Payload:** `EntityRuntime target, float current, float max`
- **Publisher:** Stats system (after stamina spend/restore)
- **Subscribers:** HudStatusMapUI (stamina bar)
- **When fired:** After any stamina change (tool use, dodge, food, sleep)
