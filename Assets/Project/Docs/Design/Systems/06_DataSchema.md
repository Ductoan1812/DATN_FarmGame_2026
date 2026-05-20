# 06 — DATA SCHEMA (ScriptableObjects & SaveData)

> Mỗi type: Fields → Relationships → Created by → Used by

---

## SCRIPTABLE OBJECTS

---

### ✅ EntityData

**Description:** Base config for any entity in the game (items, NPCs, enemies, plants, buildings, tools).

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique identifier (e.g., "iron_pickaxe", "tomato_seed") |
| keyName | string | Localization key for display name |
| icon | Sprite | Inventory/UI icon |
| category | EntityCategory | Item/Tool/Weapon/Seed/Crop/Ore/Wood/Herb/Food/Equipment/Building/Animal/MonsterDrop/Lore |
| maxStack | int | Max stack size (1 = unstackable, 99 = full stack) |
| sellPrice | int | Base sell price (0 = unsellable) |
| buyPrice | int | Base buy price (0 = unbuyable) |
| prefab | GameObject | World prefab reference (for spawning) |
| modules | List\<IModuleData\> | Module configs attached to this entity |
| baseStats | StatBlock | Base stat values (HP, Attack, Defense, etc.) |

**Relationships:**
- Referenced by: EntityRuntime (runtime instance), RecipeData (ingredients/outputs), ShopModule (stock), DropModule (drops)
- Contains: IModuleData[] (ToolModule, WeaponModule, PlacementModule, etc.)

**Created by:** Editor (manual creation per item/entity type)

**Used by:** EntityService.Create(), SpawnSystem, ShopService, CraftingService, InventoryService

---

### ✅ TimeConfig

**Description:** Configuration for time system — day length, seasons, hours.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| realMinutesPerDay | float | Real minutes per game day (14) |
| dayStartHour | int | Hour day begins (6 = 06:00) |
| dayEndHour | int | Hour forced sleep (26 = 02:00 next day) |
| daysPerSeason | int | Days per season (28) |
| seasons | Season[] | Season order (Spring, Summer, Fall, Winter) |

**Relationships:**
- Referenced by: TimeManager

**Created by:** Editor (single asset)

**Used by:** TimeManager (core time calculations)

---

### ✅ TileData

**Description:** References to tile assets used by farming system.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| plowedTile | TileBase | Tile visual for plowed ground |
| wateredTile | TileBase | 🔧 Tile visual for watered ground |
| grassTile | TileBase | Default grass tile |
| dirtTile | TileBase | Base dirt tile |

**Relationships:**
- Referenced by: HoeRuntime, WateringCanRuntime, WorldEntityService, WateredTileTracker

**Created by:** Editor (single asset, references Unity Tile assets)

**Used by:** HoeRuntime.Execute(), WateringCanRuntime.Execute(), WateredTileTracker.ResetAll()

---

### 🆕 WeatherConfig

**Description:** Weather probability configuration.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| sunnyChance | float | Probability of sunny weather (0.6) |
| rainyChance | float | Probability of rain (0.3) |
| stormyChance | float | Probability of storm (0.1) |
| seasonModifiers | SeasonWeatherMod[] | Optional per-season probability adjustments |

**Relationships:**
- Referenced by: WeatherSystem

**Created by:** Editor (single asset)

**Used by:** WeatherSystem.GenerateWeather()

---

### 🆕 MasteryUnlockData

**Description:** Defines what unlocks at each mastery level.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| entries | UnlockEntry[] | Array of level-gated unlocks |

**UnlockEntry fields:**
| Field | Type | Description |
|-------|------|-------------|
| level | int | Required mastery level |
| unlockType | UnlockType | Recipe / Feature / Area |
| targetId | string | ID of recipe/feature/area to unlock |
| description | string | Display text for notification |

**Relationships:**
- Referenced by: ProgressionService, UnlockService

**Created by:** Editor (single asset, manually configured)

**Used by:** ProgressionService.OnLevelUp() → UnlockService.CheckUnlockTable()

---

### 🆕 ResearchData

