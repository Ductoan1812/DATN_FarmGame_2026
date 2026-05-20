# 🤖 AI ASSISTANT SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** AIAssistantSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Gợi ý cây trồng
> 2. Dự báo thời tiết
> 3. Nhắc nhở tưới
> 4. Unlock theo Mastery
> 5. UI panel

---

## TỔNG QUAN

AI Assistant là helper in-game, hiện tips/gợi ý ngắn gọn (1-2 dòng) ở góc màn hình. Không phải AI thật — chỉ là rule-based system đọc game state và hiện text phù hợp. Unlock dần theo Mastery level.

---

## 1. GỢI Ý CÂY TRỒNG

### Mô tả
AI đọc soil quality + player gold + season → suggest cây trồng tốt nhất (profit/day cao nhất mà player đủ tiền mua seed). Hiện tip: "Nên trồng Cà Chua — lợi nhuận 45G/ngày."

### Điều kiện
- Mastery level >= 5 (unlock advanced tips)
- Player có ô đất trống (plowed, no plant)
- Đầu ngày mới hoặc player mở AI panel

### Flow
```
AIAssistantService.GenerateCropTip():
    1. Get available seeds from ShopService (player can afford)
    2. foreach seed:
        - Calculate profitPerDay = (sellPrice - seedCost) / growDays
        - Check season compatibility
        - Check soil quality bonus (if SoilQualityTracker available)
    3. Sort by profitPerDay descending
    4. bestCrop = top result
    5. return TipData("Nên trồng {bestCrop.name} — lợi nhuận {profitPerDay}G/ngày")

Trigger:
    - DayChangedPublish → refresh tip
    - GameHourChangedPublish (mỗi giờ) → check if tip still relevant
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AIAssistantService 🆕 | Generate tips |
| ShopService | Available seeds + prices |
| ProgressionService | Check mastery level (unlock) |
| SoilQualityTracker | Soil bonus (optional) |
| TimeManager | Season, trigger refresh |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| CropDatabase | ScriptableObject[] | All crop EntityData (growDays, sellPrice, season) |
| AITipConfig | ScriptableObject | cropTipUnlockLevel=5 |

### UI
- AIAssistantUI panel: "💡 Nên trồng Cà Chua — lợi nhuận 45G/ngày"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DayChangedPublish | TimeManager | AIAssistantService (refresh tips) |
| GameHourChangedPublish | TimeManager | AIAssistantService (periodic refresh) |

### Scene Objects
- AIAssistantService (singleton)

---

## 2. DỰ BÁO THỜI TIẾT

### Mô tả
AI đọc WeatherSystem.GetTomorrow() → hiện dự báo: "Ngày mai: Mưa 🌧️ — không cần tưới!" Giúp player plan ahead (không cần tưới nếu mai mưa).

### Điều kiện
- Mastery level >= 3 (unlock weather forecast)
- WeatherSystem có GetTomorrow() method

### Flow
```
AIAssistantService.GenerateWeatherTip():
    1. Weather tomorrow = WeatherSystem.GetTomorrow()
    2. switch tomorrow:
        - Sunny: return "Ngày mai: Nắng ☀️ — nhớ tưới cây!"
        - Rainy: return "Ngày mai: Mưa 🌧️ — không cần tưới!"
        - Stormy: return "Ngày mai: Bão ⛈️ — cẩn thận khi ra ngoài!"
    
Trigger:
    - Mỗi chiều (hour >= 16) → show forecast
    - Player mở AI panel → show forecast
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AIAssistantService | Generate tip |
| WeatherSystem | GetTomorrow() |
| ProgressionService | Check unlock level |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| AITipConfig | ScriptableObject | weatherTipUnlockLevel=3, weatherTipStartHour=16 |

### UI
- AIAssistantUI panel: "🌧️ Ngày mai: Mưa — không cần tưới!"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| GameHourChangedPublish | TimeManager | AIAssistantService (check if afternoon) |

### Scene Objects
- (Shared with AIAssistantService)

---

## 3. NHẮC NHỞ TƯỚI

### Mô tả
AI đọc WateredTileTracker → đếm số cây chưa được tưới hôm nay → nhắc: "Còn 5 cây chưa tưới!" Hiện từ sáng, update mỗi giờ.

### Điều kiện
- Mastery level >= 2 (unlock watering reminder)
- Có plant entities chưa được tưới
- Thời tiết không phải Rainy (nếu mưa → không cần nhắc)

