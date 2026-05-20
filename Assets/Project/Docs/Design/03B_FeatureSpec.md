# 03B — FEATURE SPECIFICATION (Thiết kế chức năng chi tiết)

> Mỗi chức năng: Flow → Phụ thuộc → Data → UI → Events → Scene Objects

---

## 1. FARMING

### 1.1 Cuốc đất (F1) ✅ Đã có

**Flow:**
```
Player nhấn Action → ActionRuntime forward → HoeRuntime.Validate()
    → Check: ô trước mặt tillable? Không có blocker?
        → YES: Request animation → AnimStrikeEvent → HoeRuntime.Execute()
            → WorldEntityService.SetGround(cell, plowedTile)
            → Trừ stamina (4)
        → NO: Không làm gì
```

**Phụ thuộc:** GridSystem, WorldEntityService, TileRegistry, StaminaSystem
**Data:** TileData.plowedTile
**UI:** Không
**Events:** PrimaryActionEvent → AnimStrikeEvent
**Scene:** Tilemap Ground

---

### 1.2 Gieo hạt (F2) ✅ Đã có

**Flow:**
```
Player cầm Seed item → nhấn Action → PlacementRuntime.Validate()
    → Check: ô là plowed? Không có plant? Có đủ seed?
        → YES: Request animation → AnimStrikeEvent → PlacementRuntime.Execute()
            → SpawnRequestPublish(plantEntity, cell)
            → SpawnSystem instantiate prefab
            → Trừ 1 seed từ inventory
            → Trừ stamina (1)
        → NO: Không làm gì
```

**Phụ thuộc:** InventoryService, SpawnSystem, WorldEntityService, PlacementValidator
**Data:** Seed EntityData (có PlacementModule → objectType plant), Plant EntityData (có StageModule)
**UI:** Không (seed hiển thị trên hotbar)
**Events:** PrimaryActionEvent → AnimStrikeEvent → SpawnRequestPublish → SpawnedEvent
**Scene:** Plant prefab spawned trên tilemap

---

### 1.3 Tưới nước (F3) 🆕

**Flow:**
```
Player cầm WateringCan → nhấn Action → WateringCanRuntime.Validate()
    → Check: ô trước mặt là plowed HOẶC có plant?
        → YES: Request animation → AnimStrikeEvent → WateringCanRuntime.Execute()
            → WateredTileTracker.SetWatered(cell)
            → WorldEntityService.SetGround(cell, wateredTile) [visual]
            → Trừ stamina (2)
        → NO: Không làm gì
```

**Phụ thuộc:** WateredTileTracker (🆕), GridSystem, WorldEntityService, TileData
**Data:** TileData.wateredTile (🔧 thêm), ToolModule (toolType = WateringCan)
**UI:** Không (tile visual thay đổi = feedback)
**Events:** PrimaryActionEvent → AnimStrikeEvent
**Scene:** Tilemap Ground (tile đổi visual khi tưới)

---

### 1.4 Cây phát triển + Cần tưới (F5, F6) 🔧

**Flow:**
```
TimeManager publish DayChangedPublish → StageObject nhận
    → Forward NextDayEvent tới EntityRuntime → StageRuntime.Handle(NextDayEvent)
        → Check: WateredTileTracker.IsWatered(myCell)?
            → YES: daysInCurrentStage++ → check đủ ngày → lên stage → đổi sprite
            → NO: daysWithoutWater++ (xem F7 héo)
        → Check: fertilized?
            → YES: daysInCurrentStage += 2 (grow nhanh gấp đôi)

Mỗi đầu ngày mới:
    → WateredTileTracker.ResetAll() (tất cả ô về trạng thái chưa tưới)
    → Tile visual revert về plowed (không còn watered)
```

**Phụ thuộc:** WateredTileTracker, TimeManager, SoilQualityTracker (optional)
**Data:** StageModule.stages[] (sprite, daysToGrow, canHarvest)
**UI:** Không (sprite cây thay đổi = feedback visual)
**Events:** DayChangedPublish → NextDayEvent (per entity)
**Scene:** Plant entities đã spawn

---

### 1.5 Cây héo/chết (F7) 🆕

**Flow:**
```
StageRuntime.Handle(NextDayEvent):
    → if NOT watered today:
        → daysWithoutWater++
        → if daysWithoutWater == 1: đổi sprite → wiltSprite (cảnh báo)
        → if daysWithoutWater >= 2: trigger DieEvent → cây chết, mất vụ
    → if watered:
        → daysWithoutWater = 0 (reset)
```

