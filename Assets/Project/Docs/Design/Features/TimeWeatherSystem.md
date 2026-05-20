# 🕐 TIME & WEATHER SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** TimeWeatherSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Đồng hồ game
> 2. Ngày/đêm visual
> 3. Sleep/End day
> 4. Weather random
> 5. Mưa auto-water
> 6. Kiệt sức (forced sleep)

---

## TỔNG QUAN

TimeManager là service trung tâm, publish events mà hầu hết systems khác subscribe. 14 phút thực = 1 ngày game. Weather random mỗi ngày mới, ảnh hưởng farming (mưa = auto-water).

---

## 1. ĐỒNG HỒ GAME

### Mô tả
TimeManager track thời gian game: ngày, giờ, phút. 14 phút thực = 1 ngày (06:00 → 02:00 = 20 giờ game). Mỗi giờ game publish event. Ngày mới bắt đầu lúc 06:00.

### Điều kiện
- Game đang chạy (không pause)
- TimeManager active

### Flow
```
TimeManager.Update():
    1. elapsedRealTime += Time.deltaTime
    2. gameMinutes = elapsedRealTime * (20 * 60) / (14 * 60)
       → 1 real second ≈ 1.43 game minutes
    3. currentHour = 6 + (gameMinutes / 60)
    4. currentMinute = gameMinutes % 60
    
    5. if currentHour changed:
        → EventBus.Publish(GameHourChangedPublish(currentHour))
    
    6. if currentHour >= 26 (02:00 next day):
        → Forced sleep (xem chức năng 6)
    
    7. NormalizedTime = (currentHour - 6) / 20.0
       → 0.0 = 06:00 (sáng), 0.5 = 16:00 (chiều), 1.0 = 02:00 (khuya)

TimeManager.AdvanceToNextDay():
    1. currentDay++
    2. currentHour = 6, currentMinute = 0
    3. elapsedRealTime = 0
    4. EventBus.Publish(DayChangedPublish(currentDay, currentSeason))
    5. Check season change: if currentDay % 28 == 0:
        → currentSeason = next
        → EventBus.Publish(SeasonChangedPublish(newSeason))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| TimeManager | Core time tracking (đã có) |
| EventBus | Publish time events |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| TimeConfig | ScriptableObject | realMinutesPerDay=14, dayStartHour=6, dayEndHour=26, daysPerSeason=28 |

### UI
- Clock display (HUD top-right): "Day 5 — 14:30"
- Season indicator: "Spring"
- Time speed indicator (optional)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| GameHourChangedPublish | TimeManager | AnimalRuntime, AIAssistant, NPC schedules |
| DayChangedPublish | TimeManager | StageRuntime, WateredTileTracker, AnimalRuntime, RespawnRegistry, WeatherSystem |
| SeasonChangedPublish | TimeManager | CropGrowth (season crops), Visual (palette) |

### Scene Objects
- TimeManager (singleton, DontDestroyOnLoad)
- Clock UI (HUD)

---

## 2. NGÀY/ĐÊM VISUAL

### Mô tả
DayNightLightController đọc TimeManager.NormalizedTime và điều chỉnh global light color/intensity. Sáng = bright, chiều = warm, tối = dark blue. Smooth transition.

### Điều kiện
- TimeManager active
- DayNightLightController active trong scene
- Global Light 2D present

### Flow
```
DayNightLightController.Update():
    1. float t = TimeManager.NormalizedTime (0.0 → 1.0)
    2. Color lightColor = dayNightGradient.Evaluate(t)
       → Gradient: 
         0.0 (06:00) = warm yellow (morning)
         0.25 (11:00) = white (midday)
         0.5 (16:00) = warm orange (afternoon)
         0.7 (20:00) = dark blue (evening)
         1.0 (02:00) = very dark blue (night)
    3. float intensity = intensityCurve.Evaluate(t)
       → Curve: 1.0 midday → 0.3 midnight
    4. globalLight.color = lightColor
    5. globalLight.intensity = intensity
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| TimeManager | NormalizedTime |
| DayNightLightController | Đọc time, set light |
| Global Light 2D | Unity 2D lighting |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| DayNightConfig | ScriptableObject | dayNightGradient (Gradient), intensityCurve (AnimationCurve) |

### UI
Không. Visual = lighting thay đổi smooth.

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| (Không cần event — poll NormalizedTime mỗi frame) | | |

### Scene Objects
- Global Light 2D (Unity URP)
- DayNightLightController (MonoBehaviour)

---

## 3. SLEEP / END DAY

