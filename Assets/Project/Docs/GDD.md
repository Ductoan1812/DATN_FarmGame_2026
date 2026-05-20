# 🌾 GAME DESIGN DOCUMENT — FARM & CODE

> **Thể loại:** Farming Survival (2D Top-Down)
> **Platform:** PC
> **Engine:** Unity 2022.3 | **Visual:** Pixel Art (HeroEditor4D)
> **Target:** Solo indie, vertical slice playable
> **Thời lượng:** Endless loop (không có "hết game")

---

## 1. CORE IDENTITY

### 1.1 Elevator Pitch

> *"Năm 2030, AI thay thế hàng triệu lập trình viên. Bạn — một dev vừa ra trường — thất nghiệp, hết tiền, bỏ phố về quê. Nhưng bạn không bỏ cuộc. Bạn dùng chính kiến thức công nghệ và AI để biến mảnh đất hoang thành trang trại thông minh nhất vùng."*

### 1.2 Core Fantasy

Người chơi cảm thấy: **"Tôi đang xây dựng lại cuộc đời từ con số 0, và mỗi ngày tôi mạnh hơn hôm qua."**

Không phải fantasy kiếm hiệp hay phép thuật — mà là fantasy của sự tự lực, sáng tạo, và chứng minh giá trị bản thân bằng đôi tay + trí tuệ.

### 1.3 Điểm khác biệt

| So với | Game này khác ở |
|--------|----------------|
| Stardew Valley | Cốt truyện hiện đại, relatable với gen Z Việt Nam. AI là mechanic thật. |
| Harvest Moon | Không romance-focused. Survival pressure thật (tiền, stamina, mùa vụ). |
| Rune Factory | Combat phục vụ farming (khai hoang), không phải dungeon crawl. |

### 1.4 Target Audience

- Sinh viên IT / người trẻ Việt Nam lo lắng về tương lai nghề nghiệp
- Fan farming sim muốn cốt truyện có chiều sâu cảm xúc
- Người chơi thích progression rõ ràng, relaxing nhưng có stakes

---

## 2. CỐT TRUYỆN

### 2.1 Bối cảnh

Năm 2030. AI đã thay thế phần lớn công việc lập trình, thiết kế, và vận hành hệ thống. Các công ty tech sa thải hàng loạt. Sinh viên IT ra trường không tìm được việc.

### 2.2 Nhân vật chính

Bạn vừa tốt nghiệp ngành CNTT. 6 tháng gửi CV, không ai gọi. Tiền thuê trọ hết. Bạn buộc phải rời thành phố, về quê — nơi gia đình còn vài mảnh đất bỏ hoang.

### 2.3 Hook cốt truyện

Thay vì chấp nhận thất bại, bạn quyết định: **"Nếu AI lấy mất việc của mình, thì mình sẽ dùng AI để làm việc khác."**

Bạn bắt đầu áp dụng kiến thức công nghệ vào nông nghiệp:
- Phân tích đất đai bằng dữ liệu
- Tối ưu lịch tưới/trồng
- Tự động hóa dần dần
- Nghiên cứu giống cây mới

### 2.4 Narrative delivery (cách kể chuyện)

**KHÔNG** dùng cutscene dài hay wall-of-text mở đầu.

Cốt truyện được kể qua:
- **Nhật ký ngắn** — mỗi milestone (mở đất mới, thu hoạch đầu tiên, kiếm đủ tiền trả nợ...) → 2-3 dòng suy nghĩ của nhân vật
- **Tin nhắn điện thoại** — bạn bè cũ hỏi thăm, tin tức về ngành IT, lời khuyên từ người thân
- **Thay đổi visual** — trang trại từ hoang tàn → xanh tốt = cốt truyện visual
- **NPC dialogue** — hàng xóm, thương lái, chuyên gia nông nghiệp

→ Người chơi KHÔNG BAO GIỜ bị ép đọc. Cốt truyện đến tự nhiên qua gameplay.

---

## 3. CORE LOOP

### 3.1 Một ngày trong game

```
🌅 SÁNG (6:00 - 12:00)
├── Kiểm tra trang trại: cây nào cần tưới, cây nào chín
├── Tưới nước / Thu hoạch / Gieo hạt mới
├── Cho động vật ăn (nếu có)
└── Kiểm tra AI Assistant: gợi ý hôm nay nên làm gì

☀️ CHIỀU (12:00 - 18:00)
├── Bán nông sản / Mua vật tư
├── Khai hoang vùng mới (combat ở đây)
├── Thu thập tài nguyên (gỗ, đá, thảo mộc)
└── Nâng cấp công cụ / Xây dựng

🌙 TỐI (18:00 - 02:00)
├── Hoàn thành việc còn dở
├── Nghiên cứu (unlock recipe/cây mới qua AI system)
└── Về nhà ngủ → kết thúc ngày

💤 NGỦ → Ngày mới
├── Stamina hồi đầy
├── Cây phát triển (nếu đã tưới)
├── Watered tiles reset
└── Random events (thời tiết, tin nhắn, NPC visit)
```

