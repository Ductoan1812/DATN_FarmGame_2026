# NỘI DUNG CHI TIẾT 13 SLIDES — SurvivalFarm

---
## CẤU TRÚC TỔNG THỂ

| Phần | Slides | Mục đích |
|------|--------|----------|
| Mở đầu | 1–3 | Cover, mục tiêu, xem game trực quan |
| Nền tảng | 4–5 | Bài toán đặt ra → Tính năng giải quyết |
| Thiết kế | 6–8 | Tư duy game designer — tại sao làm vậy |
| Kỹ thuật | 9–11 | EntityData, Module, Save/Load |
| Kết | 12–13 | Demo, Kết luận |

---

## SLIDE 1 — Cover

**Tiêu đề chính:** SurvivalFarm
**Phụ đề:** Game Nông Trại Sinh Tồn 2D

**Nội dung hiển thị:**
- Tên đồ án: Xây dựng Game Nông Trại Sinh Tồn 2D bằng Unity
- Sinh viên: Nông Đức Toàn
- Giảng viên hướng dẫn: [Tên thầy]
- Trường / Khoa / Năm: [Điền thông tin]

**Visual:** Screenshot đẹp nhất của game hoặc key art

---

## SLIDE 2 — Mục tiêu đề tài

**Tiêu đề:** Mục tiêu đề tài

**Nội dung hiển thị:**

**Mục tiêu:**
- Xây dựng game 2D top-down kết hợp farming + survival
- Hệ thống dữ liệu item tự thiết kế, có thể mở rộng dễ dàng
- Vòng lặp gameplay có chiều sâu, giữ chân người chơi lâu dài

**Phạm vi:**
- Nền tảng: PC — Unity 2D, C#
- Chế độ: Single player

*Ghi chú: Slide này ngắn gọn — dùng để định hướng, không mở rộng ở đây*

---

## SLIDE 3 — Giới thiệu sản phẩm (Video showcase)

**Tiêu đề:** SurvivalFarm — Trải nghiệm thực tế

**Nội dung hiển thị:**
- **[Chạy video showcase]** — cắt ghép các cảnh đẹp nhất trong game
- Hoặc: bộ 4–6 screenshot full-size bố cục grid

**Những cảnh nên có trong video/screenshot:**
1. Cảnh trang trại xanh tươi — đang thu hoạch
2. Cảnh nhân vật tưới nước — bình tưới đầy vs gần hết
3. Cảnh trong mỏ — chiến đấu quái hoặc đang đào quặng
4. Cảnh NPC shop — với các item bị khóa mờ hiện ra
5. Cảnh inventory — nhiều item đa dạng
6. Cảnh đặt vòi tưới tự động hoạt động

**Kịch bản nói:** Không cần giải thích nhiều — để hình ảnh tự nói. Chỉ nói ngắn: *"Đây là game em đã xây dựng. Trước khi đi vào chi tiết, xin hội đồng nhìn qua những gì người chơi sẽ trải nghiệm."*

---

## SLIDE 4 — Bài toán thiết kế game

**Tiêu đề:** Bài toán đặt ra khi xây dựng game

**Nội dung hiển thị:**

*Mỗi bài toán dưới đây dẫn đến một quyết định thiết kế cụ thể ở các slide tiếp theo*

**① Làm sao để người chơi tự biết cần làm gì — không cần tutorial?**
→ Curiosity-driven onboarding

**② Chiến đấu và farming — làm sao để hai tính năng không triệt tiêu nhau?**
→ Đòn bẩy thời gian, combat làm tiền đề cho công cụ farming

**③ Làm sao để người chơi không bỏ game sau vài ngày?**
→ Daily loop, level gating, random drop

**④ Hàng trăm loại item — làm sao quản lý mà không lặp code?**
→ EntityData + Modular Architecture

**⑤ Làm sao đảm bảo game state không mất khi tắt ngang?**
→ Save/Load 3 file JSON + boot 5 pha

**Thông điệp slide:** Mỗi tính năng trong game là câu trả lời cho một bài toán có thực — không phải thêm cho có.

---

## SLIDE 5 — Các Tính năng & Hướng chơi

**Tiêu đề:** Game có gì? Người chơi làm được gì?

**Nội dung hiển thị:**

**Thế giới game — 3 khu vực:**
| Khu vực | Hoạt động chính |
|---------|----------------|
| Trang trại | Cuốc đất, gieo hạt, tưới nước, thu hoạch |
| Mỏ / Dungeon | Khai thác quặng, chiến đấu quái vật |
| Làng / NPC | Mua bán, nhận quest, mở khóa item |

**Các hệ thống chính:**

