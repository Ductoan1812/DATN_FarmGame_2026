# 📈 PROGRESSION SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** ProgressionSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Farming Mastery (EXP system)
> 2. Mastery level up
> 3. Unlock by material
> 4. Unlock by money
> 5. Unlock by quest
> 6. Stamina progression

---

## TỔNG QUAN

Progression dùng **Mastery** thay vì Level truyền thống. Mastery tăng chủ yếu từ farming/harvest/quest/craft/khai hoang. Combat cho rất ít EXP (game farming-focused, không phải RPG combat).

ProgressionService đã có sẵn `GrantExp()` (sẽ rename thành `GrantMasteryExp()`), hỗ trợ L1-50.

---

## 1. FARMING MASTERY

### Mô tả
Mỗi hành động farming (thu hoạch, craft, hoàn thành quest, khai hoang vùng mới) cho Mastery EXP. Combat cho rất ít (1-2 EXP per kill vs 10-20 per harvest). Mastery level quyết định unlock features, recipes, stats.

### Điều kiện
- Player thực hiện action cho EXP (harvest, craft, quest complete, zone clear)
- ProgressionService nhận GrantMasteryExp() call

### Flow
```
Các nguồn EXP → ProgressionService.GrantMasteryExp(player, amount, source):
    
    Sources & amounts:
        - Harvest crop:        10-20 EXP (tùy crop tier)
        - Craft item:          15-25 EXP (tùy recipe tier)
        - Complete quest:      50-200 EXP (tùy quest difficulty)
        - Clear zone:          100-300 EXP (tùy zone size)
        - Kill enemy:          1-3 EXP (rất ít, intentional)
        - Mine ore:            3-5 EXP
        - Chop tree:           2-4 EXP
        - Gather herb:         2 EXP
        - Sell crops (first time): 5 EXP bonus
    
    ProgressionService.GrantMasteryExp(player, amount, source):
        1. currentExp += amount
        2. Check level up: while currentExp >= expToNextLevel:
            → currentExp -= expToNextLevel
            → currentLevel++
            → OnLevelUp(currentLevel)
        3. EventBus.Publish(MasteryExpGainedPublish(amount, source, currentExp, currentLevel))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ProgressionService | Core EXP/level logic |
| ExpRewardRuntime | Trigger GrantMasteryExp on DieEvent |
| CraftingService | Grant EXP on craft |
| QuestService | Grant EXP on quest complete |
| ClearZoneTracker | Grant EXP on zone clear |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ProgressionConfig | ScriptableObject | expTable[1-50], expSources{} |
| ExpTable | | L1→L2: 100, L2→L3: 150, ..., L49→L50: 5000 (scaling curve) |

### UI
- EXP bar (HUD, dưới HP/Stamina)
- "+15 Mastery" popup khi gain EXP
- Level up celebration (flash + sound + message)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| MasteryExpGainedPublish | ProgressionService | HUD (update EXP bar) |
| MasteryLevelUpPublish | ProgressionService | UnlockSystem, StatsSystem, NotificationUI |

### Scene Objects
- HUD (EXP bar)
- Player entity (stats tracking)

---

## 2. MASTERY LEVEL UP

### Mô tả
Khi đủ EXP → level up. Mỗi level cho stat growth (chủ yếu +MaxStamina) và unlock recipes/features. Level 1-50, chia 5 tier bands.

### Điều kiện
- currentExp >= expToNextLevel
- currentLevel < 50

### Flow
```
ProgressionService.OnLevelUp(newLevel):
    1. Apply stat growth:
        - MaxStamina += staminaPerLevel (default +3)
        - MaxHP += hpPerLevel (default +2, nhỏ)
        - Stamina = MaxStamina (full restore on level up)
    2. Check unlock table:
        - unlockTable[newLevel] → list of unlocks
        - foreach unlock: RecipeRegistry.Unlock() / FeatureFlag.Enable()
    3. EventBus.Publish(MasteryLevelUpPublish(newLevel, unlocks[]))
    4. Save progression data

Unlock table (examples):
    L2:  Watering reminder (AI Assistant)
    L3:  Weather forecast (AI Assistant), Basic Sprinkler recipe
    L5:  Iron tool recipes, Advanced crops
    L8:  Animal system unlock (buy chickens)
    L10: Mine access, Combat basics
    L15: Gold tool recipes, Cow/Sheep
    L20: Potion crafting
    L25: Advanced equipment
    L30: Mythril recipes
    L40: Endgame content
    L50: Max mastery achievement
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ProgressionService | Level up logic |
| RecipeRegistry | Unlock recipes |
| EntityRuntime.stats | Apply stat growth |
| FeatureFlagService 🆕 | Enable/disable features by level |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ProgressionConfig | ScriptableObject | staminaPerLevel=3, hpPerLevel=2, unlockTable[] |
| UnlockEntry | | level, type(Recipe/Feature/Area), targetId |