### 3.2 Engagement Loop (tại sao chơi tiếp)

```
Thu hoạch → Bán → Có tiền
    → Mua hạt tốt hơn / Nâng cấp tool / Mở đất mới
        → Trồng nhiều hơn → Thu hoạch nhiều hơn
            → Unlock AI features mới → Tự động hóa
                → Có thời gian khám phá thêm → ...
```

### 3.3 Session length

Một session chơi thoải mái: **15-30 phút** (1-2 ngày game).

---

## 4. HỆ THỐNG GAMEPLAY

### 4.1 Farming (Trụ cột chính)

| Feature | Mô tả | Ưu tiên |
|---------|--------|---------|
| Cuốc đất | Biến đất hoang → đất trồng | ✅ Có |
| Gieo hạt | Mua/tìm hạt → trồng | ✅ Có |
| Tưới nước | Bắt buộc mỗi ngày, không tưới = cây chậm/héo | 🔴 Cần làm |
| Thu hoạch | Cây chín → thu → bán/dùng | ✅ Có |
| Mùa vụ | 4 mùa, mỗi mùa có cây riêng | 🔴 Cần làm |
| Cây héo | Không tưới 2+ ngày → héo, mất vụ | 🟡 Cần làm |
| Chất lượng | Tưới đều + phân bón → sao cao → giá cao | 🟢 Sau |

**Cây trồng theo mùa (mỗi mùa 4-5 loại):**

| Mùa | Cây | Ngày lớn | Giá bán |
|-----|-----|----------|---------|
| Xuân | Cải xanh, Khoai tây, Dâu tây, Hoa hướng dương | 4-8 ngày | 50-200g |
| Hạ | Cà chua, Dưa hấu, Ngô, Ớt | 5-10 ngày | 80-300g |
| Thu | Bí đỏ, Cà rốt, Nho, Khoai lang | 5-9 ngày | 70-250g |
| Đông | Cải bó xôi, Củ cải trắng, Bắp cải | 6-8 ngày | 60-180g |

### 4.2 AI Assistant System (Mechanic độc đáo)

Đây là mechanic gắn với cốt truyện "dev dùng AI làm nông":

| Feature | Mô tả | Unlock |
|---------|--------|--------|
| Phân tích đất | Hiển thị chất lượng đất, gợi ý cây phù hợp | Mặc định |
| Lịch tưới | Nhắc nhở cây nào cần tưới, cây nào sắp chín | Farming Mastery 2 |
| Dự báo thời tiết | Biết trước ngày mai mưa/nắng → plan tốt hơn | Farming Mastery 3 |
| Tối ưu giá bán | Gợi ý thời điểm bán tốt nhất (giá dao động theo mùa) | Farming Mastery 5 |
| Auto-water (Sprinkler AI) | Tự động tưới vùng nhỏ | Craft + Mastery 7 |
| Phân tích giống | Unlock cây mới qua "nghiên cứu" | Mastery 10+ |

**Cách hoạt động trong game:**
- UI nhỏ góc màn hình: "AI gợi ý: Hôm nay nên thu hoạch cà chua, giá đang cao"
- Không bắt buộc nghe theo — chỉ là hint
- Upgrade AI = unlock thêm thông tin hữu ích
- Về mặt kỹ thuật: đây là tooltip/hint system, không phải AI thật

### 4.3 Combat = Khai Hoang

**Triết lý:** Bạn không phải chiến binh. Bạn là nông dân cần mở rộng đất.

| Yếu tố | Thiết kế |
|---------|----------|
| Khi nào combat? | Khi vào vùng hoang dã để khai hoang/lấy tài nguyên |
| Đối thủ | Thú hoang (chuột, rắn, lợn rừng, chim), côn trùng lớn — KHÔNG phải quái fantasy |
| Vũ khí | Công cụ nông nghiệp: cuốc, rìu, liềm — KHÔNG có kiếm/cung |
| Mục đích combat | Dọn đất, lấy nguyên liệu (gỗ, đá, thảo mộc, phân bón tự nhiên) |
| Reward | Nguyên liệu farming, KHÔNG phải EXP |
| Difficulty | Thấp-trung bình, không cần skill cao |

