# 04 — CORE SYSTEMS (System Architecture Map)

> Status: 🟡 Đề xuất — cần review
> Mỗi feature → cần script/service gì → đã có hay cần mới

---

## Ký hiệu

- ✅ = Đã có, không cần sửa
- 🔧 = Đã có, cần sửa/mở rộng
- 🆕 = Cần tạo mới

---

## FEATURE GROUP 1: FARMING

### F1: Cuốc đất
```
├── HoeRuntime ✅ — validate ô + đổi tile
├── ToolModule ✅ — config (toolType = Hoe)
├── GridSystem ✅ — GetCellInFrontOf
├── WorldEntityService ✅ — IsTillable(), SetGround()
├── TileRegistry ✅ — track tile changes
├── TileData ✅ — plowedTile reference
└── ToolActionBridge ✅ — animation → AnimStrikeEvent
```

### F2: Gieo hạt
```
├── PlacementRuntime ✅ — validate + spawn plant entity
├── PlacementModule ✅ — config (objectType, entityData)
├── PlacementValidator ✅ — CanPlace check
├── SpawnSystem ✅ — instantiate prefab
├── WorldEntityService ✅ — TryRegisterSpawn
└── SpatialEntityRegistry ✅ — track position
```

### F3: Tưới nước
```
├── WateringCanRuntime 🆕 — validate ô plowed + execute tưới
│   ├── extends ToolRuntime (giống HoeRuntime)
│   ├── Validate: check ô là plowed/planted
│   └── Execute: gọi WateredTileTracker.SetWatered(cell)
├── WateredTileTracker 🆕 — service track ô đã tưới
│   ├── HashSet<Vector2Int> wateredCells
│   ├── SetWatered(cell) — thêm vào set
│   ├── IsWatered(cell) — check
│   ├── ResetAll() — gọi mỗi NextDay
│   └── Save/Load — persist state
├── TileData 🔧 — thêm wateredTile (tile visual khi tưới)
├── WorldEntityService 🔧 — SetGround cho watered tile visual
├── TimeManager ✅ — DayChangedPublish → trigger reset
└── ToolModule ✅ — config (toolType = WateringCan)
```

### F5+F6: Cây phát triển + Cần tưới mới lớn
```
├── StageRuntime 🔧 — thêm check watered trước khi grow
│   ├── Handle(NextDayEvent): if (!WateredTileTracker.IsWatered(myCell)) → skip grow
│   └── Cần reference tới WateredTileTracker (qua GameManager hoặc inject)
├── StageModule ✅ — stages[], daysToGrow, canHarvest
└── TimeManager ✅ — publish NextDayEvent
```

### F7: Cây héo/chết
```
├── StageRuntime 🔧 — thêm wilt counter
│   ├── int daysWithoutWater = 0
│   ├── NextDay: if not watered → daysWithoutWater++
│   ├── if daysWithoutWater >= 2 → trigger WiltEvent hoặc DieEvent
│   └── Reset counter khi được tưới
└── (Sprite héo: thêm 1 sprite vào StageModule.stages hoặc separate wiltSprite field)
```

### F9: Chất lượng nông sản (1-3★)
```
├── QualityModule 🆕 — IModuleData, config (maxQuality = 3)
├── QualityRuntime 🆕 — IModuleRuntime
│   ├── int quality = 1 (default)
│   ├── Tính quality khi thu hoạch: dựa trên daysWatered / totalDays ratio
│   └── Save/Load quality
├── HarvestRuntime 🔧 — khi harvest, set quality trên drop item
└── EntityData 🔧 — sellPrice nhân hệ số quality (×1.0 / ×1.5 / ×2.0)
```

### F10: Phân bón
```
├── FertilizerRuntime 🆕 — extends ToolRuntime hoặc PlacementRuntime
│   ├── Validate: check ô có cây
│   ├── Execute: apply buff lên StageRuntime (giảm daysToGrow hoặc tăng quality)
│   └── Consume 1 phân bón từ inventory
├── StageRuntime 🔧 — thêm field fertilized (bool/int)
│   └── if fertilized → grow nhanh hơn hoặc quality +1
└── EntityData (phân bón) — item category = Consumable, có PlacementModule
```

### F11: Sprinkler/Auto-water
```
├── SprinklerRuntime 🆕 — IModuleRuntime, IHandleEvent<NextDayEvent>
│   ├── Handle(NextDayEvent): auto-water các ô trong range
│   ├── Config: range (1 ô T1, 5 ô T2, 9 ô T3)
│   └── Gọi WateredTileTracker.SetWatered() cho mỗi ô
├── SprinklerModule 🆕 — IModuleData, config (range, pattern)
└── PlacementRuntime ✅ — dùng để đặt sprinkler xuống đất
```