**Description:** Configuration for a single research project.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| researchId | string | Unique research identifier |
| nameKey | string | Localization key for research name |
| descriptionKey | string | Localization key for description |
| ingredients | IngredientEntry[] | Required materials to start |
| goldCost | int | Gold cost to start |
| daysRequired | int | Game days to complete |
| outputRecipeId | string | Recipe ID unlocked on completion |
| requiredMastery | int | Minimum mastery level to access |

**IngredientEntry fields:**
| Field | Type | Description |
|-------|------|-------------|
| entityDataId | string | Required item EntityData ID |
| amount | int | Required quantity |

**Relationships:**
- Referenced by: ResearchService, ResearchNPCRuntime
- References: EntityData (ingredients), RecipeData (output)

**Created by:** Editor (one per research project)

**Used by:** ResearchService.StartResearch(), ResearchService.OnNewDay()

---

### 🆕 StoryEventData

**Description:** Configuration for a single narrative story event.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| eventId | string | Unique event identifier |
| priority | int | Execution priority (higher = first) |
| conditions | StoryCondition | Trigger conditions |
| actions | StoryAction[] | Actions to execute when triggered |

**StoryCondition fields:**
| Field | Type | Description |
|-------|------|-------------|
| dayRequirement | int | Minimum day (0 = no requirement) |
| questRequired | string | Quest ID that must be completed (null = none) |
| masteryRequired | int | Minimum mastery level (0 = none) |
| phaseRequired | int | Minimum narrative phase (0 = none) |

**StoryAction fields:**
| Field | Type | Description |
|-------|------|-------------|
| actionType | StoryActionType | ShowDiary / SendMessage / ShowNews / UnlockFeature / SpawnEntity / ActivateQuest |
| textContent | string | Text for diary/message/news (Vietnamese) |
| targetId | string | Feature/entity/quest ID for unlock/spawn/activate |
| position | Vector2 | Spawn position (for SpawnEntity) |

**Relationships:**
- Referenced by: NarrativeService
- References: EntityData (spawn), QuestGraphData (activate), FeatureFlags (unlock)

**Created by:** Editor (one per story event, ~20-30 total)

**Used by:** NarrativeService.OnNewDay(), NarrativeService.CheckPhaseTransition()

---

### ✅ StarterLoadoutData

**Description:** Initial items granted to player on new game.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| items | LoadoutItem[] | Items to grant |
| startingGold | int | Initial gold amount |
| startingStamina | int | Initial max stamina |

**LoadoutItem fields:**
| Field | Type | Description |
|-------|------|-------------|
| entityData | EntityData | Item to create |
| amount | int | Quantity |
| equipSlot | EquipSlot? | Auto-equip to slot (null = inventory) |

**Relationships:**
- References: EntityData (items to grant)

**Created by:** Editor (single asset)

**Used by:** StarterLoadoutService.Apply()

---

### ✅ RecipeData

**Description:** Crafting recipe configuration.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| recipeId | string | Unique recipe identifier |
| nameKey | string | Localization key |
| category | RecipeCategory | ToolUpgrade / Food / Equipment / Building / FarmEquipment / Fertilizer / Potion |
| ingredients | RecipeIngredient[] | Required materials |
| outputs | RecipeOutput[] | Produced items |
| masteryRequired | int | Minimum mastery to see/craft |
| unlocked | bool | Default unlock state (true = always available) |

**RecipeIngredient fields:**
| Field | Type | Description |
|-------|------|-------------|
| entityData | EntityData | Required item |
| amount | int | Required quantity |

**RecipeOutput fields:**
| Field | Type | Description |
|-------|------|-------------|
| entityData | EntityData | Output item |
| amount | int | Output quantity |

**Relationships:**
- Referenced by: CraftingService, CraftingModule, ResearchData (output)
- References: EntityData (ingredients + outputs)

**Created by:** Editor (one per recipe, ~50-100 total)

**Used by:** CraftingService.TryCraft(), CraftingService.BuildView(), UnlockService

---

## SAVE DATA TYPES

---

### ✅ EntitySaveData