**Phụ thuộc:** WateredTileTracker, StageRuntime
**Data:** StageModule (🔧 thêm wiltSprite), daysWithoutWater (runtime state)
**UI:** Không (sprite héo = visual warning)
**Events:** NextDayEvent → (có thể) DieEvent
**Scene:** Plant entity sprite thay đổi

---

### 1.6 Thu hoạch (F4) ✅ Đã có + 🔧 Sửa

**Flow:**
```
Player dùng tool/tay → HarvestRuntime check:
    → Stage đã canHarvest?
    → Tool đúng loại? (hoặc None = hái tay)
        → YES: 
            → Tính quality (🆕): quality = f(daysWatered/totalDays, fertilized, soilQuality)
            → DieEvent → DropRuntime spawn nông sản (với quality tag)
            → Grant Mastery EXP (+10-30 tùy cây)
            → IF regrowable: reset stage về regrowToStage (không destroy)
            → IF not regrowable: MortalRuntime → destroy
        → NO: Không làm gì
```

**Phụ thuộc:** HarvestRuntime, DropRuntime, QualityRuntime (🆕), ProgressionService, InventoryService (pickup)
**Data:** HarvestModule, DropModule, QualityModule (🆕), StageModule.regrowable (🔧)
**UI:** Popup "+30 Mastery" (nhỏ, fade out)
**Events:** TakeDamageEvent/SecondaryActionEvent → DieEvent → SpawnRequestPublish (drop)
**Scene:** Drop item spawned, plant entity destroyed/reset

---

### 1.7 Chất lượng nông sản (F9) 🆕

**Flow:**
```
Khi thu hoạch:
    → QualityRuntime tính: 
        ratio = daysActuallyWatered / totalGrowDays
        bonus = fertilized ? +1 : 0
        bonus += soilQuality >= 2 ? +1 : 0
        quality = clamp(1 + bonus + (ratio >= 0.8 ? 1 : 0), 1, 3)
    → Drop item có quality field
    → Khi bán: sellPrice × qualityMultiplier (1.0 / 1.5 / 2.0)
```

**Phụ thuộc:** WateredTileTracker (đếm ngày tưới), SoilQualityTracker, ShopService
**Data:** QualityModule (maxQuality=3), EntityData.sellPrice
**UI:** Icon ★/★★/★★★ trên item tooltip
**Events:** Không event riêng — tính tại thời điểm harvest
**Scene:** Không

---

### 1.8 Phân bón (F10) 🆕

**Flow:**
```
Player cầm Fertilizer item → nhấn Action → FertilizerRuntime.Validate()
    → Check: ô có plant? Plant chưa fertilized?
        → YES: animation → Execute()
            → Set plant.StageRuntime.fertilized = true
            → Consume 1 fertilizer từ inventory
            → Trừ stamina (2)
        → NO: Không làm gì
```

**Phụ thuộc:** InventoryService, StageRuntime
**Data:** Fertilizer EntityData (category=Consumable, có ToolModule hoặc PlacementModule)
**UI:** Không (visual: particle nhỏ trên cây khi bón)
**Events:** PrimaryActionEvent → AnimStrikeEvent
**Scene:** Không thay đổi scene object

---

### 1.9 Sprinkler (F11) 🆕

**Flow:**
```
Player craft Sprinkler → đặt xuống đất (PlacementRuntime)
    → Sprinkler entity spawned tại cell

Mỗi đầu ngày (trước khi player thức dậy):
    → SprinklerRuntime.Handle(NextDayEvent):
        → Lấy cells trong range (T1=1ô, T2=5ô 十字, T3=9ô 3×3)
        → Với mỗi cell: WateredTileTracker.SetWatered(cell)
        → Visual: tile đổi sang wateredTile
```

**Phụ thuộc:** WateredTileTracker, PlacementRuntime, TimeManager
**Data:** SprinklerModule (range, pattern), EntityData (category=Placeable)
**UI:** Không
**Events:** NextDayEvent (trước StageRuntime)
**Scene:** Sprinkler prefab trên tilemap

---

### 1.10 Đất chất lượng (F12) 🆕

