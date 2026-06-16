# Content Gap Plan — Lấp lỗ hổng vật phẩm & vòng lặp harvest/drop

Mục tiêu: vá các lỗ hổng đang làm đứt vòng lặp gameplay cơ bản
(trồng/thu hoạch → có item; chặt/hái → có item; giết quái → có item; map có
vật cản/trang trí cho cảm giác sống động). KHÔNG làm hệ thống mới (không
animal, không weapon/armor, không storage UI, không fishing) — lý do: thiếu
asset sprite phù hợp và chưa cấp thiết. Phạm vi lần này CHỈ xoay quanh
**vật phẩm (item) và world object còn thiếu**, để khép kín các vòng lặp đã
có sẵn entity nguồn nhưng bị đứt ở khâu output.

Tham chiếu khảo sát 2026-06-16 (2 vòng):
- Vòng 1: kiểm kê tổng quan toàn bộ nội dung game.
- Vòng 2: khảo sát sâu world object filler + harvest/drop chain — phát hiện
  chain bị đứt nghiêm trọng hơn dự kiến ban đầu, không chỉ ở enemy.

## Hiện trạng thực tế (đã xác minh bằng cách đọc asset)

### Harvest/Drop chain — ĐỨT Ở ĐÂU
| Nguồn | Số lượng | DropModule | Item output | Trạng thái |
|---|---|---|---|---|
| Crop canonical (`world_crop_*`) | 6 (corn, potato, strawberry, tomato, turnip, wheat) | có, trỏ đúng item | tồn tại | ✅ OK |
| Crop legacy (`crop_*`) | 16 (asparagus, bean, blueberry, cabbage, carrot, cauliflower, cucumber, garlic, grape, melon, pea, pepper×3, radish, yam) | `harvestDrops: []` rỗng | **không tồn tại** | ❌ Đứt hoàn toàn |
| WoodTree (oak, pine, maple, fir, brech) | 5 | **chưa từng gắn DropModule** | **không tồn tại** (không có item Wood/Log nào) | ❌ Đứt hoàn toàn |
| FruitTree (apple, cherry, pear, plum) | 4 | có gắn nhưng apple trỏ `fileID:0` (null), cherry/pear/plum trỏ 1 guid không tồn tại (broken reference, cùng trỏ chung 1 guid sai) | **không tồn tại** | ❌ Đứt hoàn toàn |
| Mine resource node | 54 | có, trỏ đúng item | tồn tại (Stone/Ore/Bar/Gem) | ✅ OK |
| Enemy (Slime1-3, Orc1-3) | 6 | `harvestDrops`/`deathDrops` rỗng | N/A | ❌ Đứt hoàn toàn |

→ Seed của 16 crop legacy + 4 fruit tree + 5 wood tree đã bán/trồng được
(người chơi tốn tiền/thời gian mua giống, trồng, chăm), nhưng **thu hoạch
xong tay trắng**. Đây là lỗ hổng nghiêm trọng nhất vì nó trực tiếp lừa dối
kỳ vọng người chơi đã đầu tư công sức.

### ItemCategory — 11/15 category chưa có asset nào
`None, Food, Weapon, Armor, Accessory, Placeable, AnimalProduct, Consumable,
Quest, Currency, Misc` = 0 asset. Chỉ `Tool(5)`, `Seed(31)`, `Crop(6)`,
`Material(11)` có nội dung thật.

### World filler/decoration — hoàn toàn chưa tồn tại ở tầng entity
Không có EntityData/ObjectType nào cho cỏ hoang, hàng rào, đá trang trí
(không thể đào), bụi cây cảnh quan, hoa dại tĩnh, log gỗ vụn, biển báo, vật
cản. Toàn bộ "nền cảnh" hiện tại chỉ là texture vẽ trên Tilemap layer
`Tm_Decoration`/`Tm_GroundDetail` — không phải object tương tác được.
`ObjectType` enum cũng chưa có placeholder cho nhóm này, cần mở enum mới.

---

## Quy ước thực hiện

- EntityData mới đặt đúng cấu trúc hiện có: `Entities/Items/Crops/`,
  `Entities/Items/Materials/`, `Entities/World/...`.
- Mỗi item mới cần `id`, `keyName`, `descKey` + bản dịch VI/EN (theo pattern
  các asset do `GEDCanonicalGenerator` đã tạo).
- Không tạo module runtime mới — `DropModule`, `HarvestModule` đã đủ dùng,
  chỉ cần điền data và gắn module còn thiếu (trường hợp WoodTree).