### Mô tả
Player tương tác giường (Bed) → confirm popup → skip tới ngày hôm sau. Stamina restore (tùy giờ ngủ: trước 00:00 = full, sau = 75%). Hiện day summary (optional).

### Điều kiện
- Player đứng gần Bed entity
- Player nhấn E/chuột phải
- Confirm popup accepted

### Flow
```
E / Chuột Phải → SecondaryActionEvent → ActionRuntime
    → EntityScanSystem.GetClosest() → Bed entity
    → Forward SecondaryActionEvent → BedRuntime.Handle():
        → context.AddOption("sleep", "Đi ngủ")
        → Player chọn → Confirm popup: "Đi ngủ và kết thúc ngày?"
        → Player confirm:
            1. PlayerControler.InputEnabled = false
            2. Fade to black
            3. Calculate stamina restore:
                - if currentHour < 24 (trước 00:00): restore = MaxStamina (full)
                - else: restore = MaxStamina * 0.75
            4. EntityRuntime.stats.Set(Stamina, restore)
            5. HealthRuntime.SetHP(maxHP) → full heal
            6. TimeManager.AdvanceToNextDay()
                → DayChangedPublish → all systems process new day
            7. WeatherSystem.GenerateWeather() → set today's weather
            8. Fade in
            9. PlayerControler.InputEnabled = true
            10. Show day summary (optional): "Ngày 6 — Mưa — Thu nhập: 500G"
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| BedRuntime 🆕 | Handle sleep interaction |
| TimeManager | AdvanceToNextDay() |
| WeatherSystem | Generate new day weather |
| PlayerControler | Disable/enable input |
| EntityRuntime.stats | Restore stamina |
| HealthRuntime | Restore HP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Bed EntityData | ScriptableObject | BedModule(earlyRestorePercent=1.0, lateRestorePercent=0.75) |
| GameConfig | ScriptableObject | lateHourThreshold=24 |

### UI
- Confirm popup: "Đi ngủ?"
- Fade to black transition
- Day summary panel (optional): date, weather, earnings

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | BedRuntime |
| DayChangedPublish | TimeManager | All day-based systems |
| WeatherChangedPublish | WeatherSystem | HUD, FarmingSystem |

### Scene Objects
- Bed entity (in player house)
- Fade overlay UI
- Day summary UI (optional)

---

## 4. WEATHER RANDOM

### Mô tả
Mỗi ngày mới, WeatherSystem random thời tiết: Sunny (60%), Rainy (30%), Stormy (10%). Weather ảnh hưởng farming (mưa = auto-water) và visual (rain particles, darker lighting).

### Điều kiện
- Ngày mới bắt đầu (sau sleep hoặc forced sleep)
- WeatherSystem active

### Flow
```
TimeManager.AdvanceToNextDay() → WeatherSystem.GenerateWeather():
    1. float roll = Random.value
    2. if roll < 0.60: weather = Sunny
       elif roll < 0.90: weather = Rainy
       else: weather = Stormy
    3. currentWeather = weather
    4. EventBus.Publish(WeatherChangedPublish(weather))
    
    Subscribers:
        - HUD: update weather icon
        - RainParticleSystem: enable/disable rain visual
        - DayNightLightController: darken if rainy/stormy
        - WateredTileTracker: if Rainy → auto-water (xem chức năng 5)

WeatherSystem.GetTomorrow() → Weather:
    - Pre-roll tomorrow's weather (for AI Assistant forecast)
    - Stored but not published until day actually changes
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| WeatherSystem 🆕 | Random weather, store state |
| TimeManager | Trigger on new day |
| DayNightLightController | Adjust lighting for weather |
| WateredTileTracker | Auto-water if rainy |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| WeatherConfig | ScriptableObject | sunnyChance=0.6, rainyChance=0.3, stormyChance=0.1 |

### UI
- Weather icon (HUD, next to clock): ☀️/🌧️/⛈️
- Rain particle overlay (visual only)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| WeatherChangedPublish 🆕 | WeatherSystem | HUD, RainVisual, DayNightLight, WateredTileTracker |

### Scene Objects
- WeatherSystem (singleton)
- Rain particle system (enable/disable)
- Weather icon UI

---

## 5. MƯA AUTO-WATER

### Mô tả
Nếu thời tiết hôm nay là Rainy hoặc Stormy, tất cả ô đất outdoor đã cày (plowed) tự động được tưới. Player không cần tưới tay. WateredTileTracker.WaterAllOutdoorCells() được gọi.

### Điều kiện
- Weather = Rainy hoặc Stormy
- Có ô đất plowed outdoor

