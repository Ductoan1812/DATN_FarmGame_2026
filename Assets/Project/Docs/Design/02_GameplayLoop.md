# 02 — GAMEPLAY LOOP

> Status: ✅ CHỐT

---

## Triết lý

Mỗi hành động phải có: **Ý nghĩa** (tại sao làm) → **Phần thưởng** (nhận được gì) → **Dẫn dắt** (mở ra feature tiếp theo).

Nguyên tắc cốt lõi: **Một vấn đề sản sinh ra vấn đề khác.**

---

## Chuỗi vấn đề → giải pháp (Feature Chain)

```
Stamina ít (100) → Cần tool tốt hơn
    → Cần nguyên liệu → Mining
        → Mining tốn stamina → Cần food buff
            → Cần recipe → Quest/Research
                → Recipe cần nguyên liệu đặc biệt → Combat (mutant drop)
                    → Combat mất HP → Cần equipment
                        → Equipment cần material cao → Khai hoang vùng sâu
                            → Vùng sâu cần weapon tốt → Research
                                → Research cần lore items → Story progression
```

→ Không có shortcut bằng tiền. Mỗi nâng cấp đòi hỏi đi qua nhiều feature.

---

## Sơ đồ kết nối Feature

```
         FARMING (core)
             │
         KINH TẾ (bán/mua)
             │
    ┌────────┼────────┐
    │        │        │
 MINING   CHĂN NUÔI  QUEST
    │        │        │
    └────────┼────────┘
             │
         CRAFTING
             │
    ┌────────┼────────┐
    │        │        │
 TOOL↑   FOOD↑    EQUIPMENT↑
    │        │        │
    └────────┼────────┘
             │
      COMBAT/KHAI HOANG
             │
      MỞ RỘNG THẾ GIỚI
             │
         (Loop lại)
```

---

## Stamina — Trục trung tâm

### Config: 1 ngày = 14 phút thực. Stamina khởi đầu = 100.

### Progression Path

| Giai đoạn | Stamina | Cách đạt | Vấn đề sinh ra |
|-----------|---------|----------|----------------|
| 1 (Ngày 1-7) | 100 | Mặc định | Muốn trồng nhiều hơn → Crafting |
| 2 (Ngày 7-15) | 120 + Tool T2 | Craft tool (quặng + gỗ) | Mining tốn stamina → Food |
| 3 (Ngày 15-25) | 140 + Food buff | Nấu ăn (nông sản → food) | Cần nguyên liệu đặc biệt → Combat |
| 4 (Ngày 25-40) | 160 + Equipment | Craft từ mutant material | Farm lớn quá → Automation |
| 5 (Ngày 40+) | Sprinkler + máy | Craft từ quặng cao + research | Vùng mới, vòng mới |

### Stamina Cost

| Hành động | Tool T1 | Tool T2 | Tool T3 |
|-----------|---------|---------|---------|
| Tưới (1 ô) | 2 | 1.5 (3 ô/lần) | 1 (5 ô/lần) |
| Cuốc (1 ô) | 4 | 3 (2 ô/lần) | 2 (3 ô/lần) |
| Gieo hạt | 1 | 1 | 1 |
| Thu hoạch | 2 | 2 | 1 |
| Chặt cây | 6 | 4 | 3 |
| Đập đá | 6 | 4 | 3 |
| Combat (1 hit) | 3 | 2.5 | 2 |
| Dodge | 2 | 2 | 1 |
| Cho vật nuôi ăn | 2 | 2 | 1 (auto-feeder) |

---

## Micro Loop (1 ngày)

```
06:00  Thức dậy (Stamina full)
06-12  Chăm trang trại: tưới, thu hoạch, gieo, cho ăn
12-18  Lựa chọn: Bán/mua | Mining | Khai hoang | Craft
18-02  Hoàn thành việc dở + về ngủ
02:00  Kiệt sức (bắt buộc ngất, -50% stamina ngày sau)
```

## Meso Loop (1 tuần)

Trồng → Chăm mỗi ngày → Thu hoạch → Bán → Mua hạt/vật tư tốt hơn → Trồng lại

## Macro Loop (nhiều tuần)

Mở vùng → Nguyên liệu mới → Craft/upgrade → Farming hiệu quả hơn → Mở vùng tiếp

---

## Nhịp độ

| Loại | Thời gian | Ví dụ |
|------|-----------|-------|
| Cây nhanh | 3-4 ngày | Rau xanh (30-60g) |
| Cây trung bình | 5-7 ngày | Khoai, cà chua (80-150g) |
| Cây chậm | 8-12 ngày | Dưa hấu, bí (200-400g) |
| Khai hoang vùng 1 | 2-3 ngày | Bìa rừng |
| Khai hoang vùng 2 | 5-7 ngày | Rừng sâu |
| Khai hoang vùng 3 | 8-12 ngày | Đồi đá |
| Khai hoang vùng 4 | 15-20 ngày | Vùng ô nhiễm |

---

## Phase Transition

| Ngày | Sự kiện |
|------|---------|
| 1-6 | Pure farming. Học mechanics. |
| 7 | Tin tức đầu tiên: "Sinh vật lạ ở vùng lân cận" |
| 8-9 | NPC bàn tán, thêm tin tức |
| 10 | Mutant đầu tiên xuất hiện → combat tutorial |
| 10+ | Vùng khai hoang mở, game mở rộng |

---

## Động lực theo giai đoạn

| Giai đoạn | Người chơi muốn | Feature dẫn dắt |
|-----------|----------------|-----------------|
| Ngày 1-3 | Sống sót, kiếm tiền đầu tiên | Farming + Shop |
| Ngày 4-7 | Mở rộng, tool tốt hơn | Mining + Craft |
| Ngày 7-10 | Biết chuyện gì đang xảy ra | Narrative |
| Ngày 10-20 | Bảo vệ + khám phá | Combat + Khai hoang |
| Ngày 20-40 | Mạnh hơn, hiệu quả hơn | Equipment + Automation |
| Ngày 40+ | Trang trại hoàn hảo | Sprinkler + Research + Chăn nuôi |