### UI
- Level up popup: "Mastery Level 5! Unlocked: Iron Pickaxe recipe"
- Unlock notification list
- Stats screen showing growth

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| MasteryLevelUpPublish | ProgressionService | RecipeRegistry, FeatureFlagService, HUD, NotificationUI |

### Scene Objects
- HUD (level display, EXP bar)

---

## 3. UNLOCK BY MATERIAL

### Mô tả
Một số recipes/features yêu cầu player có đủ nguyên liệu để unlock (không phải craft — chỉ "show" rằng bạn có). Ví dụ: "Mang 10 Iron Ingot cho Blacksmith để unlock Gold tool recipes." Dùng CraftingService check.

### Điều kiện
- Player tương tác NPC có unlock requirement
- Player có đủ materials trong inventory
- Unlock chưa active

### Flow
```
SecondaryAction → NPC → UnlockNPCRuntime.Handle(SecondaryActionEvent):
    → context.AddOption("unlock", "Mở khóa công thức mới")
    → Player chọn → UnlockUI hiện requirements
    → Player confirm → UnlockService.TryUnlock(unlockId, player):
        1. Check requirements: InventoryService.HasAmount(player, material, count)
        2. if all met:
            → Consume materials: EntityService.AddAmount(material, -count)
            → RecipeRegistry.Unlock(recipeId) hoặc FeatureFlag.Enable(featureId)
            → EventBus.Publish(UnlockCompletedPublish(unlockId))
            → ProgressionService.GrantMasteryExp(player, unlockExp)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| UnlockService 🆕 | Check + consume + unlock |
| InventoryService | Check materials |
| RecipeRegistry | Unlock target |
| CraftingService | Ingredient checking (reuse) |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| UnlockData | ScriptableObject | unlockId, requirements[{materialId, count}], targetRecipeIds[], exp |

### UI
- UnlockUI panel: show requirements (green/red), Confirm button
- Notification: "Đã mở khóa: Gold Pickaxe recipe!"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | UnlockNPCRuntime |
| UnlockCompletedPublish 🆕 | UnlockService | RecipeRegistry, NotificationUI |

### Scene Objects
- NPC with UnlockNPCModule
- UnlockUI (Canvas panel)

---

## 4. UNLOCK BY MONEY

### Mô tả
Một số features/recipes unlock bằng gold. Ví dụ: mua license từ NPC, mở shop mới, unlock area. Dùng ShopService.SpendGold().

### Điều kiện
- Player tương tác NPC/object có gold unlock
- Player có đủ gold
- Unlock chưa active

### Flow
```
SecondaryAction → NPC/Object → GoldUnlockRuntime.Handle(SecondaryActionEvent):
    → context.AddOption("buy_unlock", "Mua giấy phép (500G)")
    → Player chọn → Confirm popup
    → ShopService.SpendGold(player, cost):
        1. Check gold >= cost → YES
        2. gold -= cost
        3. RecipeRegistry.Unlock(targetId) hoặc FeatureFlag.Enable(featureId)
        4. EventBus.Publish(UnlockCompletedPublish(unlockId))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ShopService | Check + spend gold |
| RecipeRegistry / FeatureFlagService | Unlock target |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| GoldUnlockData | ScriptableObject | unlockId, goldCost, targetId, description |

### UI
- Confirm popup: "Mua giấy phép khai hoang? 500G"
- Gold display update

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| UnlockCompletedPublish | GoldUnlockRuntime | RecipeRegistry, NotificationUI |

### Scene Objects
- NPC/Object with GoldUnlockModule

---

## 5. UNLOCK BY QUEST

### Mô tả
Hoàn thành quest cụ thể → unlock recipe/feature/area. QuestService completion triggers unlock. Ví dụ: "Hoàn thành quest 'Giúp Blacksmith' → unlock Iron Armor recipe."

### Điều kiện
- Quest completed (QuestService)
- Quest có unlockReward configured