- Với object filler (cỏ/hàng rào/đá cảnh): nếu không có sprite phù hợp ngay
  (đã xác nhận chưa có), giai đoạn này CHỈ tạo EntityData + ObjectType enum
  value + ghi rõ "đợi sprite" — không tự chế placeholder graphic. Việc gắn
  sprite/prefab thật để art-director hoặc bạn làm sau, tách riêng khỏi việc
  data.

---

## P0 — Vá đứt chain harvest/drop (entity nguồn đã có sẵn, chỉ thiếu output)

### 1. Item thu hoạch cho 16 crop legacy
- Tạo 16 item Crop còn thiếu: asparagus, bean, blueberry, cabbage, carrot,
  cauliflower, cucumber, garlic, grape, melon, pea, pepper (green/red/yellow
  — có thể dùng chung 1 item màu khác nhau hoặc 3 item riêng theo đúng 3
  crop hiện có), radish, yam.
- Nối `DropModule.harvestDrops` của 16 file `crop_*.asset` (legacy) trỏ tới
  item tương ứng — hiện đang rỗng `[]`.
- Theo đúng pattern của 6 crop canonical đã chạy đúng (`world_crop_tomato`
  → `item_crop_tomato`), đặt tên `item_crop_<ten>`.
- File đụng tới: `Entities/Items/Crops/` (16 file mới),
  `Entities/World/Crops/crop_*.asset` (16 file sửa DropModule).
- Acceptance: trồng + thu hoạch mỗi loại trong Play Mode, item rơi đúng vào
  inventory.

### 2. Item gỗ (Wood/Log) cho 5 WoodTree
- Vấn đề kép: WoodTree chưa từng gắn `DropModule`, và chưa có item gỗ nào.
- Tạo tối thiểu 1-2 item Material: `item_mat_wood` (gỗ thường, dùng chung
  cho oak/pine/maple/fir/brech ở mức cơ bản — không cần phân loại
  hardwood/softwood ở bước này, tránh đi sâu).
- Gắn `DropModule` vào cả 5 file WoodTree (`WoodTree_oak`, `_pine`, `_maple`,
  `_fir`, `_brech`), trỏ `deathDrops`/`harvestDrops` tới `item_mat_wood` với
  số lượng tăng nhẹ theo độ "to" của cây nếu có field tier, nếu không thì
  số lượng đồng nhất là đủ.
- File đụng tới: `Entities/Items/Materials/` (1-2 file mới),
  `Entities/World/WoodTrees/*.asset` (5 file — thêm module).
- Acceptance: chặt cây trong Play Mode, nhận gỗ vào inventory.

### 3. Item quả cho 4 FruitTree (sửa broken reference)
- Vấn đề: Apple trỏ `fileID:0` (chưa từng gán), Cherry/Pear/Plum cùng trỏ 1
  guid không tồn tại (tham chiếu hỏng).
- Tạo 4 item Crop hoặc Food (quyết định: dùng category `Crop` cho nhất quán
  với crop khác, vì quả cây cũng là nông sản bán được):
  `item_fruit_apple`, `item_fruit_cherry`, `item_fruit_pear`,
  `item_fruit_plum`.
- Sửa `DropModule.harvestDrops` của cả 4 file FruitTree trỏ đúng guid item
  mới tạo (xóa reference hỏng).
- File đụng tới: `Entities/Items/Crops/` hoặc `Entities/Items/Fruits/`
  (4 file mới), `Entities/World/FruitTrees/*.asset` (4 file sửa reference).
- Acceptance: hái quả trong Play Mode, item đúng loại rơi vào inventory,
  không còn missing reference warning trong Console.

### 4. Enemy drop table (giữ nguyên từ bản plan trước — vẫn P0)
- 6 enemy (Slime1-3, Orc1-3) có `DropModule` nhưng `harvestDrops`/
  `deathDrops` rỗng → giết quái không rơi gì.
- Điền drop dùng material đã có sẵn + 2 material mới ở mục 2 nếu hợp lý
  (ví dụ Slime rơi vật phẩm chế tạo cơ bản, Orc rơi ore/bar tier tương ứng):
  - Slime1-3: tăng dần lượng/tỉ lệ rơi `item_mat_stone` hoặc coin, tỉ lệ
    thấp rơi `item_resource_coal`.
  - Orc1-3: rơi `item_mat_iron_ore`/`item_resource_iron_bar`, Orc3 có tỉ lệ
    thấp rơi gem (ruby/sapphire/emerald).
- File đụng tới: 6 EntityData enemy tại `Entities/Characters/Enemies/`.
- Acceptance: giết từng loại enemy trong Play Mode, thấy item rơi.