**Vùng khai hoang:**

| Vùng | Thú hoang | Tài nguyên | Unlock khi |
|------|-----------|------------|-----------|
| Bìa rừng | Chuột, thỏ hoang | Gỗ, thảo mộc, nấm | Mặc định |
| Rừng sâu | Rắn, lợn rừng | Gỗ cứng, đá, quặng sắt | Có rìu T2 |
| Đồi đá | Chim ưng, dê hoang | Đá quý, quặng, khoáng chất | Có cuốc T2 |
| Đầm lầy | Côn trùng lớn, rắn độc | Bùn phì nhiêu, thảo dược hiếm | Có ủng + liềm T2 |

**Khi khai hoang xong 1 vùng:**
- Thú hoang biến mất vĩnh viễn ở vùng đó
- Đất trở thành đất trồng được
- Trang trại mở rộng visually

→ Combat tự nhiên giảm dần khi trang trại lớn lên. Đây là progression visible.

### 4.4 Stamina & Resource Tension

| Stat | Mô tả |
|------|--------|
| Stamina | Mỗi hành động tốn stamina. Hết = không làm gì được, phải ngủ. |
| Stamina tối đa | Tăng dần theo Farming Mastery |
| Hồi stamina | Ngủ = hồi đầy. Ăn food = hồi 1 phần. |

**Chi phí stamina:**

| Hành động | Stamina cost |
|-----------|-------------|
| Cuốc đất | 4 |
| Tưới nước | 2 |
| Gieo hạt | 1 |
| Thu hoạch | 2 |
| Chặt cây | 6 |
| Đập đá | 6 |
| Chiến đấu (mỗi hit) | 3 |

**Stamina mặc định:** 100 → mỗi ngày được ~30-40 hành động → phải chọn lựa.

### 4.5 Progression System (thay thế Level)

**KHÔNG CÓ** hệ thống Level chung (L1-L50).

Thay vào đó: **Farming Mastery** + **Tiền** + **Diện tích**

#### Farming Mastery

Mỗi lần thu hoạch thành công → +Mastery EXP.

| Mastery Level | Unlock |
|---------------|--------|
| 1 | Bắt đầu (cuốc gỗ, bình tưới nhỏ) |
| 2 | AI: Lịch tưới. Unlock phân bón cơ bản. |
| 3 | AI: Dự báo thời tiết. Unlock cây mùa 2. |
| 4 | Craft: Bình tưới đồng (tưới 3 ô). |
| 5 | AI: Tối ưu giá. Unlock chuồng gà. |
| 6 | Craft: Cuốc sắt (cuốc 2 ô). |
| 7 | AI: Auto-water nhỏ. Unlock cây hiếm. |
| 8 | Craft: Rìu thép (khai hoang rừng sâu). |
| 9 | Unlock đầm lầy. Craft thuốc trừ sâu. |
| 10 | AI: Phân tích giống. Trang trại "thông minh". |

**Mastery EXP sources:**
- Thu hoạch cây: +10-30 (tùy loại cây, chất lượng)
- Bán nông sản: +5 mỗi giao dịch
- Hoàn thành đơn hàng/quest: +20-50
- Khai hoang vùng mới: +50-100
- Combat: +2-5 mỗi con (rất ít, phụ)

→ **Farming là nguồn Mastery chính.** Combat cho nguyên liệu, không cho progression đáng kể.

#### Tiền (Gold) — thước đo kinh tế

- Mua hạt giống, vật tư, nâng cấp
- Một số unlock cần tiền (mở rộng nhà, mua đất)
- Tiền KHÔNG phải mục tiêu cuối — mà là công cụ để mở rộng

#### Diện tích trang trại — thước đo visual

- Bắt đầu: 1 mảnh nhỏ (5x5 ô)
- Khai hoang → mở rộng dần
- Mỗi vùng mới = content mới (cây mới, NPC mới, quest mới)

### 4.6 Thời gian & Mùa vụ

| Config | Giá trị |
|--------|---------|
| 1 ngày game | 14 phút thực (đã có trong TimeConfig) |
| 1 mùa | 28 ngày |
| 1 năm | 4 mùa = 112 ngày |
| Giờ bắt đầu | 6:00 |
| Giờ kiệt sức | 2:00 (bắt buộc ngủ) |

**Mùa ảnh hưởng:**
- Cây nào trồng được
- Giá bán nông sản (cung-cầu theo mùa)
- Thời tiết (mưa nhiều hơn vào thu, nắng gắt vào hạ)
- Thú hoang (một số chỉ xuất hiện mùa nhất định)

