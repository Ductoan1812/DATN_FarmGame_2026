# 05 — PROTOTYPE PLAN

> Mục tiêu: Xác định chính xác cần BUILD gì tiếp theo, theo thứ tự nào, dựa trên những gì ĐÃ CÓ.

---

## HIỆN TRẠNG: Đã có gì?

### ✅ Hoàn chỉnh (không cần động)
| Hệ thống | Chi tiết |
|----------|----------|
| Entity + Module architecture | EntityData → EntityRuntime → modules[] → events |
| Input routing | PlayerControler → ActionRuntime → forward PrimaryAction/SecondaryAction |
| Tool pipeline | ToolRuntime → Validate → ToolActionBridge → animation → AnimStrikeEvent → Execute |
| Cuốc đất | HoeRuntime → SetGround(plowedTile) |
| Gieo hạt | PlacementRuntime → SpawnRequestPublish → plant entity |
| Cây phát triển | StageRuntime → NextDayEvent → stage++ → sprite change |
| Thu hoạch (tool) | DamageToolRuntime → TakeDamageEvent → HarvestRuntime gate → DieEvent → DropRuntime |
| Thu hoạch (tay) | SecondaryAction → HarvestRuntime → DieEvent |
| Combat framework | WeaponRuntime, DamageToolRuntime, HealthRuntime, DieEvent pipeline |
| Enemy AI | EnemyObject (chase/attack state machine) |
| Drop system | DropRuntime → SpawnRequestPublish → DropMotionObject → PickUpObject |
| Inventory | InventoryRuntime (Hotbar + Backpack), drag-drop, stack/split |
| Equipment | EquipmentRuntime, stat bonuses, HeroEditor4D visual |
| Shop | ShopService.TryBuy/TrySell, ShopPanelUI |
| Crafting | CraftingService.TryCraft, CraftingPanelUI, 13 recipes |
| Quest | QuestService, QuestLogRuntime, 15 quests, dialogue integration |
| Dialogue | DialogueService, graph traversal, condition evaluation |
| NPC interaction | SecondaryAction → InteractionContext → options menu |
| Time | TimeManager (hour/day/season/year), DayChangedPublish, 14min/day |
| Day/Night | DayNightLightController, NormalizedTime |
| Scene transition | ScenePortalRuntime → SceneTransitionService → spawn point |
| Save/Load | SaveLoadManager (entities, world, time) |
| Progression | ProgressionService (L1-50, EXP formula, stat growth) |
| Animal (gà) | AnimalRuntime (Hungry→Fed→ProductReady), 1 chicken defined |
| Dodge | PlayerControler.DodgeRoutine (shift, stamina cost, i-frames) |
| Debug console | set, give, spawn, save, load, scene commands |
| 3 scenes | FarmScene, TownScene, MineScene |

### ❌ Chưa có (cần build)
| Hệ thống | Ưu tiên | Lý do |
|----------|---------|-------|
| Tưới nước (WateringCanRuntime) | 🔴 Critical | Farming ritual hàng ngày — core loop thiếu |
| Watered tracking (WateredTileTracker) | 🔴 Critical | Cây cần tưới mới lớn |
| Cây cần tưới mới grow | 🔴 Critical | StageRuntime check watered |
| Cây héo/chết | 🔴 Critical | Survival pressure |
| Sleep/End day (BedRuntime) | 🔴 Critical | Kết thúc vòng lặp 1 ngày |
| Stamina cost farming tools | 🔴 Critical | Resource tension |
| Weather (WeatherSystem) | 🟡 High | Variety + auto-water khi mưa |
| Phân bón (FertilizerRuntime) | 🟡 High | Tăng tốc/quality |
| Chất lượng nông sản (QualityRuntime) | 🟡 High | Reward farming cẩn thận |
| Sprinkler (SprinklerRuntime) | 🟡 High | Automation reward |
| Mastery rename + rebalance | 🟡 High | Farming = EXP chính |
| Khai hoang (ClearZoneTracker) | 🟡 High | Mở đất mới |
| Player chết (PlayerDeathHandler) | 🟡 High | Hậu quả combat |
| Bò + Cừu | 🟢 Medium | Mở rộng chăn nuôi |
| Chuồng trại (BuildingPlacement) | 🟢 Medium | Multi-cell placement |
| Research (ResearchService) | 🟢 Medium | Unlock recipe qua thời gian |
| Đất chất lượng (SoilQualityTracker) | 🟢 Medium | Depth cho farming |
| Cây tái thu hoạch | 🟢 Medium | Variety cây trồng |
| Narrative (NarrativeService) | 🟢 Medium | Story delivery |
| AI Assistant | 🟢 Medium | Hints/tips |
| End-of-day summary UI | 🟢 Medium | Satisfaction feedback |
| Calendar UI | 🟢 Medium | Thông tin thời gian |
| Minimap | 🔵 Low | Nice-to-have |
| Tutorial | 🔵 Low | Onboarding |