### Flow
```
AIAssistantService.GenerateWateringTip():
    1. if WeatherSystem.CurrentWeather == Rainy:
        → return null (không cần nhắc, mưa tưới rồi)
    2. int totalPlants = WorldEntityService.GetAllPlants().Count
    3. int wateredCount = WateredTileTracker.GetWateredPlantCount()
    4. int unwatered = totalPlants - wateredCount
    5. if unwatered > 0:
        → return "Còn {unwatered} cây chưa tưới!"
    6. if unwatered == 0 && totalPlants > 0:
        → return "✅ Đã tưới hết! Tốt lắm!"
    7. return null (no plants)

Trigger:
    - GameHourChangedPublish → refresh count
    - After player waters → refresh (via WateredTileTracker change)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AIAssistantService | Generate tip |
| WateredTileTracker | Count watered/unwatered |
| WorldEntityService | Count total plants |
| WeatherSystem | Check if rainy |
| ProgressionService | Check unlock level |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| AITipConfig | ScriptableObject | wateringTipUnlockLevel=2 |

### UI
- AIAssistantUI panel: "💧 Còn 5 cây chưa tưới!"
- Hoặc: "✅ Đã tưới hết!"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| GameHourChangedPublish | TimeManager | AIAssistantService |

### Scene Objects
- (Shared)

---

## 4. UNLOCK THEO MASTERY

### Mô tả
AI Assistant features unlock dần theo Mastery level. Level thấp = basic tips. Level cao = advanced analysis. Tránh overwhelm player mới.

### Điều kiện
- ProgressionService.CurrentMastery >= required level per feature

### Flow
```
AIAssistantService.GetAvailableTips():
    int mastery = ProgressionService.CurrentMastery
    
    List<TipGenerator> available = new()
    
    if mastery >= 2: available.Add(WateringReminder)
    if mastery >= 3: available.Add(WeatherForecast)
    if mastery >= 5: available.Add(CropSuggestion)
    if mastery >= 8: available.Add(AnimalReminder)    // "Gà đang đói!"
    if mastery >= 10: available.Add(MineFloorTip)     // "Tầng 4+ có Iron Ore"
    if mastery >= 15: available.Add(ProfitAnalysis)   // "Tuần này lãi 2000G"
    
    // Priority: urgent tips first (watering > weather > crop)
    return available.OrderByPriority().First().Generate()
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ProgressionService | Current mastery level |
| AIAssistantService | Feature gating |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| AITipConfig | ScriptableObject | unlockTable[{feature, requiredMastery}] |

### UI
- New tip unlock notification: "AI Assistant mới: Dự báo thời tiết!"
- Tips greyed/hidden until unlocked

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| MasteryLevelUpPublish | ProgressionService | AIAssistantService (check new unlocks) |

### Scene Objects
- (Shared)

---

## 5. UI PANEL

### Mô tả
AIAssistantUI là panel nhỏ ở bottom-right màn hình. Hiện 1-2 dòng text (tip hiện tại). Refresh mỗi giờ game hoặc khi có tip mới urgent. Player có thể toggle on/off.

### Điều kiện
- AI Assistant unlocked (Mastery >= 2)
- Panel enabled (player setting)

### Flow
```
AIAssistantUI (MonoBehaviour):
    
    OnEnable():
        - Subscribe: GameHourChangedPublish, DayChangedPublish, MasteryLevelUpPublish
    
    RefreshTip():
        1. string tip = AIAssistantService.GetCurrentTip()
        2. if tip != currentDisplayedTip:
            - Animate text change (fade out old → fade in new)
            - currentDisplayedTip = tip
    
    Layout:
        - Position: bottom-right, above hotbar
        - Size: 250x60px
        - Background: semi-transparent dark
        - Text: 1-2 lines, small font, white
        - Icon: 💡 or relevant emoji
        - Close button (X) to dismiss current tip
        - Toggle button in settings to disable entirely

    Priority system (show most urgent):
        1. Watering reminder (if unwatered > 0)
        2. Weather forecast (afternoon)
        3. Crop suggestion (morning)
        4. General tips (rotate)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| AIAssistantService | Provide tips |
| AIAssistantUI 🆕 | Display panel |
| TimeManager | Refresh trigger |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| AIAssistantUI Prefab | GameObject | Panel, Text (TMP), Icon, CloseButton |

### UI
- Small panel bottom-right
- 1-2 lines text
- Semi-transparent background
- Dismiss button
- Settings toggle

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| GameHourChangedPublish | TimeManager | AIAssistantUI (refresh) |
| DayChangedPublish | TimeManager | AIAssistantUI (refresh) |
| AITipChangedPublish 🆕 | AIAssistantService | AIAssistantUI (immediate update) |

### Scene Objects
- AIAssistantUI prefab (Canvas, bottom-right)
- Settings UI (toggle option)

---

## TỔNG KẾT: AI ASSISTANT SYSTEM DEPENDENCY

```
ProgressionService (Mastery Level)
    │
    ▼
AIAssistantService (Feature Gating + Tip Generation)
    │
    ├── Mastery >= 2: WateringReminder
    │       └── Reads: WateredTileTracker, WorldEntityService, WeatherSystem
    │
    ├── Mastery >= 3: WeatherForecast
    │       └── Reads: WeatherSystem.GetTomorrow()
    │
    ├── Mastery >= 5: CropSuggestion
    │       └── Reads: ShopService, CropDatabase, SoilQualityTracker
    │
    ├── Mastery >= 8: AnimalReminder
    │       └── Reads: AnimalRuntime states
    │
    └── Mastery >= 10+: Advanced tips
            └── Reads: Various systems

TimeManager
    │
    ├── GameHourChangedPublish → AIAssistantService.RefreshTip()
    └── DayChangedPublish → AIAssistantService.RefreshTip()

AIAssistantService
    │
    ▼
AITipChangedPublish
    │
    ▼
AIAssistantUI (bottom-right panel)
    │
    ├── Display current tip (1-2 lines)
    ├── Priority: urgent first (watering > weather > crop)
    ├── Refresh every game hour
    └── Player can dismiss/toggle off
```