### F12: Đất có chất lượng
```
├── SoilQualityTracker 🆕 — service quản lý quality per cell
│   ├── Dictionary<Vector2Int, int> soilQuality (1-3)
│   ├── GetQuality(cell), SetQuality(cell, value)
│   ├── Ảnh hưởng: quality cao → cây grow nhanh hơn / quality nông sản cao hơn
│   └── Save/Load
└── StageRuntime 🔧 — đọc soilQuality khi tính growth/quality
```

### F13: Cây tái thu hoạch
```
├── StageModule 🔧 — thêm field: bool regrowable, int regrowDays, int regrowToStage
├── HarvestRuntime 🔧 — khi harvest:
│   ├── if regrowable → reset stage về regrowToStage (không destroy)
│   └── else → DieEvent (destroy như hiện tại)
└── StageRuntime 🔧 — support reset stage
```

---

## FEATURE GROUP 2: CHĂN NUÔI

### A1-A3: Gà/Bò/Cừu
```
├── AnimalRuntime ✅ — state machine (Hungry→Fed→ProductReady)
├── AnimalModule ✅ — config (feedItem, productItem, amount)
├── SecondaryActionEvent ✅ — cho ăn / thu sản phẩm
└── (Chỉ cần thêm EntityData + prefab cho Bò, Cừu — code đã có)
```

### A4: Chuồng trại
```
├── BuildingPlacementRuntime 🆕 — extends PlacementRuntime
│   ├── Validate: check đủ diện tích, không overlap
│   ├── Execute: spawn building entity
│   └── Building entity có InventoryModule (chứa animals)
├── BuildingModule 🆕 — config (capacity, animalType allowed)
└── PlacementValidator 🔧 — support multi-cell placement
```

### A5: Động vật bệnh/chết (đơn giản)
```
├── AnimalRuntime 🔧 — thêm:
│   ├── int daysNotFed = 0
│   ├── NextDay: if state == Hungry → daysNotFed++
│   ├── if daysNotFed >= 3 → trigger DieEvent (biến mất)
│   └── Visual: sprite "buồn" khi daysNotFed >= 1
└── (Không cần animation bệnh phức tạp)
```

---

## FEATURE GROUP 3: THỜI GIAN & MÔI TRƯỜNG

### T4: Thời tiết (nắng/mưa)
```
├── WeatherSystem 🆕 — MonoBehaviour hoặc service
│   ├── WeatherType enum: Sunny, Rainy, Stormy
│   ├── Mỗi NextDay → random weather (% theo config)
│   ├── Publish WeatherChangedPublish
│   ├── If Rainy → auto-water tất cả outdoor cells
│   └── Save/Load current weather + forecast
├── WeatherConfig 🆕 — ScriptableObject
│   ├── float rainChance = 0.3f
│   ├── float stormChance = 0.05f
│   └── (Có thể mở rộng theo season sau)
└── WateredTileTracker 🔧 — WeatherSystem gọi SetWatered cho all cells khi mưa
```

---

## FEATURE GROUP 4: COMBAT & KHAI HOANG

### C3: Vùng khai hoang
```
├── ClearZoneTracker 🆕 — service track vùng nào đã clear
│   ├── Dictionary<string, bool> zoneCleared
│   ├── CheckZoneCleared(zoneId): đếm enemy/obstacle còn lại
│   ├── OnZoneCleared → publish ZoneClearedPublish → mở đất
│   └── Save/Load
├── WorldEntityService 🔧 — khi zone cleared, convert vùng thành farmable
└── (Scene setup: mỗi vùng = area trong scene với markers)
```

### C4: Enemy drop nguyên liệu
```
├── DropRuntime ✅ — đã có, chỉ cần config drop table đúng
├── DropModule ✅ — DropEntry[] (item, chance, min/max)
└── (Chỉ cần tạo EntityData cho mutant materials)
```

### C5: Vũ khí riêng
```
├── WeaponRuntime ✅ — đã có (archetype, damage, range, cooldown, knockback)
├── WeaponModule ✅ — config
└── (Chỉ cần thêm EntityData + sprite cho weapon mới)
```

### C7: Enemy spawn theo vùng
```
├── EnemyObject ✅ — đã có AI (chase/attack)
├── RespawnRuntime ✅ — respawn sau delay
└── (Config: enemy chỉ spawn trong vùng chưa cleared)
```