---

## PROTOTYPE PLAN: 5 Sprints

### Sprint 1: Farming Loop Hoàn Chỉnh (Ưu tiên cao nhất)
> **Mục tiêu:** Người chơi có thể chơi 1 ngày farming hoàn chỉnh: thức dậy → tưới cây → cây lớn → thu hoạch → bán → ngủ → lặp lại.
> **Khi xong:** Core farming loop playable, có tension (stamina), có reward (tiền + mastery).

| # | Task | Script/File | Phụ thuộc | Estimate |
|---|------|-------------|-----------|----------|
| 1.1 | Tạo WateringCanRuntime | Data/Runtime/WateringCanRuntime.cs | ToolRuntime (base), GridSystem | 2h |
| 1.2 | Tạo WateredTileTracker | Core/Service/WateredTileTracker.cs | TimeManager (reset), WorldEntityService | 2h |
| 1.3 | Sửa TileData — thêm wateredTile | Data/Structs/TileData.cs | Cần assign tile asset | 30m |
| 1.4 | Sửa StageRuntime — check watered | Data/Runtime/StageRuntime.cs | WateredTileTracker | 1h |
| 1.5 | Sửa StageRuntime — wilt logic | Data/Runtime/StageRuntime.cs | (1.4 xong trước) | 1h |
| 1.6 | Sửa StageModule — thêm wiltSprite | Data/Module/StageModule.cs | Cần sprite asset | 30m |
| 1.7 | Tạo BedRuntime (Sleep/End day) | Data/Runtime/BedRuntime.cs | TimeManager.AdvanceToNextDay() | 2h |
| 1.8 | Thêm stamina cost cho HoeRuntime | Data/Runtime/HoeRuntime.cs | Stats (Stamina) | 30m |
| 1.9 | Thêm stamina cost cho WateringCanRuntime | (đã có từ 1.1) | | 0 |
| 1.10 | Tạo WateringCan EntityData | ScriptableObject asset | ToolModule config | 30m |
| 1.11 | Tạo Bed EntityData | ScriptableObject asset | BedModule config | 30m |
| 1.12 | Sửa SaveLoadManager — save watered state | Systems/SaveLoadManager.cs | WateredTileTracker | 1h |
| 1.13 | Scene setup: đặt Bed + WateringCan trong farm | Unity Editor | Prefab + scene | 30m |

**Tổng estimate:** ~12 giờ
**Test khi xong:** Cuốc → gieo → tưới → ngủ → cây lớn. Không tưới → cây héo. Hết stamina → không làm được.

---

### Sprint 2: Weather + Survival Pressure
> **Mục tiêu:** Thêm variety cho farming (mưa = không cần tưới), thêm depth (phân bón, quality), thêm feedback (end-of-day summary).
> **Khi xong:** Mỗi ngày khác nhau (weather), farming có chiều sâu (quality), player có feedback rõ ràng.

| # | Task | Script/File | Phụ thuộc | Estimate |
|---|------|-------------|-----------|----------|
| 2.1 | Tạo WeatherType enum | Data/Enums/WeatherType.cs | | 15m |
| 2.2 | Tạo WeatherConfig SO | Data/Structs/WeatherConfig.cs | | 30m |
| 2.3 | Tạo WeatherSystem | Core/Service/WeatherSystem.cs | TimeManager, WateredTileTracker | 2h |
| 2.4 | Sửa WateredTileTracker — subscribe WeatherChanged | Core/Service/WateredTileTracker.cs | WeatherSystem | 30m |
| 2.5 | Tạo FertilizerRuntime | Data/Runtime/FertilizerRuntime.cs | StageRuntime, EntityService | 1.5h |
| 2.6 | Tạo QualityModule + QualityRuntime | Data/Module + Runtime | WateredTileTracker, StageRuntime | 2h |
| 2.7 | Sửa HarvestRuntime — set quality trên drop | Data/Runtime/HarvestRuntime.cs | QualityRuntime | 1h |
| 2.8 | Sửa ShopService — quality price multiplier | Core/Service/ShopService.cs | EntityRuntime quality field | 30m |
| 2.9 | Tạo DailyTracker | Core/Service/DailyTracker.cs | EventBus subscriptions | 1h |
| 2.10 | Tạo EndOfDaySummaryUI | UI/EndOfDaySummaryUI.cs | DailyTracker, BedRuntime | 2h |
| 2.11 | HUD — thêm weather icon | UI/HudStatusMapUI.cs (sửa) | WeatherSystem | 30m |
| 2.12 | Tạo Fertilizer EntityData | ScriptableObject asset | | 30m |
| 2.13 | Sửa SaveLoadManager — save weather | Systems/SaveLoadManager.cs | WeatherSystem | 30m |