**Flow:**
```
SoilQualityTracker quản lý quality per cell (1-3):
    → Mặc định: 1 (đất thường)
    → Tăng: bón phân liên tục nhiều vụ → quality tăng dần
    → Ảnh hưởng: quality cao → cây grow nhanh hơn / nông sản quality cao hơn
    → Đọc bởi: StageRuntime (growth speed), QualityRuntime (harvest quality)
```

**Phụ thuộc:** FertilizerRuntime (trigger tăng), StageRuntime, QualityRuntime
**Data:** SoilQualityTracker state (Dictionary<Vector2Int, int>)
**UI:** AI Assistant gợi ý "đất ở vùng này chất lượng cao" (optional)
**Events:** Không event riêng
**Scene:** Không visual (hoặc subtle tile tint)

---

### 1.11 Cây tái thu hoạch (F13) 🔧

**Flow:**
```
Khi harvest cây có regrowable = true:
    → KHÔNG trigger MortalRuntime (không destroy)
    → Reset StageRuntime: currentStage = regrowToStage, daysInCurrentStage = 0
    → Cây quay lại stage nhỏ hơn → grow lại → harvest lại sau regrowDays
```

**Phụ thuộc:** HarvestRuntime, StageRuntime
**Data:** StageModule (🔧 thêm: regrowable, regrowDays, regrowToStage)
**UI:** Không
**Events:** DieEvent bị chặn nếu regrowable → thay bằng reset
**Scene:** Plant entity giữ nguyên, sprite đổi về stage thấp

---

## 2. CHĂN NUÔI

### 2.1 Gà/Bò/Cừu (A1-A3) ✅ Framework có + cần data

**Flow:**
```
Mỗi ngày:
    → AnimalRuntime state machine:
        HUNGRY (mặc định mỗi sáng)
            → Player interact (SecondaryAction) + có feedItem trong inventory
                → Consume feedItem → state = FED
        FED
            → NextDay → state = PRODUCT_READY
        PRODUCT_READY
            → Player interact → nhận productItem → state = HUNGRY

Nếu không cho ăn:
    → daysNotFed++ (🔧 thêm)
    → daysNotFed >= 3 → DieEvent (biến mất, mất con vật)
```

**Phụ thuộc:** InventoryService, TimeManager, AnimalRuntime
**Data:** AnimalModule (feedItem, productItem, productAmount)
**UI:** Bubble icon trên đầu con vật (❗ hungry, ✓ fed, 🥚 ready)
**Events:** SecondaryActionEvent, NextDayEvent, DieEvent
**Scene:** Animal prefab trong chuồng

---

### 2.2 Chuồng trại (A4) 🆕

**Flow:**
```
Player craft Building item → đặt xuống (BuildingPlacementRuntime)
    → Validate: đủ diện tích (2×2 hoặc 3×3)? Không overlap?
    → Spawn building entity
    → Building có capacity (max animals)
    → Player mua/nhận animal → đặt vào building (nếu còn slot)
```

**Phụ thuộc:** PlacementValidator (🔧 multi-cell), SpawnSystem, InventoryService
**Data:** BuildingModule (capacity, allowedAnimalType, size)
**UI:** Không (interact building → menu "Đặt con vật vào")
**Events:** SpawnRequestPublish
**Scene:** Building prefab (multi-cell)

---

## 3. THỜI TIẾT

### 3.1 Weather System (T4) 🆕

**Flow:**
```
Mỗi đầu ngày (trong DayChangedPublish handler):
    → WeatherSystem.GenerateWeather():
        → Random theo WeatherConfig (30% mưa, 5% bão, 65% nắng)
        → Publish WeatherChangedPublish(newWeather)
    
    → If weather == Rainy:
        → WateredTileTracker.WaterAllOutdoorCells()
        → Player không cần tưới hôm nay → dành stamina cho việc khác

    → If weather == Stormy:
        → WaterAll + 5% chance mỗi cây yếu bị damage (optional, có thể bỏ)

AI Assistant (nếu unlocked):
    → Tối hôm trước: "Ngày mai có mưa" → player plan trước
```

**Phụ thuộc:** TimeManager, WateredTileTracker, AIAssistantService (optional)
**Data:** WeatherConfig (ScriptableObject: rainChance, stormChance)
**UI:** Icon thời tiết trên HUD (☀️/🌧️/⛈️), CalendarUI hiển thị forecast
**Events:** DayChangedPublish → WeatherChangedPublish (🆕)
**Scene:** (Optional) rain particle effect overlay

---