**Canh tác** — Nhiều loại cây, mỗi loại có số ngày sinh trưởng khác nhau. Bình tưới có giới hạn charges — cần quản lý.

**Khai khoáng** — Phá đá, thu quặng. Nguyên liệu để craft công cụ cao cấp.

**Chiến đấu** — Quái vật trong mỏ. Drop item ngẫu nhiên khi tiêu diệt.

**NPC & Shop** — Bán nông sản, mua hạt/công cụ. Item mở dần theo level và quest.

**Crafting** — Kết hợp nguyên liệu → công cụ. Ví dụ: vòi tưới tự động.

**Progression** — Level nhân vật, hệ thống quest, inventory phân loại.

---

## SLIDE 6 — Onboarding — Không Tutorial

**Tiêu đề:** Thiết kế Trải nghiệm — Curiosity-Driven Onboarding

**Nội dung hiển thị:**

**Bài toán ①:** Làm sao để người chơi tự biết cần làm gì?

**Giải pháp: Không tutorial — chỉ tình huống**

**Hành trình người chơi mới:**
```
Bắt đầu với 10 hạt giống
        ↓
Tự khám phá: cuốc đất → gieo → tưới → chờ → thu hoạch
        ↓
Tìm NPC để bán hàng lần đầu
        ↓
Nhìn vào shop → thấy nhiều item bị khóa (bóng mờ)
        ↓
"Cái đó là gì? Làm sao để mở?"  ← câu hỏi tự xuất hiện
```

**Nguyên tắc: "Visible but Locked"**
- Cho người chơi thấy đích trước khi cho phép tới
- Không cần nói "hãy làm X để mở Y" — người chơi tự muốn tìm ra

---

## SLIDE 7 — Combat: Tính năng hay Cạm bẫy?

**Tiêu đề:** Combat Design — Tính năng hay Cạm bẫy cho Nhà phát triển?

**Nội dung hiển thị:**

**Bài toán ②:** Chiến đấu và farming — làm sao để không triệt tiêu nhau?

```
Nếu: combat + bán item drop  >  farming về lợi nhuận
→ Người chơi bỏ ruộng, chỉ chiến đấu
→ Farming trở nên thừa, buồn chán
→ Game mất định hướng thể loại
```

**Giải pháp: Đòn bẩy thời gian**
> Mỗi ngày phải tưới tay 10–50 ô đất. Bình tưới giới hạn charges.
> Tưới nhiều = mất phần lớn thời gian trong ngày.

**Kết quả thiết kế:**
```
Ruộng lớn → gánh nặng tưới tăng → tìm vòi tự động
        ↓
Vòi tự động cần quặng → phải vào mỏ → phải chiến đấu
        ↓
Combat và Farming: từ đối thủ → đồng minh ✓
```

Combat ở mức cơ bản là quyết định có chủ đích — nếu quá mạnh, game hóa thành RPG và mất bản sắc nông trại.

---

## SLIDE 8 — Progression & Giữ chân người chơi

**Tiêu đề:** Hệ thống Progression & Giữ chân Người chơi

**Nội dung hiển thị:**

**Bài toán ③:** Làm sao để người chơi không bỏ game sau vài ngày?

**① Vòng lặp theo ngày — Daily Commitment**
- Cây trồng cần X ngày mới chín → người chơi tắt game nhưng đã nghĩ "ngày mai phải vào tưới"
- → Tạo thói quen mở game đều đặn

**② Level + Quest Gating — Visible Progression**
- Item mới mở theo level và hoàn thành quest
- Luôn thấy mục tiêu tiếp theo — đủ gần để kích thích, đủ xa để phấn đấu

**③ Random Drop — Variable Reward**
- Mỗi lần vào mỏ có xác suất nhận item hiếm
- Không biết lần này có drop không → thử thêm một lần
- → Nguyên lý tâm lý học: variable reward schedule

**Vòng lặp tổng thể:**
```
Thu hoạch → Tiền → Thấy item locked → Vào mỏ → Random drop
→ Craft công cụ → Farm hiệu quả → Mở rộng ruộng → [lặp lại]
```

---

## SLIDE 9 — EntityData Architecture

**Tiêu đề:** Kiến trúc Dữ liệu — EntityData & EntityRuntime

**Nội dung hiển thị:**

**Bài toán ④:** Hàng trăm loại item — làm sao quản lý mà không lặp code?