**Description:** Serialized state of a single EntityRuntime.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| id | string | Runtime unique ID |
| entityDataId | string | Reference to EntityData asset |
| amount | int | Current stack amount |
| ownerId | string | ID of owning container entity |
| ownerSlotIndex | int | Slot index in owner's inventory |
| stats | StatSaveData | All stat key-value pairs |
| modules | ModuleSaveData[] | Per-module save data |

**Created by:** EntityService.SaveData()

**Used by:** EntityService.LoadData() → EntityRuntime.LoadFromSave()

---

### ✅ ModuleSaveData

**Description:** Base serialized state for any module runtime.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| moduleType | string | Type name of the module |
| jsonData | string | Serialized module-specific data |

**Created by:** Each ModuleRuntime.ToSaveData()

**Used by:** EntityRuntime.ApplySaveData() → ModuleRuntime.ApplySaveData()

---

### ✅ StageModuleSave

**Description:** Save data for StageRuntime (crop growth state).

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| currentStageIndex | int | Current growth stage (0-based) |
| daysInCurrentStage | int | Days spent in current stage |
| daysWithoutWater | int | 🔧 Consecutive days not watered |
| fertilized | bool | 🔧 Whether fertilizer applied |
| daysWatered | int | 🆕 Total days watered (for quality calc) |

**Created by:** StageRuntime.ToSaveData()

**Used by:** StageRuntime.ApplySaveData()

---

### ✅ InventorySaveData

**Description:** Save data for InventoryRuntime (slot contents).

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| inventoryType | InventoryType | Hotbar / Backpack / Equipment / MerchantStock |
| slotEntityIds | string[] | Entity IDs in each slot (null = empty) |
| selectedIndex | int | Currently selected slot (hotbar only) |
| slotCount | int | Total slot count |

**Created by:** InventoryRuntime.ToSaveData()

**Used by:** InventoryRuntime.ApplySaveData() + RestoreSlots()

---

### ✅ QuestLogSaveData

**Description:** Save data for QuestLogRuntime (quest states).

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| questStates | QuestStatePair[] | Array of {questId, state} |

**QuestStatePair fields:**
| Field | Type | Description |
|-------|------|-------------|
| questId | string | Quest identifier |
| state | QuestState | NotStarted / InProgress / Completed |

**Created by:** QuestLogRuntime.ToSaveData()

**Used by:** QuestLogRuntime.ApplySaveData()

---

### ✅ TimeSaveData

**Description:** Save data for TimeManager state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| currentDay | int | Current game day |
| currentHour | int | Current hour (6-26) |
| currentMinute | int | Current minute (0-59) |
| currentSeason | Season | Current season enum |
| elapsedRealTime | float | Elapsed real seconds in current day |

**Created by:** TimeManager.Save()

**Used by:** TimeManager.Load()

---

### 🆕 WeatherSaveData

**Description:** Save data for WeatherSystem state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| currentWeather | WeatherType | Today's weather (Sunny/Rainy/Stormy) |
| tomorrowWeather | WeatherType | Pre-rolled forecast for tomorrow |
| randomSeed | int | RNG seed for reproducibility (optional) |

**Created by:** WeatherSystem.Save()

**Used by:** WeatherSystem.Load()

---

### 🆕 WateredTileSaveData

**Description:** Save data for WateredTileTracker state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| wateredCells | Vector2IntArray | All currently watered cell positions |

**Vector2IntArray fields:**
| Field | Type | Description |
|-------|------|-------------|
| xs | int[] | X coordinates |
| ys | int[] | Y coordinates |

**Created by:** WateredTileTracker.Save()

**Used by:** WateredTileTracker.Load()

---

### 🆕 ClearZoneSaveData

**Description:** Save data for ClearZoneTracker state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| zones | ZoneStateSave[] | Per-zone state |

**ZoneStateSave fields:**
| Field | Type | Description |
|-------|------|-------------|
| zoneId | string | Zone identifier |
| isCleared | bool | Whether zone is fully cleared |
| remainingTargets | int | Targets still alive (if not cleared) |
| totalTargets | int | Original total targets |

**Created by:** ClearZoneTracker.Save()

**Used by:** ClearZoneTracker.Load()

---

### 🆕 ResearchSaveData

**Description:** Save data for ResearchService state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| activeResearchId | string | Currently researching (null if none) |
| daysRemaining | int | Days left on active research |
| completedResearchIds | string[] | All completed research IDs |