## 4. COMBAT & KHAI HOANG

### 4.1 Vùng khai hoang (C3) 🆕

**Flow:**
```
Mỗi vùng = area trong scene với:
    → N enemy entities + M obstacle entities (cây/đá)
    → ClearZoneTracker track: zoneId → {totalEntities, remainingEntities}

Khi player giết enemy hoặc phá obstacle trong vùng:
    → ClearZoneTracker.OnEntityRemoved(zoneId)
    → if remaining == 0:
        → Publish ZoneClearedPublish(zoneId)
        → Mở đất: convert vùng thành farmable (remove collision tiles)
        → Grant Mastery EXP (+50-100)
        → Narrative trigger: story event có thể fire

Vùng đã clear:
    → Enemy không respawn
    → Đất trở thành tillable
    → Player có thể cuốc/trồng ở đây
```

**Phụ thuộc:** WorldEntityService, TimeManager, NarrativeService, ProgressionService
**Data:** Zone markers trong scene (tag/layer), ClearZoneTracker state
**UI:** Minimap hiển thị vùng cleared vs uncleaned
**Events:** DieEvent (enemy) → ClearZoneTracker update → ZoneClearedPublish (🆕)
**Scene:** Enemy prefabs + Obstacle prefabs trong vùng, collision tilemap

---

### 4.2 Enemy drop nguyên liệu (C4) ✅ Có + cần data

**Flow:**
```
Enemy chết (DieEvent) → DropRuntime.Handle(DieEvent)
    → Spawn drop items theo DropModule config
    → Items: mutant material, thảo mộc, quặng đặc biệt
    → Player pickup → vào inventory
```

**Phụ thuộc:** DropRuntime (✅), PickUpObject (✅), InventoryService (✅)
**Data:** DropModule trên enemy EntityData (DropEntry[])
**UI:** Không
**Events:** DieEvent → SpawnRequestPublish (drop items)
**Scene:** Drop prefab spawned

---

### 4.3 Player chết (C9) 🆕

**Flow:**
```
Player HP <= 0 → DieEvent trên player entity
    → PlayerDeathHandler.OnPlayerDeath():
        → Fade to black
        → Respawn player tại nhà (bed position)
        → Set stamina = 50% max (penalty)
        → Skip to next day (TimeManager.SkipToNextDay)
        → Hiển thị message: "Bạn đã kiệt sức và được đưa về nhà"
        → KHÔNG mất item (hopeful tone)
```

**Phụ thuộc:** TimeManager, PlayerBridge, SaveLoadManager
**Data:** Không cần data mới
**UI:** Fade overlay + message text
**Events:** DieEvent → DayChangedPublish (skip day)
**Scene:** Không

---

## 5. CRAFTING

### 5.1 Craft tool/food/equipment (CR1-CR7) ✅ Có

**Flow:**
```
Player interact NPC Crafting → CraftingRuntime tạo option
    → Player chọn "Chế tạo" → CraftingService.Open()
    → Publish CraftingViewPublish → CraftingPanelUI hiển thị
    → Player chọn recipe → CraftingService.TryCraft()
        → Check: có đủ nguyên liệu? Mastery level đủ?
            → YES: consume materials, grant output item, +Mastery EXP
            → NO: hiển thị thiếu gì
```

**Phụ thuộc:** CraftingService (✅), InventoryService (✅), UnlockService (🔧), ProgressionService
**Data:** CraftingModule.recipes[], RecipeData (ingredients, output, masteryRequired)
**UI:** CraftingPanelUI (✅)
**Events:** SecondaryActionEvent → CraftingViewPublish
**Scene:** NPC Crafting entity

---

### 5.2 Research (CR8) 🆕

**Flow:**
```
Player interact NPC Researcher → dialogue option "Nghiên cứu"
    → ResearchService.ShowAvailable() → hiển thị danh sách research
    → Player chọn research + nộp nguyên liệu
        → ResearchService.Start(researchId):
            → Consume materials
            → Set timer (X ngày)
    
Mỗi NextDay:
    → ResearchService.AdvanceAll():
        → Giảm remaining days
        → if remaining == 0:
            → Unlock recipe mới trong CraftingService
            → Notification: "Nghiên cứu hoàn thành! Đã mở khóa: [recipe name]"
```

