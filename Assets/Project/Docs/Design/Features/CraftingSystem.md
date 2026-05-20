# 🔨 CRAFTING SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** CraftingSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Craft tool upgrade
> 2. Craft food
> 3. Craft equipment
> 4. Craft building
> 5. Craft sprinkler
> 6. Craft phân bón
> 7. Craft potion
> 8. Research (unlock recipe)

---

## INPUT ROUTING (áp dụng cho toàn bộ CraftingSystem)

Tất cả crafting đều qua NPC interaction:

```
Player đứng gần NPC (Blacksmith/Chef/Alchemist)
    → E / Chuột Phải → SecondaryActionEvent → ActionRuntime
        → EntityScanSystem.GetClosest() → NPC entity
        → Forward SecondaryActionEvent sang NPC
        → CraftingNPCRuntime.Handle(SecondaryActionEvent):
            → context.AddOption("craft", "ui.craft.open", priority)
            → InteractionOptionsReadyPublish → UI hiện menu
                → Player chọn "Craft" → mở CraftingUI
                    → Player chọn recipe → CraftingService.TryCraft()
```

**CraftingService.TryCraft() flow chung:**
1. Check recipe unlocked?
2. Check ingredients đủ?
3. Consume ingredients (EntityService.AddAmount(-n))
4. Grant output item (InventoryService.TryAdd())
5. Grant Mastery EXP
6. Publish CraftCompletedPublish

---

## 1. CRAFT TOOL UPGRADE

### Mô tả
Người chơi tương tác NPC Blacksmith, mở menu craft, chọn recipe upgrade tool. Consume nguyên liệu (ore + gold) → nhận tool tier cao hơn (damage/range tăng).

### Điều kiện
- Player đứng gần Blacksmith NPC
- Recipe đã unlock (via Mastery level hoặc quest)
- Player có đủ nguyên liệu (ore, ingot, gold)
- Player inventory có slot trống

