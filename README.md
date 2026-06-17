# Farm & Code — Game Nông Trại Sinh Tồn 2D

**Đề tài tốt nghiệp — Đại học Công nghệ Giao thông Vận tải**

| | |
|---|---|
| **Sinh viên** | Nông Đức Toàn — MSSV 73DCTT23253 |
| **Lớp** | 73DCTT21 |
| **GVHD** | ThS. Nguyễn Văn Cường |
| **Năm học** | 2021–2026 |
| **Engine** | Unity 2022.3 LTS |
| **Ngôn ngữ** | C# |
| **Nền tảng** | PC (Windows) |

---

## Giới thiệu

**Farm & Code** là game 2D Top-Down thuộc thể loại nông trại kết hợp sinh tồn nhẹ, được xây dựng bằng Unity 2022.3.

> *Năm 2030, AI thay thế hàng triệu lập trình viên. Bạn — một dev IT vừa ra trường — thất nghiệp, bỏ phố về quê. Bạn dùng kiến thức công nghệ để biến mảnh đất hoang thành trang trại thông minh nhất vùng. Nhưng thế giới bên ngoài đang thay đổi...*

Người chơi trải qua vòng lặp trồng trọt → thu hoạch → bán nông sản → mở rộng trang trại, kết hợp với hệ thống khai hoang và một cốt truyện sci-fi được tiết lộ dần theo thời gian.

---

## Chạy game

### Yêu cầu hệ thống

- Windows 10/11 (64-bit)
- RAM tối thiểu: 4 GB
- GPU hỗ trợ DirectX 11 trở lên
- Dung lượng trống: khoảng 500 MB

### Chạy bản build sẵn

1. Vào thư mục `Build/`
2. Chạy file `FarmAndCode.exe`
3. Không cần cài đặt thêm gì

### Mở project trong Unity Editor

1. Cài Unity 2022.3 LTS (khuyến nghị qua Unity Hub)
2. Clone repo về máy
3. Mở Unity Hub → **Add project from disk** → chọn thư mục repo
4. Unity tự động import package, chờ compile hoàn tất
5. Mở scene chính: `Assets/Project/Scenes/Coreplay/FarmScene.unity`
6. Nhấn **Play** để chạy

---

## Điều khiển

| Phím | Hành động |
|---|---|
| WASD / Mũi tên | Di chuyển nhân vật |
| Chuột trái | Sử dụng công cụ đang cầm |
| Chuột phải / E | Tương tác (thu hoạch tay, mở hộp thoại NPC) |
| Tab | Mở / đóng Inventory |
| 1–0 | Chuyển slot hotbar |
| Esc | Tạm dừng / Menu |
| F5 | Lưu game |
| F9 | Tải game |

---

## Cấu trúc thư mục dự án

```
DATN_FarmGame/
├── Assets/
│   └── Project/
│       ├── Docs/               ← Toàn bộ tài liệu thiết kế
│       ├── Scripts/            ← Source code C#
│       ├── Prefabs/            ← Prefab game object
│       ├── Scenes/             ← Unity scenes
│       ├── Resources/          ← Asset được load lúc runtime
│       ├── Data/               ← ScriptableObject data
│       ├── Art/                ← Sprite, tileset, texture
│       └── Audio/              ← Âm thanh
├── Build/                      ← Bản build chạy được (nếu có)
├── Packages/                   ← Unity package dependencies
└── ProjectSettings/            ← Cài đặt Unity project
```

---

## Tài liệu thiết kế

Toàn bộ tài liệu nằm trong `Assets/Project/Docs/`.

### Tài liệu chính

| File | Mô tả |
|---|---|
| [`Docs/GDD.md`](Assets/Project/Docs/GDD.md) | **Game Design Document** — Bản thiết kế game hoàn chỉnh. Bao gồm: ý tưởng, cốt truyện, vòng lặp gameplay, tất cả hệ thống (farming, combat, kinh tế, tiến triển, thời tiết...), phạm vi nội dung MVP, kế hoạch triển khai. |
| [`Docs/DeCuongDoAnTotNghiep.md`](Assets/Project/Docs/DeCuongDoAnTotNghiep.md) | Đề cương chi tiết đồ án tốt nghiệp — nộp cho khoa. |
| [`Docs/DataEntity.md`](Assets/Project/Docs/DataEntity.md) | **Kiến trúc dữ liệu EntityData** — Quy tắc thiết kế toàn bộ đối tượng trong game theo mô hình `EntityData → ModuleData → ModuleRuntime`. Bao gồm: danh mục Stats chính thức, blueprint theo archetype (Player, Tool, Crop, NPC, Enemy...), quy tắc tạo data mới. |
| [`Docs/ItemDesign.md`](Assets/Project/Docs/ItemDesign.md) | **Thiết kế item** — Danh mục toàn bộ item trong game: ID, category, modules cần gắn, source (từ đâu có được), sink (dùng để làm gì). |