**Phụ thuộc:** CraftingService, InventoryService, TimeManager, NarrativeService (notification)
**Data:** ResearchData (inputMaterials[], daysNeeded, outputRecipeId)
**UI:** Dùng dialogue system (NPC) + notification khi xong. Không cần UI panel riêng.
**Events:** DayChangedPublish → research progress
**Scene:** NPC Researcher entity

---

## 6. MINING

### 6.1 Đập đá/quặng + Chặt cây (M1, M2) ✅ Có

**Flow:**
```
Player dùng Pickaxe/Axe → DamageToolRuntime → TakeDamageEvent
    → HealthRuntime trừ HP → if HP <= 0 → DieEvent
        → DropRuntime spawn ore/wood
        → MortalRuntime destroy entity
```

**Phụ thuộc:** DamageToolRuntime (✅), HealthRuntime (✅), DropRuntime (✅)
**Data:** Ore/Tree EntityData (HealthModule, DropModule, MortalModule)
**UI:** HP bar nhỏ trên đầu ore/tree khi bị đánh
**Events:** PrimaryActionEvent → TakeDamageEvent → DieEvent
**Scene:** Ore/Tree prefabs trong Mine/Wilderness scene

---

### 6.2 Tài nguyên respawn (M6) ✅ Có

**Flow:**
```
Entity chết → RespawnRuntime.Handle(DieEvent):
    → Despawn entity (ẩn)
    → Start timer (respawnDelay ngày)
    → Mỗi NextDay: timer--
    → if timer == 0: respawn tại vị trí gốc, restore HP
```

**Phụ thuộc:** TimeManager, SpawnSystem
**Data:** RespawnModule (delay, restoreHP, position)
**Events:** DieEvent → DespawnRequestPublish → (sau X ngày) SpawnRequestPublish
**Scene:** Entity reappear tại vị trí cũ

---

## 7. PROGRESSION

### 7.1 Farming Mastery (P1) 🔧

**Flow:**
```
Mỗi khi thu hoạch/craft/quest/khai hoang:
    → ProgressionService.GrantMasteryExp(player, amount, source)
        → Cộng EXP → check level up
        → if level up:
            → +MaxStamina (chính)
            → Check MasteryUnlockData: unlock recipe/cây/AI feature
            → Publish LevelUpPublish
            → Notification: "Mastery lên level X! Mở khóa: [feature]"

EXP sources (ưu tiên farming):
    → Thu hoạch: +10-30 (chính)
    → Hoàn thành quest: +20-50
    → Khai hoang vùng: +50-100
    → Craft: +5-10
    → Combat kill: +2-5 (rất ít)
```

**Phụ thuộc:** ProgressionService (🔧), UnlockService (🔧), CraftingService, ShopService
**Data:** MasteryUnlockData (level → unlock list), EXP formula
**UI:** Mastery bar trên HUD, level up popup
**Events:** ProgressionChangedPublish, LevelUpPublish
**Scene:** Không

---

## 8. NARRATIVE

### 8.1 Story Events (ST1-ST7) 🆕

**Flow:**
```
NarrativeService check mỗi đầu ngày:
    → Duyệt List<StoryEventData>:
        → if triggerCondition met (day >= X, zone cleared, mastery >= Y):
            → if not already triggered:
                → Mark triggered
                → Deliver content theo deliveryType:
                    DIARY → DiaryUI popup (2-3 dòng)
                    MESSAGE → MessageNotificationUI (tin nhắn)
                    NEWS → NewsBroadcastUI (text overlay)
                    NPC_DIALOGUE → unlock dialogue mới trên NPC
                    LORE_ITEM → spawn lore item trong vùng mới clear

Ví dụ timeline trong game:
    Day 7: NEWS "Phát hiện sinh vật lạ ở tỉnh lân cận"
    Day 9: MESSAGE từ bạn cũ "Ê mày có nghe tin không?"
    Day 10: Mutant xuất hiện (gameplay trigger)
    Day 15: DIARY "Thế giới đang thay đổi... nhưng trang trại vẫn cần tưới"
    Day 20: LORE_ITEM tìm được trong vùng 1 (tài liệu phòng thí nghiệm)
```

**Phụ thuộc:** TimeManager, ClearZoneTracker, ProgressionService
**Data:** StoryEventData[] (triggerType, condition, deliveryType, content)
**UI:** DiaryUI, MessageNotificationUI, NewsBroadcastUI (tất cả 🆕)
**Events:** DayChangedPublish → NarrativeService check → StoryEventTriggeredPublish (🆕)
**Scene:** Lore item entities spawned khi zone cleared