### Flow
```
WeatherSystem.GenerateWeather() → weather = Rainy
    → EventBus.Publish(WeatherChangedPublish(Rainy))
    → WateredTileTracker.Handle(WeatherChangedPublish):
        1. if weather == Rainy || weather == Stormy:
            → WaterAllOutdoorCells():
                - Get all plowed cells from TileRegistry
                - Filter: only outdoor (not in greenhouse/building)
                - foreach cell: SetWatered(cell)
                - Update tile visuals → wateredTile
        2. Debug.Log("Mưa đã tưới tất cả cây ngoài trời!")
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| WeatherSystem | Publish weather |
| WateredTileTracker | WaterAllOutdoorCells() |
| TileRegistry | Get plowed cells |
| WorldEntityService | Update tile visuals |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| (Không cần data riêng — dùng existing TileRegistry + WateredTileTracker) | | |

### UI
- Rain visual (particles)
- All outdoor plowed tiles show watered visual
- AI Assistant: "Hôm nay mưa, không cần tưới!"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| WeatherChangedPublish | WeatherSystem | WateredTileTracker |

### Scene Objects
- Tilemap (tiles auto-change visual)
- Rain particles

---

## 6. KIỆT SỨC (FORCED SLEEP)

### Mô tả
Nếu player vẫn thức lúc 02:00 (giờ game), bị forced sleep. Ngày hôm sau stamina chỉ restore 50% (penalty). Message: "Bạn đã kiệt sức..."

### Điều kiện
- currentHour >= 26 (02:00)
- Player chưa ngủ

### Flow
```
TimeManager → GameHourChangedPublish(26) → ExhaustionHandler.Handle():
    1. PlayerControler.InputEnabled = false
    2. Show message: "Bạn đã kiệt sức..."
    3. Fade to black
    4. Wait 1s
    5. Stamina restore = MaxStamina * 0.5 (penalty: chỉ 50%)
    6. HealthRuntime.SetHP(maxHP)
    7. TimeManager.AdvanceToNextDay()
    8. Fade in
    9. PlayerControler.InputEnabled = true
    10. Show warning: "Đừng thức quá khuya! Stamina chỉ hồi 50%."
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ExhaustionHandler 🆕 | Listen 02:00, force sleep |
| TimeManager | Hour tracking |
| PlayerControler | Disable input |
| EntityRuntime.stats | Stamina penalty |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| GameConfig | ScriptableObject | exhaustionHour=26, exhaustionStaminaPenalty=0.5 |

### UI
- "Kiệt sức..." message overlay
- Fade to black
- Warning message next morning

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| GameHourChangedPublish(26) | TimeManager | ExhaustionHandler |
| DayChangedPublish | TimeManager | All systems |

### Scene Objects
- ExhaustionHandler (MonoBehaviour hoặc service)
- Fade overlay UI

---

## TỔNG KẾT: TIME & WEATHER SYSTEM DEPENDENCY

```
TimeManager (Core)
    │
    ├── Update() → track gameMinutes/hours
    │       │
    │       ├── GameHourChangedPublish (mỗi giờ game)
    │       │       ├── AnimalRuntime (product timer)
    │       │       ├── AIAssistant (refresh tips)
    │       │       ├── NPC schedules
    │       │       └── ExhaustionHandler (02:00 check)
    │       │
    │       └── NormalizedTime → DayNightLightController (mỗi frame)
    │
    ├── AdvanceToNextDay() (từ Sleep hoặc Exhaustion)
    │       │
    │       ├── DayChangedPublish
    │       │       ├── StageRuntime (crop growth)
    │       │       ├── WateredTileTracker.ResetAll()
    │       │       ├── AnimalRuntime (hunger reset)
    │       │       ├── RespawnRegistry (resource respawn)
    │       │       ├── ResearchService (countdown)
    │       │       └── QuestService (daily quests)
    │       │
    │       └── WeatherSystem.GenerateWeather()
    │               │
    │               └── WeatherChangedPublish
    │                       ├── HUD (weather icon)
    │                       ├── RainVisual (particles)
    │                       ├── DayNightLight (darken)
    │                       └── WateredTileTracker (auto-water if rainy)
    │
    └── SeasonChangedPublish (mỗi 28 ngày)
            ├── Crop availability
            └── Visual palette

Sleep Flow:
    Bed interact → confirm → fade → restore stamina/HP → AdvanceToNextDay → fade in

Exhaustion Flow:
    02:00 → forced → fade → 50% stamina penalty → AdvanceToNextDay → fade in
```