**Tổng estimate:** ~13 giờ
**Test khi xong:** Mưa → cây tự tưới. Bón phân → cây lớn nhanh. Thu hoạch → quality ★. Ngủ → summary hiện.

---

### Sprint 3: Progression Rebalance + Automation
> **Mục tiêu:** Farming = nguồn EXP chính. Mastery unlock features. Sprinkler = reward cho progression.
> **Khi xong:** Player thấy rõ: farm nhiều → level up → unlock tool/recipe/sprinkler → farm hiệu quả hơn.

| # | Task | Script/File | Phụ thuộc | Estimate |
|---|------|-------------|-----------|----------|
| 3.1 | Sửa ProgressionService — rebalance EXP sources | Core/Service/ProgressionService.cs | | 1h |
| 3.2 | Tạo MasteryUnlockData SO | Data/Structs/MasteryUnlockData.cs | | 1h |
| 3.3 | Sửa UnlockService — check mastery unlock table | Core/Service/UnlockService.cs | MasteryUnlockData | 1.5h |
| 3.4 | Sửa ProgressionService — OnLevelUp call UnlockService | Core/Service/ProgressionService.cs | UnlockService | 1h |
| 3.5 | Tạo SprinklerModule + SprinklerRuntime | Data/Module + Runtime | WateredTileTracker, TimeManager | 2h |
| 3.6 | Tạo Sprinkler EntityData (T1, T2) | ScriptableObject assets | SprinklerModule | 30m |
| 3.7 | Sửa StageModule — regrowable support | Data/Module/StageModule.cs | | 30m |
| 3.8 | Sửa StageRuntime — regrow logic | Data/Runtime/StageRuntime.cs | | 1h |
| 3.9 | Sửa HarvestRuntime — regrow (skip MortalRuntime) | Data/Runtime/HarvestRuntime.cs | StageRuntime | 1h |
| 3.10 | Tạo SoilQualityTracker | Core/Service/SoilQualityTracker.cs | | 1h |
| 3.11 | Sửa FertilizerRuntime — increment soil quality | Data/Runtime/FertilizerRuntime.cs | SoilQualityTracker | 30m |
| 3.12 | Cập nhật ExpRewardModule trên plant entities | ScriptableObject assets | | 30m |

**Tổng estimate:** ~12 giờ
**Test khi xong:** Thu hoạch → +20 Mastery. Level up → unlock sprinkler recipe. Craft sprinkler → đặt → auto-water. Cây regrow sau harvest.

---

### Sprint 4: Combat Integration + Khai Hoang
> **Mục tiêu:** Combat có mục đích (khai hoang mở đất, lấy nguyên liệu). Player chết có hậu quả nhẹ.
> **Khi xong:** Player có lý do rời farm (cần nguyên liệu), combat phục vụ farming.

| # | Task | Script/File | Phụ thuộc | Estimate |
|---|------|-------------|-----------|----------|
| 4.1 | Tạo ClearZoneTracker | Core/Service/ClearZoneTracker.cs | EventBus, WorldEntityService | 2h |
| 4.2 | Sửa WorldEntityService — zone clear → unlock tiles | Core/Service/WorldEntityService.cs | ClearZoneTracker | 1h |
| 4.3 | Tạo PlayerDeathHandler | Systems/PlayerDeathHandler.cs | TimeManager, HealthRuntime | 2h |
| 4.4 | Sửa AnimalRuntime — daysNotFed + death | Data/Runtime/AnimalRuntime.cs | | 1h |
| 4.5 | Tạo Bò + Cừu EntityData | ScriptableObject assets | AnimalModule | 30m |
| 4.6 | Tạo BuildingModule + BuildingPlacementRuntime | Data/Module + Runtime | PlacementValidator, SpawnSystem | 3h |
| 4.7 | Sửa PlacementValidator — multi-cell | Systems/PlacementValidator.cs | | 1h |
| 4.8 | Tạo Building EntityData (ChickenCoop, CowBarn) | ScriptableObject assets | BuildingModule | 30m |
| 4.9 | Scene setup: ClearZone ở farm edge | Unity Editor | Enemy + obstacle prefabs | 1h |
| 4.10 | Tạo mutant material EntityData (drops) | ScriptableObject assets | | 30m |
| 4.11 | Cập nhật enemy DropModule — drop mutant materials | ScriptableObject assets | | 30m |
| 4.12 | Sửa SaveLoadManager — save zones | Systems/SaveLoadManager.cs | ClearZoneTracker | 1h |

