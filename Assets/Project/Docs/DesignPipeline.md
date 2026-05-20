# 🎯 DESIGN PIPELINE — FARM & CODE

> Đi theo đúng lộ trình. Mỗi bước phải CHỐT trước khi sang bước tiếp.
> Output mỗi bước sẽ được ghi vào GDD khi hoàn thành.

---

## PIPELINE OVERVIEW

```
[1] Ý tưởng chính ──────── CHỐT
 ↓
[2] Gameplay Loop ────────── CHỐT
 ↓
[3] Mục tiêu người chơi ─── CHỐT
 ↓
[4] Core Systems ─────────── CHỐT
 ↓
[5] Prototype ────────────── BUILD
 ↓
[6] Vertical Slice ───────── BUILD
 ↓
[7] Content mở rộng ──────── FILL
 ↓
[8] Playtest ─────────────── TEST
 ↓
[9] Polish ───────────────── REFINE
 ↓
[10] Release ─────────────── SHIP
```

---

## [1] Ý TƯỞNG CHÍNH

### Status: ✅ CHỐT

### 1.1 Thể loại
Farming Survival 2D Top-Down

### 1.2 Cốt truyện (timeline thế giới)
- **2026:** Mỹ công bố 161 hồ sơ UFO. Nhiều quốc gia theo sau. Thành lập tổ chức nghiên cứu quốc tế.
- **2027:** Từ dữ liệu nghiên cứu → phát minh vật liệu/công nghệ mới.
- **2029:** Tai nạn phòng thí nghiệm → chất lạ lan toàn cầu → sinh vật biến đổi (mutant).
- **2030:** AI bùng nổ → ngành IT sa thải hàng loạt. Nhân vật chính (dev IT vừa tốt nghiệp) thất nghiệp, bỏ phố về quê.
- **Game bắt đầu:** Nhân vật kế thừa đất quê, dùng kiến thức công nghệ + AI để phát triển nông nghiệp.

### 1.3 Narrative Structure: PROGRESSIVE REVEAL
- **Phase 1 (Ngày 1 → ~Ngày X):** Pure farming. Yên bình. Người chơi học mechanics, xây trang trại.
- **Dần dần:** Tin tức xuất hiện (TV/radio/tin nhắn) — thế giới bên ngoài đang có vấn đề.
- **Phase 2 (Ngày X+):** Mutant lan tới vùng quê → combat + khai hoang mở ra.
- Cốt truyện KHÔNG ép đọc. Đến qua tin tức, nhật ký, NPC, lore items.

### 1.4 Setting
- **Fictional world lấy cảm hứng Việt Nam** (tên, văn hóa, cây trồng, kiến trúc gợi VN nhưng không phải VN thật)

### 1.5 Tone
- **Hopeful** — thế giới có vấn đề, mutant nguy hiểm, nhưng không tuyệt vọng. Người chơi đang xây dựng lại cuộc sống. Ánh sáng cuối đường hầm.

### 1.6 Điểm khác biệt
- Cốt truyện gắn thực tế (AI thay thế việc làm, UFO disclosure 2026)
- AI là mechanic thật trong game
- Combat = khai hoang (đánh mutant để mở đất), unlock SAU khi farming đã establish
- Progressive reveal — game bắt đầu peaceful, escalate dần
- Target: Gen Z Việt Nam, relatable

---

## [2] GAMEPLAY LOOP

### Status: 🟡 Đang xác định

### 2.1 Daily Loop (1 ngày)
```
Thức dậy → Chăm trang trại (tưới, cho ăn, thu hoạch)
    → Bán/mua/craft
        → Khai hoang HOẶC mine HOẶC mở rộng farm
            → Về nhà ngủ → Ngày mới
```

### 2.2 Weekly Loop (7 ngày)
```
Trồng cây → Chăm mỗi ngày → Thu hoạch cuối tuần
    → Bán → Có tiền → Mua hạt tốt hơn / Nâng cấp tool
        → Trồng nhiều hơn / Mở đất mới
```

### 2.3 Long-term Loop (nhiều tuần)
```
Mở vùng khai hoang → Đánh mutant → Lấy nguyên liệu
    → Craft tool/equipment tốt hơn → Mở vùng khó hơn
        → Trang trại lớn hơn → Thu nhập cao hơn → ...
```

### 2.4 Câu hỏi cần chốt
- [ ] 1 ngày game = bao nhiêu phút thực? (hiện tại 14 phút)
- [ ] Stamina mặc định bao nhiêu? Đủ làm gì trong 1 ngày?
- [ ] Cây nhanh nhất mấy ngày chín? Chậm nhất?
- [ ] Khai hoang 1 vùng mất bao lâu (bao nhiêu ngày)?

---

## [3] MỤC TIÊU NGƯỜI CHƠI

### Status: ⬜ Chưa bắt đầu

### 3.1 Mục tiêu ngắn hạn (mỗi ngày)
- ?

### 3.2 Mục tiêu trung hạn (mỗi tuần/tháng game)
- ?

### 3.3 Mục tiêu dài hạn (endgame)
- ?

### 3.4 Câu hỏi cần chốt
- [ ] Game endless nhưng có "soft ending" (story kết thúc) không?
- [ ] Người chơi "thắng" khi nào? (trang trại đạt level X? story xong? tiền đạt Y?)
- [ ] Có leaderboard/achievement không?
- [ ] Motivation chơi tiếp sau khi "xong" story là gì?

---

## [4] CORE SYSTEMS

### Status: ⬜ Chưa bắt đầu

Sẽ được xây dựng SAU KHI chốt bước 1-3.

Output: System Architecture Map (script tree cho từng feature)

---

## [5] PROTOTYPE

### Status: ⬜ Chưa bắt đầu

Sẽ được build SAU KHI chốt bước 4.

Output: Playable prototype với core loop cơ bản nhất

---

## [6-10] CÁC BƯỚC SAU

Sẽ được plan khi đến lượt.

---

## TIẾN ĐỘ HIỆN TẠI

| Bước | Status | Output |
|------|--------|--------|
| 1. Ý tưởng | ✅ CHỐT | Concept, timeline, setting, tone, narrative structure |
| 2. Gameplay Loop | 🟡 50% | Có framework, cần chốt con số |
| 3. Mục tiêu | ⬜ 0% | Chưa xác định |
| 4. Core Systems | ⬜ 0% | Chờ bước 1-3 |
| 5. Prototype | 🟡 40% | Code đã có nhiều, nhưng chưa align với design mới |
| 6-10 | ⬜ | Chưa đến |

---

## NEXT ACTION

→ Chốt hết câu hỏi trong **Bước 1** trước, rồi sang **Bước 2**, rồi **Bước 3**.