### Tài liệu thiết kế chi tiết (`Docs/Design/`)

| File | Mô tả |
|---|---|
| [`Design/01_CoreIdea.md`](Assets/Project/Docs/Design/01_CoreIdea.md) | Ý tưởng lõi: bối cảnh thế giới, narrative structure, tone, điểm khác biệt |
| [`Design/02_GameplayLoop.md`](Assets/Project/Docs/Design/02_GameplayLoop.md) | Vòng lặp gameplay: micro/meso/macro loop, feature chain, nhịp độ tiến triển |
| [`Design/03_PlayerGoals.md`](Assets/Project/Docs/Design/03_PlayerGoals.md) | Mục tiêu người chơi: ngắn hạn / trung hạn / dài hạn, motivation framework |
| [`Design/04_CoreSystems.md`](Assets/Project/Docs/Design/04_CoreSystems.md) | Bản đồ kiến trúc hệ thống — liệt kê từng feature cần script/service gì, đã có hay cần tạo mới |
| [`Design/04_ScriptTree.md`](Assets/Project/Docs/Design/04_ScriptTree.md) | Cây thư mục toàn bộ script C# trong dự án với trạng thái (đã có / cần sửa / cần tạo mới) |
| [`Design/05_PrototypePlan.md`](Assets/Project/Docs/Design/05_PrototypePlan.md) | Kế hoạch prototype theo sprint |
| [`Design/06_VerticalSlice.md`](Assets/Project/Docs/Design/06_VerticalSlice.md) | Kế hoạch vertical slice — danh sách nội dung đủ để chơi từ đầu đến cuối |
| [`Design/GED.md`](Assets/Project/Docs/Design/GED.md) | **Game Entity Design** — Bản thiết kế đầy đủ từng entity: baseStats, modules gắn, tham số gameplay cụ thể |

### Thiết kế hệ thống chi tiết (`Docs/Design/Features/`)

| File | Mô tả |
|---|---|
| [`Features/FarmingSystem.md`](Assets/Project/Docs/Design/Features/FarmingSystem.md) | Hệ thống farming: cuốc đất, gieo hạt, tưới nước, sinh trưởng cây, héo/chết, thu hoạch — flow và dependency đầy đủ |
| [`Features/CombatSystem.md`](Assets/Project/Docs/Design/Features/CombatSystem.md) | Hệ thống chiến đấu và khai hoang: mutant AI, vũ khí, khu vực, hậu quả chết |
| [`Features/CraftingSystem.md`](Assets/Project/Docs/Design/Features/CraftingSystem.md) | Hệ thống chế tạo: recipe, nguyên liệu, output |
| [`Features/MiningSystem.md`](Assets/Project/Docs/Design/Features/MiningSystem.md) | Hệ thống khai thác khoáng sản và tài nguyên |
| [`Features/ProgressionSystem.md`](Assets/Project/Docs/Design/Features/ProgressionSystem.md) | Hệ thống tiến triển: Farming Mastery, unlock, EXP |
| [`Features/TimeWeatherSystem.md`](Assets/Project/Docs/Design/Features/TimeWeatherSystem.md) | Hệ thống thời gian và thời tiết |
| [`Features/AnimalSystem.md`](Assets/Project/Docs/Design/Features/AnimalSystem.md) | Hệ thống chăn nuôi |
| [`Features/NarrativeSystem.md`](Assets/Project/Docs/Design/Features/NarrativeSystem.md) | Hệ thống cốt truyện: story events, delivery type, trigger |
| [`Features/AIAssistantSystem.md`](Assets/Project/Docs/Design/Features/AIAssistantSystem.md) | Hệ thống AI Assistant — hint system thông minh |