### Flow
```
SecondaryAction → NPC → CraftingNPCRuntime → context.AddOption("craft")
    → Player chọn Craft → CraftingUI mở
        → Hiển thị danh sách recipe (filter: category=ToolUpgrade)
        → Player chọn recipe "Iron Pickaxe"
        → UI gọi CraftingService.TryCraft(recipeId, player):
            1. RecipeRegistry.Get(recipeId) → recipe
            2. Check recipe.unlocked? → YES
            3. Check ingredients:
                - InventoryService.HasAmount(player, "iron_ingot", 5)? → YES
                - InventoryService.HasAmount(player, "gold", 500)? → YES
            4. Consume:
                - EntityService.AddAmount(iron_ingot, -5)
                - ShopService.SpendGold(player, 500)
            5. Grant:
                - InventoryService.TryAdd(player, "iron_pickaxe", 1)
            6. ProgressionService.GrantMasteryExp(player, 20, CraftSource)
            7. EventBus.Publish(CraftCompletedPublish(recipeId, outputItem))
        → CraftingUI refresh (recipe greyed nếu đã craft unique)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() logic |
| RecipeRegistry | Store/lookup recipes |
| InventoryService | Check + consume ingredients, grant output |
| ShopService | Spend gold |
| ProgressionService | Grant EXP |
| CraftingNPCRuntime | Mở craft menu |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_IronPickaxe | ScriptableObject | ingredients[{iron_ingot,5},{gold,500}], output=iron_pickaxe, category=ToolUpgrade |
| IronPickaxe EntityData | ScriptableObject | ToolModule(toolType=Pickaxe), stats(Attack=3, Range=1.5) |
| Blacksmith EntityData | ScriptableObject | CraftingNPCModule(recipeCategory=ToolUpgrade) |

### UI
- CraftingUI panel: recipe list (left), ingredient preview (right), Craft button
- Ingredient slots hiện đỏ nếu thiếu, xanh nếu đủ
- "+20 Mastery" popup khi craft xong

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | CraftingNPCRuntime |
| InteractionOptionsReadyPublish | ActionRuntime | InteractionUI |
| CraftCompletedPublish | CraftingService | ProgressionService, QuestService |

### Scene Objects
- Blacksmith NPC prefab (EntityRoot, Collider2D, SpriteRenderer)
- CraftingUI (Canvas panel)

---

## 2. CRAFT FOOD

### Mô tả
Tương tác NPC Chef hoặc bếp (Kitchen station). Craft food từ nguyên liệu nông sản. Output = food item có stamina/HP restore khi dùng.

### Điều kiện
- Player gần Chef NPC hoặc Kitchen station
- Recipe unlocked
- Có đủ nguyên liệu (crops, animal products)
- Inventory có slot trống

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = Food
    - ingredients = crops/animal products
    - output = food item (ConsumableModule: restoreStamina, restoreHP)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| InventoryService | Ingredients + output |
| ConsumableRuntime | Food use logic (khi player ăn) |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_FriedEgg | ScriptableObject | ingredients[{egg,2},{oil,1}], output=fried_egg, category=Food |
| FriedEgg EntityData | ScriptableObject | ConsumableModule(restoreStamina=25, restoreHP=0) |
| Chef EntityData | ScriptableObject | CraftingNPCModule(recipeCategory=Food) |

### UI
- CraftingUI (shared, filter by Food category)
- Preview: hiện stamina/HP restore amount

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService, QuestService |

### Scene Objects
- Chef NPC hoặc Kitchen station prefab

---

## 3. CRAFT EQUIPMENT

### Mô tả
Craft armor/accessory từ nguyên liệu. Output = equipment item có stat bonuses (MaxHP, Defense, MaxStamina, etc.) khi trang bị.

### Điều kiện
- Player gần Blacksmith/Tailor NPC
- Recipe unlocked
- Đủ nguyên liệu (ore, cloth, monster drops)

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = Equipment
    - output = equipment item (EquipmentModule: slot, statBonuses[])
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| EquipmentRuntime | Apply stat bonuses khi equip |
| InventoryService | Ingredients + output |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_IronArmor | ScriptableObject | ingredients[{iron_ingot,8},{cloth,3}], output=iron_armor |
| IronArmor EntityData | ScriptableObject | EquipmentModule(slot=Body, bonuses[{Defense,+5},{MaxHP,+20}]) |

### UI
- CraftingUI (shared, filter Equipment)
- Preview: hiện stat bonuses

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService |

### Scene Objects
- Blacksmith/Tailor NPC

---

## 4. CRAFT BUILDING

### Mô tả
Craft building item (chuồng, kho, etc.) từ nguyên liệu. Output = building item để đặt vào farm (dùng BuildingPlacementRuntime).

### Điều kiện
- Player gần Carpenter NPC
- Recipe unlocked
- Đủ nguyên liệu (wood, stone, gold)

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = Building
    - output = building item (PlacementModule + BuildingModule config)
    - Player sau đó dùng item để đặt (xem AnimalSystem.XâyChuồng)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| InventoryService | Ingredients + output |
| BuildingPlacementRuntime | Đặt building sau khi craft |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_ChickenCoop | ScriptableObject | ingredients[{wood,20},{stone,10},{gold,1000}], output=chicken_coop_item |
| ChickenCoopItem EntityData | ScriptableObject | PlacementModule(buildingSize=2x2, placedEntityData=ChickenCoop) |
| Carpenter EntityData | ScriptableObject | CraftingNPCModule(recipeCategory=Building) |

### UI
- CraftingUI (shared, filter Building)
- Preview: hiện building size + capacity

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService |

### Scene Objects
- Carpenter NPC prefab

---

## 5. CRAFT SPRINKLER

### Mô tả
Craft sprinkler item từ ore + components. Output = sprinkler item để đặt trên farm. Sprinkler tự động tưới các ô xung quanh mỗi sáng.

### Điều kiện
- Player gần Blacksmith/Engineer NPC
- Recipe unlocked (Mastery level 3+)
- Đủ nguyên liệu (iron, copper, gold)

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = FarmEquipment
    - output = sprinkler item (PlacementModule)
    - Khi đặt: SprinklerRuntime → mỗi DayChangedPublish → WateredTileTracker.SetWatered(cells[])
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| SprinklerRuntime | Auto-water logic (xem FarmingSystem) |
| WateredTileTracker | Nhận water từ sprinkler |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_BasicSprinkler | ScriptableObject | ingredients[{copper_ingot,3},{iron_ingot,2}], output=basic_sprinkler |
| BasicSprinkler EntityData | ScriptableObject | PlacementModule, SprinklerModule(radius=1, pattern=cross) |

### UI
- CraftingUI (shared)
- Placement ghost hiện vùng tưới (highlight cells)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService |
| DayChangedPublish | TimeManager | SprinklerRuntime |

### Scene Objects
- Sprinkler prefab (EntityRoot, SpriteRenderer)

---

## 6. CRAFT PHÂN BÓN

### Mô tả
Craft fertilizer từ nguyên liệu (weeds, animal products, etc.). Output = fertilizer item dùng trên ô đất để tăng tốc grow hoặc tăng quality.

### Điều kiện
- Player gần Chef/Alchemist NPC
- Recipe unlocked
- Đủ nguyên liệu

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = Fertilizer
    - output = fertilizer item (FertilizerModule: type=Speed/Quality, bonus)
    - Khi dùng: FertilizerRuntime apply lên ô đất (xem FarmingSystem)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| FertilizerRuntime | Apply logic (FarmingSystem) |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_SpeedFertilizer | ScriptableObject | ingredients[{weed,5},{egg,2}], output=speed_fertilizer |
| SpeedFertilizer EntityData | ScriptableObject | FertilizerModule(type=Speed, growBonus=1) |

### UI
- CraftingUI (shared)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService |

### Scene Objects
- NPC (shared with other craft categories)

---

## 7. CRAFT POTION

### Mô tả
Craft buff potion từ herbs + monster drops. Output = potion item, khi dùng → buff tạm thời (attack up, defense up, speed up, etc.) trong X phút game.

### Điều kiện
- Player gần Alchemist NPC
- Recipe unlocked
- Đủ nguyên liệu (herbs, monster drops)

### Flow
```
(Giống flow chung CraftingService.TryCraft)
    - category = Potion
    - output = potion item (ConsumableModule: buffType, buffValue, durationMinutes)
    - Khi dùng: BuffRuntime.ApplyBuff(player, buff) → timer → remove buff
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| CraftingService | TryCraft() |
| BuffRuntime 🆕 | Apply/remove timed buffs |
| ConsumableRuntime | Use potion logic |
| TimeManager | Track buff duration |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Recipe_AttackPotion | ScriptableObject | ingredients[{red_herb,3},{slime_drop,2}], output=attack_potion |
| AttackPotion EntityData | ScriptableObject | ConsumableModule(buffType=Attack, buffValue=+5, durationMinutes=30) |
| Alchemist EntityData | ScriptableObject | CraftingNPCModule(recipeCategory=Potion) |