**Created by:** ResearchService.Save()

**Used by:** ResearchService.Load()

---

### 🆕 NarrativeSaveData

**Description:** Save data for NarrativeService state.

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| currentPhase | int | Current narrative phase (1-5) |
| triggeredEventIds | string[] | All triggered story event IDs |
| messages | MessageSave[] | Message inbox |
| diaryEntries | DiaryEntrySave[] | All diary entries |

**MessageSave fields:**
| Field | Type | Description |
|-------|------|-------------|
| messageId | string | Unique message ID |
| sender | string | Sender name |
| subject | string | Subject line |
| body | string | Full message text |
| daySent | int | Game day sent |
| isRead | bool | Read status |

**DiaryEntrySave fields:**
| Field | Type | Description |
|-------|------|-------------|
| entryId | string | Unique entry ID |
| text | string | Diary text content |
| dayTriggered | int | Game day triggered |

**Created by:** NarrativeService.Save()

**Used by:** NarrativeService.Load()

---

## MODULE DATA (IModuleData) SUMMARY

| Module | Key Fields | Creates Runtime |
|--------|-----------|-----------------|
| ✅ ActionModule | (none) | ActionRuntime |
| ✅ ToolModule | toolType, animTrigger | HoeRuntime/AxeRuntime/PickaxeRuntime/ScytheRuntime/WateringCanRuntime |
| ✅ WeaponModule | animTrigger, hitAll | WeaponRuntime |
| ✅ PlacementModule | objectType, placedEntityData, placementRule | PlacementRuntime |
| ✅ HealthModule | baseHP, requiredTool | HealthRuntime |
| ✅ DropModule | drops[] {entityData, chance, minAmount, maxAmount} | DropRuntime |
| ✅ MortalModule | (none) | MortalRuntime |
| ✅ RespawnModule | daysToRespawn | RespawnRuntime |
| ✅ ExpRewardModule | expAmount, sourceType | ExpRewardRuntime |
| ✅ InventoryModule | inventoryType, slotCount | InventoryRuntime |
| ✅ EquipmentModule | slots[] | EquipmentRuntime |
| ✅ StageModule | stages[], 🔧regrowable, regrowToStage, wiltSprite | StageRuntime |
| ✅ HarvestModule | harvestTool | HarvestRuntime |
| ✅ ShopModule | stockItems[], buyCategories[], sellCategories[] | ShopRuntime |
| ✅ DialogueModule | dialogueGraph | DialogueRuntime |
| ✅ QuestModule | questGraphs[] | QuestRuntime |
| ✅ QuestLogModule | (none) | QuestLogRuntime |
| ✅ CraftingModule | recipes[], recipeCategory | CraftingRuntime |
| ✅ AnimalModule | animalType, feedItemId, productItemId, hoursToProduct, maxDaysNotFed | AnimalRuntime |
| ✅ AppearanceModule | baseSprites, colorConfig | AppearanceRuntime |
| ✅ ScenePortalModule | targetScene, spawnPointId | ScenePortalRuntime |
| ✅ StatsModule | baseStats[] | StatsRuntime |
| ✅ AttackModule | (uses entity stats) | AttackRuntime |
| 🆕 QualityModule | maxQuality (3) | QualityRuntime |
| 🆕 SprinklerModule | radius, pattern (cross/3×3) | SprinklerRuntime |
| 🆕 FertilizerModule | fertilizerType (Speed/Quality), bonus | FertilizerRuntime |
| 🆕 BuildingModule | size (Vector2Int), capacity, animalType | BuildingRuntime |
| 🆕 BuildingPlacementModule | buildingSize, placedEntityData | BuildingPlacementRuntime |
| 🆕 BedModule | earlyRestorePercent, lateRestorePercent, lateHourThreshold | BedRuntime |
| 🆕 LoreItemModule | loreId, dialogueGraphId | LoreItemRuntime |
| 🆕 HerbPickupModule | (none — uses entity's EntityData for item grant) | HerbPickupRuntime |
| 🆕 ResearchNPCModule | availableResearch[] | ResearchNPCRuntime |