---

## 9. AI ASSISTANT

### 9.1 Gợi ý + Dự báo + Nhắc nhở (AI1, AI2, AI4) 🆕

**Flow:**
```
AIAssistantService chạy mỗi giờ game (GameHourChangedPublish):
    → Tính toán hints dựa trên game state:
        → Cây nào chưa tưới? → "Còn 3 cây chưa tưới"
        → Cây nào sắp chín? → "Cà chua chín ngày mai"
        → Thời tiết ngày mai? → "Ngày mai mưa, không cần tưới"
        → Giá bán tốt? → (nếu có price fluctuation)
    → Publish AIHintChangedPublish(hintText)
    → AIAssistantUI hiển thị hint mới nhất

Unlock theo Mastery:
    → Mastery 1: không có AI
    → Mastery 2: nhắc tưới
    → Mastery 3: dự báo thời tiết
    → Mastery 5+: gợi ý nâng cao
```

**Phụ thuộc:** WateredTileTracker, WeatherSystem, TimeManager, ProgressionService
**Data:** Không cần SO riêng — logic if/else đọc game state
**UI:** AIAssistantUI (🆕) — panel nhỏ góc phải, 1-2 dòng text
**Events:** GameHourChangedPublish → AIHintChangedPublish (🆕)
**Scene:** Không

---

## 10. SLEEP / END DAY

### 10.1 Ngủ kết thúc ngày 🆕

**Flow:**
```
Player interact Bed → SecondaryActionEvent → dialogue "Đi ngủ?"
    → Player confirm:
        → Fade to black
        → TimeManager.SkipToNextDay()
        → Restore stamina = MaxStamina
        → WateredTileTracker.ResetAll()
        → WeatherSystem.GenerateWeather()
        → NarrativeService.CheckEvents()
        → All entities receive NextDayEvent
        → EndOfDaySummaryUI hiển thị:
            - Tiền kiếm hôm nay
            - Cây phát triển
            - Mastery gained
            - Thời tiết ngày mai
        → Fade in → ngày mới bắt đầu
```

**Phụ thuộc:** TimeManager, WateredTileTracker, WeatherSystem, NarrativeService, DailyTracker, ProgressionService
**Data:** Bed EntityData (có DialogueModule)
**UI:** EndOfDaySummaryUI (🆕), fade overlay
**Events:** SecondaryActionEvent → DayChangedPublish → NextDayEvent (broadcast)
**Scene:** Bed entity trong Farm scene

---

## DEPENDENCY MAP (Hệ thống nào phụ thuộc hệ thống nào)

```
TimeManager (gốc — không phụ thuộc ai)
    ├── WeatherSystem (cần TimeManager)
    ├── WateredTileTracker (cần TimeManager để reset)
    ├── NarrativeService (cần TimeManager để check day)
    ├── ResearchService (cần TimeManager để progress)
    ├── AIAssistantService (cần TimeManager để refresh)
    └── DailyTracker (cần TimeManager để reset)

WateredTileTracker (cần TimeManager)
    ├── WateringCanRuntime (ghi vào)
    ├── WeatherSystem (ghi vào khi mưa)
    ├── SprinklerRuntime (ghi vào mỗi ngày)
    ├── StageRuntime (đọc để check grow)
    └── AIAssistantService (đọc để nhắc nhở)

WorldEntityService (gốc)
    ├── HoeRuntime (SetGround)
    ├── WateringCanRuntime (SetGround visual)
    ├── PlacementRuntime (TryRegisterSpawn)
    ├── ClearZoneTracker (convert vùng)
    └── SpawnSystem (register/unregister)

ProgressionService (cần EventBus)
    ├── HarvestRuntime (grant EXP khi thu hoạch)
    ├── CraftingService (grant EXP khi craft)
    ├── QuestService (grant EXP khi complete)
    ├── ClearZoneTracker (grant EXP khi clear)
    └── UnlockService (check level để unlock)

InventoryService (gốc)
    ├── ShopService (buy/sell)
    ├── CraftingService (consume/grant)
    ├── HarvestRuntime (pickup drops)
    ├── FertilizerRuntime (consume)
    ├── AnimalRuntime (consume feed, grant product)
    └── ResearchService (consume materials)

EventBus (gốc — mọi thứ dùng)
    └── Tất cả Publish/Subscribe events
```