### Flow
```
QuestService.CompleteQuest(questId):
    1. Grant rewards (items, gold, EXP)
    2. Check quest.unlockRewards[]:
        foreach unlock:
            → RecipeRegistry.Unlock(recipeId)
            → FeatureFlag.Enable(featureId)
    3. EventBus.Publish(QuestCompletedPublish(questId, rewards))
    4. EventBus.Publish(UnlockCompletedPublish(unlockId)) per unlock
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| QuestService | Quest completion |
| RecipeRegistry | Unlock recipes |
| FeatureFlagService | Enable features |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| QuestData | ScriptableObject | 🔧 thêm unlockRewards[{type, targetId}] |

### UI
- Quest complete popup: "Quest hoàn thành! Đã mở khóa: Iron Armor recipe"
- Quest log update

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| QuestCompletedPublish | QuestService | RecipeRegistry, FeatureFlagService, NotificationUI |

### Scene Objects
- Quest NPCs (existing)

---

## 6. STAMINA PROGRESSION

### Mô tả
Stamina là resource chính giới hạn actions per day. Base 100, tăng qua: Mastery level up (+3/level), equipment bonuses, food buffs. Max theoretical ~250 at L50 + best gear + food.

### Điều kiện
- Stamina tăng passive qua Mastery level
- Equipment với MaxStamina bonus khi equip
- Food/potion buff tạm thời

### Flow
```
Stamina sources:
    1. Base: 100 (L1)
    2. Mastery: +3 per level → +147 at L50 → total 247
    3. Equipment: EquipmentRuntime.OnEquip() → add bonus to MaxStamina
    4. Food buff: BuffRuntime.ApplyBuff() → temporary MaxStamina increase
    5. Sleep: restore to MaxStamina (full)
    6. Food consume: restore X stamina (not exceed max)

Stamina calculation:
    effectiveMaxStamina = baseStamina + (masteryLevel * staminaPerLevel) + equipmentBonus + buffBonus
    
    EntityRuntime.stats.Get(MaxStamina) → returns effectiveMaxStamina
    (Recalculated on: level up, equip/unequip, buff apply/expire)

Stamina costs:
    - Hoe: 4
    - Watering: 2
    - Seed: 1
    - Pickaxe/Axe: 4
    - Weapon swing: 3-5
    - Dodge: 12
    - Harvest: 2
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ProgressionService | Level up → +MaxStamina |
| EquipmentRuntime | Equip bonus |
| BuffRuntime | Temporary bonus |
| EntityRuntime.stats | Store/calculate effective max |
| TimeManager | Sleep → restore |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ProgressionConfig | ScriptableObject | baseStamina=100, staminaPerLevel=3 |
| Equipment EntityData | ScriptableObject | bonuses[{MaxStamina, +10}] |
| Food EntityData | ScriptableObject | restoreStamina=25 |

### UI
- Stamina bar (HUD) — shows current/max
- Stamina bar grows visually as max increases
- Warning flash khi stamina < 20%
- "Kiệt sức!" message khi stamina = 0

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| MasteryLevelUpPublish | ProgressionService | Stats recalculation |
| EquipmentChangedPublish | EquipmentRuntime | Stats recalculation |
| BuffAppliedPublish / BuffExpiredPublish | BuffRuntime | Stats recalculation |
| StaminaChangedPublish 🆕 | EntityRuntime.stats | HUD (update bar) |

### Scene Objects
- HUD (Stamina bar)
- Player entity (stats)

---

## TỔNG KẾT: PROGRESSION SYSTEM DEPENDENCY

```
EXP Sources (nhiều systems publish)
    │
    ├── Harvest → ExpRewardRuntime → GrantMasteryExp(10-20)
    ├── Craft → CraftingService → GrantMasteryExp(15-25)
    ├── Quest → QuestService → GrantMasteryExp(50-200)
    ├── Zone Clear → ClearZoneTracker → GrantMasteryExp(100-300)
    ├── Kill Enemy → ExpRewardRuntime → GrantMasteryExp(1-3)
    └── Mine/Gather → ExpRewardRuntime → GrantMasteryExp(2-5)
    
    ▼
ProgressionService
    │
    ├── Track EXP + Level (1-50)
    ├── OnLevelUp():
    │       ├── +MaxStamina (+3/level)
    │       ├── +MaxHP (+2/level)
    │       └── Check unlock table → unlock recipes/features
    │
    └── MasteryLevelUpPublish
            │
            ├── RecipeRegistry.Unlock() → new craft options
            ├── FeatureFlagService.Enable() → new features
            └── HUD update

Unlock Paths (3 types):
    ├── By Material: UnlockService → consume materials → unlock
    ├── By Money: ShopService.SpendGold() → unlock
    └── By Quest: QuestService.CompleteQuest() → unlock

Stamina Progression:
    Base(100) + Mastery(+3/lvl) + Equipment(bonus) + Buff(temp)
    → effectiveMaxStamina → limits actions per day
```