### Thiết kế kiến trúc kỹ thuật (`Docs/Design/Systems/`)

| File | Mô tả |
|---|---|
| [`Systems/01_Services.md`](Assets/Project/Docs/Design/Systems/01_Services.md) | Đặc tả API đầy đủ của từng Service (EntityService, InventoryService, ShopService...) |
| [`Systems/02_Runtimes.md`](Assets/Project/Docs/Design/Systems/02_Runtimes.md) | Đặc tả các Runtime class chính |
| [`Systems/03_UIDataFlow.md`](Assets/Project/Docs/Design/Systems/03_UIDataFlow.md) | Luồng dữ liệu UI — cách UI đọc state và phản ứng với event |
| [`Systems/04_SceneObjects.md`](Assets/Project/Docs/Design/Systems/04_SceneObjects.md) | Danh sách GameObject và component trong từng scene |
| [`Systems/05_EventCatalog.md`](Assets/Project/Docs/Design/Systems/05_EventCatalog.md) | Catalog toàn bộ event trong EventBus |
| [`Systems/06_DataSchema.md`](Assets/Project/Docs/Design/Systems/06_DataSchema.md) | Schema dữ liệu save/load |

---

## Kiến trúc hệ thống (tóm tắt)

Game được xây dựng theo mô hình **Entity–Module–Runtime** kết hợp **EventBus** để các hệ thống giao tiếp mà không phụ thuộc trực tiếp lẫn nhau:

```
EntityData (ScriptableObject)     ← Cấu hình tĩnh, không thay đổi lúc chơi
    ├── baseStats[]               ← Chỉ số số học (HP, Stamina, Attack...)
    └── modules[]                 ← Danh sách capability (Tool, Stage, Harvest...)

EntityRuntime (MonoBehaviour)     ← Trạng thái sống trong scene
    ├── ref EntityData
    ├── stats (runtime overrides)
    └── moduleRuntimes[]          ← Logic và state của từng capability

GameManager                       ← Đăng ký và cấp phát toàn bộ Service
    ├── EntityService             ← CRUD entity
    ├── InventoryService          ← Quản lý inventory
    ├── ShopService               ← Mua bán
    ├── CraftingService           ← Chế tạo
    ├── QuestService              ← Nhiệm vụ
    ├── ProgressionService        ← Farming Mastery
    ├── TimeManager               ← Đồng hồ game
    ├── WeatherSystem             ← Thời tiết
    ├── WateredTileTracker        ← Track ô đã tưới
    ├── NarrativeService          ← Story events
    └── SaveLoadManager           ← Lưu/tải game

EventBus                          ← Hệ thống sự kiện trung tâm
    ← DayChangedPublish, WeatherChangedPublish, DieEvent, SpawnRequestPublish...
```

---

## Công nghệ và thư viện sử dụng

| Công nghệ | Mục đích |
|---|---|
| Unity 2022.3 LTS | Game engine |
| C# | Ngôn ngữ lập trình |
| Unity Tilemap | Hệ thống bản đồ dạng lưới |
| Unity Input System | Xử lý đầu vào |
| Unity TextMeshPro | Hiển thị text trong UI |
| HeroEditor4D | Bộ asset nhân vật pixel art |
| Newtonsoft JSON | Serialize/deserialize save data |

---

## Tính năng đã hoàn thiện

- Di chuyển nhân vật 4 hướng với animation
- Hệ thống cuốc đất, gieo hạt, thu hoạch
- Hệ thống stamina (chi phí, hồi phục, hết stamina)
- Inventory với hotbar, backpack, drag-drop
- Hệ thống shop: mua hạt giống, bán nông sản
- Hệ thống crafting: recipe và chế tạo item
- Hệ thống quest và đơn hàng NPC
- Hệ thống tiến triển (Level/Mastery + EXP)
- Hệ thống thời gian (giờ, ngày, mùa)
- Hệ thống enemy và combat (mutant AI)
- Hệ thống khai thác tài nguyên (cây, đá, quặng)
- Hệ thống chăn nuôi (gà → trứng)
- Dialogue NPC với dialogue graph
- Save/load game state
- Chuyển cảnh (FarmScene ↔ MineScene)

---

*Đề tài tốt nghiệp — Khóa 2021–2026 — Đại học Công nghệ Giao thông Vận tải*