### 4.7 Weather (Thời tiết)

| Thời tiết | Hiệu ứng |
|-----------|----------|
| Nắng | Bình thường, phải tưới tay |
| Mưa | Auto-water tất cả cây ngoài trời. Không cần tưới → dành stamina cho việc khác |
| Mưa bão | Auto-water + có thể hư hại cây yếu (% nhỏ) |
| Nắng gắt (Hạ) | Cây cần tưới 2 lần/ngày hoặc héo nhanh hơn |

**Tần suất:** Mưa ~30% ngày Xuân/Thu, ~15% Hạ, ~40% Đông.

### 4.8 Shop & Kinh tế

| Feature | Mô tả |
|---------|--------|
| Mua hạt giống | Từ NPC thương lái, stock thay đổi theo mùa |
| Bán nông sản | Giá dao động ±20% theo mùa và supply |
| Mua vật tư | Phân bón, thức ăn động vật, vật liệu xây dựng |
| Đơn hàng đặc biệt | NPC yêu cầu X nông sản → thưởng tiền + Mastery bonus |

### 4.9 Quest System

**Không có quest chain dài.** Chỉ có:

| Loại quest | Mô tả | Reward |
|------------|--------|--------|
| Đơn hàng hàng ngày | "Giao 5 cà chua" | Tiền + Mastery |
| Đơn hàng mùa | "Giao 50 nông sản trước cuối mùa" | Tiền lớn + Unlock |
| Quest khai hoang | "Dọn sạch bìa rừng" | Mở đất mới |
| Quest NPC | "Giúp bác Ba sửa hàng rào" | Friendship + Recipe |

### 4.10 Inventory & Equipment

| Feature | Trạng thái |
|---------|-----------|
| Hotbar (10 slot) | ✅ Có |
| Backpack | ✅ Có |
| Equipment (mũ, áo, giày, phụ kiện) | ✅ Có |
| Stack / Split / Drag-drop | ✅ Có |

**Equipment trong game này:**
- Không có armor/weapon kiểu RPG
- Thay vào đó: **Đồ bảo hộ lao động** (mũ, găng tay, ủng) → tăng stamina, giảm stamina cost, chống thời tiết
- **Công cụ** = vũ khí duy nhất (cuốc, rìu, liềm, bình tưới)

### 4.11 Động vật

| Con vật | Input | Output | Chu kỳ |
|---------|-------|--------|--------|
| Gà | Thức ăn/ngày | Trứng | 1 ngày |
| (Mở rộng sau) | | | |

Giữ đơn giản: 1 loại động vật cho MVP. Thêm sau nếu cần.

---

## 5. CONTENT SCOPE (Con số cụ thể cho MVP)

| Category | Số lượng |
|----------|---------|
| Cây trồng | 16-20 (4-5/mùa) |
| Thú hoang | 6-8 loại |
| Vùng khai hoang | 4 vùng |
| NPC | 4-5 (thương lái, hàng xóm, chuyên gia) |
| Quest | 15-20 (đơn hàng + khai hoang) |
| Công cụ | 4 loại × 3 tier = 12 |
| Craft recipe | 10-15 (tool upgrade + food + phân bón) |
| Food items | 5-8 (hồi stamina) |
| Scenes | 3 (Farm, Town, Wilderness) |

---

## 6. CUT LIST (KHÔNG LÀM)

Những thứ tuyệt đối không triển khai trong MVP:

- ❌ Romance / hẹn hò NPC
- ❌ Dungeon nhiều tầng
- ❌ PvP / Multiplayer
- ❌ Procedural generation
- ❌ Fishing mini-game phức tạp
- ❌ Hệ thống thú cưỡi
- ❌ Skill tree phức tạp
- ❌ Crafting UI phức tạp (giữ đơn giản: có nguyên liệu → craft)
- ❌ Lễ hội / seasonal events
- ❌ Housing decoration chi tiết
- ❌ Nhiều hơn 1 save slot (MVP)
- ❌ Achievement system
- ❌ Hệ thống level chung (L1-L50) — thay bằng Farming Mastery

---

## 7. UI REQUIREMENTS (Functional)

| UI Element | Mô tả |
|------------|--------|
| HUD | Stamina bar, thời gian (giờ + ngày + mùa), tiền, Mastery level |
| Inventory | Grid-based, drag-drop, tooltip |
| Shop | Danh sách mua/bán, giá, stock |
| AI Assistant | Panel nhỏ góc phải: gợi ý, thông báo |
| Dialogue | Text box dưới màn hình, portrait NPC |
| Quest log | Danh sách đơn hàng đang active |
| Map | Mini-map đơn giản hiển thị vùng đã khai hoang |
| End-of-day | Tổng kết: thu nhập, cây phát triển, Mastery gained |