### UI
- CraftingUI (shared, filter Potion)
- Active buff icons trên HUD (icon + timer countdown)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| CraftCompletedPublish | CraftingService | ProgressionService |
| BuffAppliedPublish 🆕 | BuffRuntime | HUD (show buff icon) |
| BuffExpiredPublish 🆕 | BuffRuntime | HUD (remove icon), EntityRuntime (revert stat) |

### Scene Objects
- Alchemist NPC prefab
- Buff icon UI (HUD overlay)

---

## 8. RESEARCH (UNLOCK RECIPE)

### Mô tả
Người chơi tương tác NPC đặc biệt (Scholar/Researcher), consume nguyên liệu + gold → bắt đầu research timer. Sau X ngày game, recipe mới được unlock. Không instant — phải chờ.

### Điều kiện
- Player gần Scholar NPC
- Research slot available (max 1 active research)
- Đủ nguyên liệu + gold cho research cost
- Recipe chưa unlock

### Flow
```
SecondaryAction → Scholar NPC → context.AddOption("research")
    → Player chọn Research → ResearchUI mở
        → Hiển thị danh sách recipe chưa unlock
        → Player chọn recipe để research
        → ResearchService.StartResearch(recipeId, player):
            1. Check ingredients + gold → consume
            2. Set activeResearch = {recipeId, daysRemaining}
            3. EventBus.Publish(ResearchStartedPublish(recipeId, days))
        
TimeManager → DayChangedPublish → ResearchService.OnNewDay():
    → if activeResearch != null:
        - daysRemaining--
        - if daysRemaining <= 0:
            - RecipeRegistry.Unlock(recipeId)
            - activeResearch = null
            - EventBus.Publish(ResearchCompletedPublish(recipeId))
            - NotificationUI: "Đã nghiên cứu xong: [Recipe Name]!"
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ResearchService 🆕 | Manage active research + timer |
| RecipeRegistry | Unlock recipe |
| InventoryService | Consume ingredients |
| ShopService | Spend gold |
| TimeManager | DayChangedPublish → countdown |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ResearchData | ScriptableObject | recipeId, researchCost[ingredients], goldCost, daysRequired |
| Scholar EntityData | ScriptableObject | ResearchNPCModule |

### UI
- ResearchUI panel: list available research, cost preview, Start button
- Active research indicator: "Đang nghiên cứu: Iron Armor (còn 2 ngày)"
- Notification khi hoàn thành

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | ResearchNPCRuntime |
| ResearchStartedPublish 🆕 | ResearchService | ResearchUI |
| ResearchCompletedPublish 🆕 | ResearchService | NotificationUI, RecipeRegistry |
| DayChangedPublish | TimeManager | ResearchService |

### Scene Objects
- Scholar NPC prefab
- ResearchUI (Canvas panel)

---

## TỔNG KẾT: CRAFTING SYSTEM DEPENDENCY

```
PlayerControler (Input E / Chuột Phải)
    │
    ▼
ActionRuntime → SecondaryAction → NPC entity
    │
    ▼
CraftingNPCRuntime / ResearchNPCRuntime
    │
    ├── context.AddOption("craft") → InteractionUI → CraftingUI
    └── context.AddOption("research") → InteractionUI → ResearchUI
    
CraftingUI
    │
    ▼
CraftingService.TryCraft(recipeId, player)
    │
    ├── RecipeRegistry.Get(recipeId) → check unlocked
    ├── InventoryService.HasAmount() → check ingredients
    ├── EntityService.AddAmount(-n) → consume
    ├── ShopService.SpendGold() → consume gold
    ├── InventoryService.TryAdd() → grant output
    └── ProgressionService.GrantMasteryExp() → EXP
    
ResearchService
    │
    ├── StartResearch() → consume + start timer
    └── OnNewDay() → countdown → RecipeRegistry.Unlock()

Output Items → Used by:
    ├── Tool Upgrade → ToolRuntime (better stats)
    ├── Food → ConsumableRuntime (restore stamina/HP)
    ├── Equipment → EquipmentRuntime (stat bonuses)
    ├── Building → BuildingPlacementRuntime (place in world)
    ├── Sprinkler → SprinklerRuntime (auto-water)
    ├── Fertilizer → FertilizerRuntime (boost growth)
    └── Potion → BuffRuntime (timed stat buff)
```