### C9: Player chết → hậu quả
```
├── PlayerDeathHandler 🆕 — MonoBehaviour hoặc service
│   ├── Listen DieEvent trên player
│   ├── Hậu quả: respawn tại nhà, mất 50% stamina, mất 1 ngày
│   └── (Không mất item — giữ hopeful tone)
└── TimeManager 🔧 — SkipToNextDay() khi player chết
```

---

## FEATURE GROUP 5: CRAFTING & NÂNG CẤP

### CR1-CR7: Tất cả crafting
```
├── CraftingService ✅ — đã có (validate, consume, grant)
├── CraftingRuntime ✅ — interaction option
├── CraftingModule ✅ — recipe list
└── (Chỉ cần thêm RecipeData cho: food, phân bón, vật liệu, equipment, sprinkler, potion)
```

### CR8: Research
```
├── ResearchService 🆕 — service quản lý research queue
│   ├── StartResearch(recipeId, materials) → consume materials, start timer
│   ├── Handle NextDay → progress timer
│   ├── OnComplete → unlock recipe trong CraftingService
│   └── Save/Load research state
├── ResearchData 🆕 — ScriptableObject (input materials, days needed, output recipe)
└── (UI: dùng NPC dialogue "Nhà nghiên cứu" — không cần UI panel riêng)
```

---

## FEATURE GROUP 6: MINING & TÀI NGUYÊN

### M3: Hái thảo mộc
```
├── HarvestRuntime ✅ — harvestTool = None (hái tay)
└── (Chỉ cần EntityData cho thảo mộc + spawn trong vùng hoang)
```

### M4: Mine/hang động
```
├── (Scene riêng: MineScene — đã có trong project)
├── ScenePortalRuntime ✅ — portal vào mine
└── SceneTransitionService ✅ — chuyển scene
```

### M5: Quặng theo tier
```
├── (Đã có 5 tier ore: Copper→Mythril)
└── ✅ Chỉ cần balance HP/drop
```

### M6: Tài nguyên respawn
```
├── RespawnRuntime ✅ — đã có (delay, restore HP)
└── (Config respawn delay per resource type)
```

---

## FEATURE GROUP 7: NPC & QUEST

### N1-N3: Shop, Dialogue, Quest
```
├── ShopService ✅
├── DialogueService ✅
├── QuestService ✅
└── (Tất cả đã có — chỉ cần thêm content/data)
```

---

## FEATURE GROUP 8: PROGRESSION

### P1: Farming Mastery (thay Level)
```
├── ProgressionService 🔧 — rename concept Level → Mastery
│   ├── EXP formula giữ nguyên (hoặc tune)
│   ├── Mastery level unlock: recipe, cây, AI feature
│   ├── EXP sources: farming = chính, combat = phụ
│   └── Stat growth: +MaxStamina per level (thay vì +ATK/DEF)
├── MasteryUnlockData 🆕 — ScriptableObject define unlock per level
│   ├── Level 2: unlock recipe X
│   ├── Level 3: unlock AI feature Y
│   └── ...
└── UnlockService 🔧 — check mastery level thay vì combat level
```

### P3: Unlock bằng nguyên liệu
```
├── CraftingService ✅ — đã check nguyên liệu
└── (Recipe cần material cụ thể = unlock tự nhiên)
```

### P7: Tiền là thước đo
```
├── StatType.Money ✅ — đã có
└── (UI hiển thị tổng tiền kiếm được = progression indicator)
```

---

## FEATURE GROUP 9: SAVE/LOAD

### S1: Save/Load
```
├── SaveLoadManager ✅
└── (Mở rộng: save thêm WateredTileTracker, WeatherSystem, ClearZoneTracker, ResearchService)
```

### S2: Multiple save slots
```
├── SaveLoadManager 🔧 — thêm slot parameter
│   ├── Save(slotId), Load(slotId)
│   └── File naming: entities_save_{slotId}.json
└── (UI: save slot selection screen)
```

### S3: Auto-save
```
├── SaveLoadManager 🔧 — subscribe DayChangedPublish → auto save
└── (Đơn giản: save mỗi khi ngủ/sang ngày mới)
```

---

## FEATURE GROUP 10: AI ASSISTANT

### AI1, AI2, AI4: Gợi ý, dự báo, nhắc nhở
```
├── AIAssistantService 🆕 — service tính toán hints
│   ├── GetPlantSuggestion() — dựa trên soil quality, tiền hiện có
│   ├── GetWeatherForecast() — đọc WeatherSystem.GetTomorrow()
│   ├── GetWateringReminder() — check cây nào chưa tưới
│   └── Unlock theo Mastery level
├── AIAssistantUI 🆕 — panel nhỏ góc màn hình
│   ├── Subscribe GameHourChangedPublish → refresh hints
│   └── Hiển thị 1-2 dòng text gợi ý
└── (Không phải AI thật — chỉ là logic if/else đọc game state)
```