**Tổng estimate:** ~14 giờ
**Test khi xong:** Đánh mutant → drop material. Clear zone → đất mở. Chết → respawn nhà, mất stamina. Xây chuồng → nuôi bò.

---

### Sprint 5: Narrative + AI Assistant + Polish
> **Mục tiêu:** Game có story (progressive reveal), có helper (AI tips), có onboarding cơ bản.
> **Khi xong:** Game cảm thấy hoàn chỉnh — có mục đích, có hướng dẫn, có cốt truyện.

| # | Task | Script/File | Phụ thuộc | Estimate |
|---|------|-------------|-----------|----------|
| 5.1 | Tạo NarrativeService | Core/Service/NarrativeService.cs | TimeManager, EventBus | 3h |
| 5.2 | Tạo StoryEventData SO | Data/Structs/StoryEventData.cs | | 1h |
| 5.3 | Tạo 5-10 story events (Day 7, 10, 14...) | ScriptableObject assets | NarrativeService | 2h |
| 5.4 | Tạo DiaryUI | UI/DiaryUI.cs | NarrativeService | 1.5h |
| 5.5 | Tạo MessageNotificationUI | UI/MessageNotificationUI.cs | NarrativeService | 1.5h |
| 5.6 | Tạo NewsBroadcastUI | UI/NewsBroadcastUI.cs | NarrativeService | 1.5h |
| 5.7 | Tạo AIAssistantService | Core/Service/AIAssistantService.cs | WateredTileTracker, WeatherSystem, ProgressionService | 2h |
| 5.8 | Tạo AIAssistantUI | UI/AIAssistantUI.cs | AIAssistantService | 1.5h |
| 5.9 | Tạo ResearchService | Core/Service/ResearchService.cs | TimeManager, UnlockService | 2h |
| 5.10 | Tạo ResearchData SO + 5 research entries | ScriptableObject assets | | 1h |
| 5.11 | Tạo CalendarUI | UI/CalendarUI.cs | TimeManager, WeatherSystem | 1.5h |
| 5.12 | Sửa SaveLoadManager — save narrative + research | Systems/SaveLoadManager.cs | | 1h |

**Tổng estimate:** ~20 giờ
**Test khi xong:** Day 7 → news xuất hiện. Day 10 → mutant spawn. AI gợi ý tưới. Research unlock recipe sau 3 ngày.

---

## TỔNG KẾT

| Sprint | Thời gian | Kết quả |
|--------|-----------|---------|
| 1 | ~12h | Farming loop playable (tưới, héo, ngủ, stamina) |
| 2 | ~13h | Weather + quality + summary |
| 3 | ~12h | Mastery progression + sprinkler + regrow |
| 4 | ~14h | Combat có mục đích + khai hoang + buildings |
| 5 | ~20h | Story + AI + research + polish |
| **TỔNG** | **~71h** | **Prototype hoàn chỉnh** |

---

## THỨ TỰ DEPENDENCIES (không thể đảo)

```
Sprint 1 (bắt buộc trước)
    │
    ├── 1.1-1.3: WateringCan + Tracker + TileData
    │       → 1.4: StageRuntime check watered (phụ thuộc Tracker)
    │       → 1.5: Wilt logic (phụ thuộc 1.4)
    │
    ├── 1.7: BedRuntime (phụ thuộc TimeManager — đã có)
    │
    └── 1.8: Stamina cost (phụ thuộc Stats — đã có)

Sprint 2 (phụ thuộc Sprint 1)
    │
    ├── 2.3: WeatherSystem (phụ thuộc WateredTileTracker từ Sprint 1)
    ├── 2.5: Fertilizer (phụ thuộc StageRuntime sửa từ Sprint 1)
    └── 2.6: Quality (phụ thuộc WateredTileTracker từ Sprint 1)

Sprint 3 (phụ thuộc Sprint 2)
    │
    ├── 3.5: Sprinkler (phụ thuộc WateredTileTracker)
    └── 3.10: SoilQuality (phụ thuộc FertilizerRuntime từ Sprint 2)

Sprint 4 (có thể song song với Sprint 3 phần lớn)
    │
    └── 4.6: Building (phụ thuộc PlacementValidator — đã có)

Sprint 5 (phụ thuộc Sprint 1-4 hoàn thành)
    │
    └── 5.7: AI Assistant (phụ thuộc WeatherSystem + WateredTileTracker)
```

---

## BẮT ĐẦU TỪ ĐÂU?

**Task đầu tiên:** Sprint 1, Task 1.1 — Tạo `WateringCanRuntime.cs`

Khi bạn sẵn sàng, nói "bắt đầu Sprint 1" và tôi sẽ implement từng task.