---

## P1 — Item chế tạo/nâng cấp còn treo (đã có recipe, thiếu output)

### 5. Food item cho 2 recipe đang treo
- `recipe_cheese_basic`, `recipe_mayo_basic` đã tồn tại nhưng category
  `Food` = 0 asset toàn project → output không tồn tại.
- Tạo `item_food_cheese`, `item_food_mayo` (category Food, dùng
  `ConsumableModule` có sẵn trong code, hồi HP/Stamina mức nhỏ).
- File đụng tới: `Entities/Items/Food/` (2 file mới, folder hiện rỗng), nối
  output vào 2 recipe đã có.
- Acceptance: craft thử trong Play Mode, nhận item ăn được, ăn thử thấy hồi
  HP/Stamina.

### 6. Tool Tier 2 — item thật cho 3 recipe nâng cấp
- `Recipe_Pickaxe_T2`, `Recipe_Scythe_T2`, `Recipe_Sprinkler_T2` đã tồn tại,
  không có item T2 để nhận.
- Tạo `item_tool_pickaxe_t2`, `item_tool_scythe_t2`, item sprinkler T2 cầm
  tay tương ứng (lưu ý: `world_utility_sprinkler_t2` đã có ở World/Utility —
  kiểm tra xem có phải chỉ thiếu item placeable, không phải thiếu world
  entity). Stat nhích nhẹ so với T1.
- Chỉ làm T2, KHÔNG làm T3-T5 ở giai đoạn này.
- File đụng tới: `Entities/Items/Tools/` (3 file mới), 3 recipe có sẵn (nối
  output).
- Acceptance: craft 1 trong 3 recipe T2, nhận item dùng được.

---

## P2 — World filler/decoration (chỉ phần DATA, chờ sprite riêng)

### 7. Object filler cơ bản: cỏ hoang, hàng rào, đá trang trí
- Vì hiện chưa có sprite phù hợp, giai đoạn này CHỈ:
  - Mở rộng `ObjectType` enum (`Assets/Project/Scripts/Data/Enums/ObjectType.cs`)
    thêm tối thiểu 3 giá trị: `Decoration_Grass`, `Decoration_Fence`,
    `Decoration_Rock` (đặt tên theo convention enum hiện có).
  - Tạo EntityData khung tối giản cho mỗi loại (không cần gameplay module
    phức tạp — cỏ/đá trang trí có thể chỉ cần là vật cản tĩnh hoặc vật trang
    trí không tương tác; hàng rào có thể cần `Collision` nếu dùng làm vật
    cản ranh giới farm).
  - KHÔNG gán sprite/prefab thật — để trống hoặc gán placeholder rõ ràng,
    ghi chú trong file để art-director xử lý sau khi có asset.
- Acceptance: enum + EntityData tồn tại, compile sạch, không cần chạy được
  trong Play Mode ở bước này (vì chưa có visual) — bàn giao rõ cho bước art
  riêng.
- Lưu ý: đây là mục duy nhất có thể TẠM HOÃN nếu Codex ưu tiên thời gian cho
  mục 1-6 trước, vì nó không tạo ra vòng lặp chơi được ngay mà chỉ chuẩn bị
  khung cho việc thêm sprite sau.

---

## Việc KHÔNG làm trong plan này

- Animal/chăn nuôi (chưa có sprite phù hợp — theo yêu cầu).
- Weapon/Armor/Accessory riêng biệt.
- Storage/Building UI.
- Fishing.
- Tool T3-T5, Gear Set đầy đủ.
- Quest milestone mới (M1-M4) — tách riêng nếu cần, không phải trọng tâm lần này.
- Audio.
- Balance pass chuyên sâu — chỉ cần số liệu hợp lý, không cần tối ưu.

## Thứ tự đề xuất cho Codex

1. Mục 1 (16 item crop legacy) — khối lượng lớn nhất nhưng lặp pattern đơn giản, làm trước để rảnh tay.
2. Mục 2 (Wood item + gắn DropModule) — vừa thiếu item vừa thiếu module, cần chú ý kỹ.
3. Mục 3 (Fruit item + sửa broken reference) — ưu tiên cao vì đang có lỗi tham chiếu hỏng (Console warning).
4. Mục 4 (Enemy drop table) — đã rõ từ trước, nhanh.
5. Mục 5 (Food item) — mở khóa 2 recipe treo.
6. Mục 6 (Tool T2) — mở khóa 3 recipe treo.
7. Mục 7 (World filler data khung) — làm cuối, có thể hoãn sang lượt khác khi có sprite.