**Giải pháp: EntityData (ScriptableObject)**
```
EntityData (định nghĩa — dùng chung cho tất cả instance)
├── id, keyName, descKey
├── icon, category
├── maxStack, buyPrice, sellPrice
├── baseStats (StatsData)
└── modules: List<IModuleData>

EntityRuntime (instance — mỗi item trong tay người chơi)
├── id (GUID duy nhất)
├── entityData → (tham chiếu EntityData)
├── Amount, Quality
└── stats, modules (runtime state riêng)
```

**Ví dụ — Bình tưới Tier 1:**
- Instance A: CurrentCharges **20/20** (đầy)
- Instance B: CurrentCharges **5/20** (gần hết)
- → Cùng 1 EntityData, hai trạng thái runtime khác nhau

**Lợi ích:** Thêm item mới = tạo 1 file .asset, không sửa code

---

## SLIDE 10 — Modular Architecture

**Tiêu đề:** Thiết kế Module — Mở rộng không giới hạn

**Nội dung hiển thị:**

**Vấn đề:** Bình tưới, rìu, hạt giống, quặng — mỗi loại hành vi khác nhau. Làm sao không viết class riêng cho từng cái?

**Giải pháp: Composition over Inheritance**

| Module | Chức năng |
|--------|-----------|
| `ToolModule` | Đây là công cụ, thuộc tier mấy |
| `StaminaCostModule` | Dùng tốn bao nhiêu stamina |
| `ToolRequirementModule` | Cần level bao nhiêu để dùng |
| `AppearanceModule` | Cách hiển thị trong thế giới |

**Ví dụ thực tế:**
| Item | Modules |
|------|---------|
| Bình tưới Tier 1 | ToolModule + StaminaCostModule + AppearanceModule |
| Rìu khai mỏ | ToolModule + StaminaCostModule + ToolRequirementModule |
| Hạt giống | SeedModule + AppearanceModule |

**Kết quả:**
- Thêm item mới → chọn module, tạo asset, xong
- Thêm cơ chế mới → viết 1 module, gắn vào bất kỳ item nào
- Không đụng code cũ → không tạo bug cũ

---

## SLIDE 11 — Save/Load System

**Tiêu đề:** Hệ thống Save/Load — Nền tảng kỹ thuật

**Nội dung hiển thị:**

**Bài toán ⑤:** Làm sao đảm bảo game state không mất khi tắt ngang?

**3 file lưu trữ (JSON):**
| File | Nội dung |
|------|---------|
| `entity.json` | Toàn bộ item trên bản đồ, trong inventory |
| `player.json` | Nhân vật: vị trí, stats, tiền, level |
| `world.json` | Ô đất đã cuốc, cây đang giai đoạn mấy, đã tưới chưa |

**Quy trình boot 5 pha:**
1. Khởi tạo Registry
2. Load World Data
3. Khởi tạo EventBus
4. Load Entity Data
5. Load Player Data

> Sai thứ tự = EntityRuntime khởi tạo trước khi Registry sẵn sàng = crash.

**Kết quả:** Chơi 30 phút → tắt ngang → bật lại: cây đúng giai đoạn, bình tưới đúng charges, inventory đúng 100%.

---

## SLIDE 12 — Demo

**Tiêu đề:** Demo Gameplay

**Nội dung hiển thị:** [Chạy game trực tiếp]

**Hành trình demo (2–3 phút):**
1. Bắt đầu → 10 hạt giống → cuốc, gieo, tưới
2. Bình tưới hết charges → minh họa time pressure
3. Thu hoạch → NPC bán → thấy item locked trong shop
4. Vào mỏ → chiến đấu → nhặt quặng
5. Craft vòi tưới tự động → đặt xuống → ruộng tự tưới
6. Save → tắt → load lại → kiểm tra trạng thái còn nguyên

---

## SLIDE 13 — Kết luận & Hướng phát triển

**Tiêu đề:** Kết luận & Hướng phát triển

**Nội dung hiển thị:**

**Đã đạt được:**
- Hệ thống dữ liệu item (EntityData + Modular) tự xây, dễ mở rộng
- Save/Load toàn bộ trạng thái game, không mất dữ liệu
- Vòng lặp gameplay: farming ↔ mining ↔ combat bổ trợ nhau
- Onboarding curiosity-driven, không cần tutorial
- Progression: level gating, quest, random drop

**Bài học lớn nhất:**
> "Game design không phải là thêm tính năng — mà là cân bằng tính năng để chúng bổ trợ nhau."

**Hướng phát triển:**
- Multiplayer: chơi chung với bạn bè
- Thêm biomes: sa mạc, tuyết — cây đặc trưng mỗi vùng
- Mở rộng crafting: nhiều loại máy móc tự động hóa hơn
- Storyline: hệ thống quest có cốt truyện

**Cảm ơn hội đồng đã lắng nghe!**