---

## 8. TECHNICAL NOTES

### 8.1 Thay đổi cần thiết so với code hiện tại

| Hệ thống hiện tại | Thay đổi |
|-------------------|----------|
| ProgressionService (L1-L50) | Chuyển thành FarmingMasteryService |
| StatType.Level/Exp/MaxExp | Giữ lại nhưng rename concept → Mastery |
| EnemyObject (chase/attack AI) | Giữ, nhưng enemy = thú hoang, chỉ ở vùng khai hoang |
| WeaponRuntime | Bỏ weapon riêng, dùng tool (Scythe/Axe) làm vũ khí |
| ToolRuntime | Thêm WateringCanRuntime |
| StageRuntime | Thêm check watered trước khi grow |
| TimeManager | Đã đủ, thêm Weather system |
| ShopService | Đã đủ, thêm seasonal price variation |
| QuestService | Đã đủ, thêm daily order generation |

### 8.2 Systems cần xây mới

| System | Mô tả | Priority |
|--------|--------|----------|
| WateringCanRuntime | Tưới nước → đổi tile → flag watered | 🔴 |
| WateredTileTracker | Track ô nào đã tưới, reset mỗi ngày | 🔴 |
| StageRuntime update | Chỉ grow nếu watered = true | 🔴 |
| SleepSystem | Interact giường → end day → restore stamina | 🔴 |
| WeatherSystem | Random weather/ngày, ảnh hưởng watering | 🟡 |
| FarmingMasteryService | Thay ProgressionService, EXP từ farming | 🟡 |
| WiltSystem | Cây không tưới 2+ ngày → héo | 🟡 |
| SeasonCropRestriction | Check mùa khi trồng | 🟡 |
| AIAssistantUI | Hint/tooltip system | 🟢 |
| DailyOrderGenerator | Random quest mỗi ngày | 🟢 |

---

## 9. IMPLEMENTATION PRIORITY

### Phase 1: Farming Loop Hoàn Chỉnh (tuần 1-2)
> Mục tiêu: 1 ngày chơi farming có ý nghĩa

1. WateringCanRuntime + WateredTileTracker
2. StageRuntime chỉ grow khi watered
3. SleepSystem (end day + stamina restore)
4. Stamina cost cho tất cả farming tools
5. Seed items trong shop (mua hạt → trồng)

### Phase 2: Survival Pressure (tuần 3-4)
> Mục tiêu: Mỗi ngày có tension, quyết định có hậu quả

6. Season restriction (cây chỉ trồng đúng mùa)
7. Cây héo nếu không tưới
8. Weather system (mưa = auto-water)
9. Food crafting (nông sản → food → hồi stamina)
10. End-of-day summary UI

### Phase 3: Khai Hoang & Mở Rộng (tuần 5-6)
> Mục tiêu: Có lý do rời trang trại

11. Vùng khai hoang với thú hoang
12. Thú hoang drop nguyên liệu farming
13. Khai hoang xong = mở đất mới
14. Tool upgrade (T1 → T2 → T3)
15. FarmingMasteryService thay thế Level

### Phase 4: Polish & Content (tuần 7-8)
> Mục tiêu: Game cảm thấy hoàn chỉnh

16. AI Assistant UI (hints, gợi ý)
17. Daily orders (quest ngẫu nhiên)
18. NPC dialogue + personality
19. Narrative moments (nhật ký, tin nhắn)
20. Balance pass (giá cả, stamina cost, growth time)

---

## 10. NGUYÊN TẮC THIẾT KẾ

1. **Farming là vua** — Mọi hệ thống khác phục vụ farming, không ngược lại.
2. **Mỗi ngày phải có ritual** — Thức dậy → có việc phải làm → thỏa mãn khi xong.
3. **Quyết định có hậu quả** — Stamina có hạn, mùa có hạn, không tưới = mất cây.
4. **Tiến triển phải nhìn thấy** — Trang trại thay đổi visual, tool mạnh hơn, thu nhập tăng.
5. **Combat phục vụ farming** — Đánh thú = lấy nguyên liệu + mở đất, KHÔNG phải grind EXP.
6. **Respect thời gian người chơi** — 15 phút chơi = phải có tiến triển đáng kể.
7. **Cốt truyện không ép** — Đến tự nhiên, không bao giờ block gameplay.
8. **AI là flavor, không phải gimmick** — Mechanic AI phải hữu ích thật, không chỉ để "cool".