### AI6: Research/Phân tích giống
```
├── ResearchService 🆕 (đã liệt kê ở CR8)
└── (Unlock cây mới = output của research)
```

---

## FEATURE GROUP 11: NARRATIVE

### ST1-ST4, ST6, ST7: Story delivery
```
├── NarrativeService 🆕 — service quản lý story events
│   ├── List<StoryEvent> events (trigger condition, content)
│   ├── Check mỗi NextDay: có event nào trigger không?
│   ├── Trigger types: dayReached, zoneClear, masteryLevel, questComplete
│   └── Save/Load: which events already triggered
├── StoryEventData 🆕 — ScriptableObject per event
│   ├── triggerCondition (day >= X, zone cleared, etc.)
│   ├── deliveryType (diary, message, news, NPC dialogue)
│   └── content (text key, portrait, audio key)
├── DiaryUI 🆕 — popup nhật ký (2-3 dòng text)
├── MessageUI 🆕 — notification tin nhắn
├── NewsUI 🆕 — text overlay tin tức
└── DialogueService ✅ — dùng cho NPC story dialogue
```

---

## FEATURE GROUP 12: UI

### U1: HUD
```
├── (Đã có stamina/HP bar, time display, money)
└── 🔧 Thêm: Mastery level indicator, weather icon
```

### U5: Minimap
```
├── MinimapSystem 🆕 — render minimap từ tilemap data
└── (Đơn giản: camera thứ 2 zoom out, render layer riêng)
```

### U6: End-of-day summary
```
├── EndOfDaySummaryUI 🆕 — popup khi ngủ
│   ├── Hiển thị: tiền kiếm hôm nay, cây phát triển, mastery gained
│   └── Subscribe DayChangedPublish
└── DailyTracker 🆕 — service track metrics trong ngày (reset mỗi sáng)
```

### U8: AI Assistant panel
```
└── AIAssistantUI 🆕 (đã liệt kê ở AI group)
```

### U9: Calendar
```
├── CalendarUI 🆕 — hiển thị ngày hiện tại, event sắp tới
└── TimeManager ✅ — đọc day/season
```

### U10: Tutorial
```
├── TutorialService 🆕 — step-by-step guide ngày đầu
│   ├── List<TutorialStep> (condition, hint text, highlight)
│   ├── Track progress, skip if already done
│   └── Save: tutorial completed flag
└── (Đơn giản: text popup + highlight UI element)
```

---

## TÓM TẮT: CẦN TẠO MỚI

| # | Script/Service | Thuộc feature |
|---|---------------|---------------|
| 1 | WateringCanRuntime | F3 |
| 2 | WateredTileTracker | F3, F6, F11 |
| 3 | QualityModule + QualityRuntime | F9 |
| 4 | FertilizerRuntime | F10 |
| 5 | SprinklerModule + SprinklerRuntime | F11 |
| 6 | SoilQualityTracker | F12 |
| 7 | BuildingPlacementRuntime + BuildingModule | A4 |
| 8 | WeatherSystem + WeatherConfig | T4 |
| 9 | ClearZoneTracker | C3 |
| 10 | PlayerDeathHandler | C9 |
| 11 | ResearchService + ResearchData | CR8 |
| 12 | MasteryUnlockData | P1 |
| 13 | AIAssistantService + AIAssistantUI | AI1-4 |
| 14 | NarrativeService + StoryEventData | ST1-7 |
| 15 | DiaryUI + MessageUI + NewsUI | ST2-3, ST7 |
| 16 | MinimapSystem | U5 |
| 17 | EndOfDaySummaryUI + DailyTracker | U6 |
| 18 | CalendarUI | U9 |
| 19 | TutorialService | U10 |

## TÓM TẮT: CẦN SỬA

| # | Script | Sửa gì |
|---|--------|--------|
| 1 | StageRuntime | Check watered, wilt counter, regrow support |
| 2 | HarvestRuntime | Set quality trên drop, regrow logic |
| 3 | AnimalRuntime | daysNotFed counter, death |
| 4 | ProgressionService | Rename → Mastery, tune EXP sources |
| 5 | UnlockService | Check mastery thay vì level |
| 6 | SaveLoadManager | Thêm save cho weather, watered, zones, research |
| 7 | PlacementValidator | Multi-cell support (buildings) |
| 8 | TileData | Thêm wateredTile |
| 9 | StageModule | Thêm regrowable, regrowDays, wiltSprite |
