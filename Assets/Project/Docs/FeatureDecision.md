# 📋 FEATURE DECISION DOCUMENT

> **Mục đích:** Xác định rõ game sẽ có chức năng gì trước khi viết GDD chi tiết.
> **Cách dùng:** Đánh dấu mỗi feature: ✅ GIỮ | ❌ BỎ | ⏳ SAU (post-MVP)
> **Nguyên tắc:** Indie solo dev → chỉ giữ những gì tạo core experience.

---

## NHÓM 1: FARMING (Trồng trọt)

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| F1 | Cuốc đất | Biến đất hoang → đất trồng | ✅ Đã có |
| F2 | Gieo hạt | Mua/tìm hạt → đặt xuống đất | ✅ Đã có |
| F3 | Tưới nước | Bình tưới, cây cần tưới mỗi ngày | ? |
| F4 | Thu hoạch | Cây chín → nhặt → vào inventory | ✅ Đã có |
| F5 | Cây phát triển theo ngày | Mỗi ngày cây lên 1 stage | ✅ Đã có |
| F6 | Cây cần tưới mới lớn | Không tưới = không grow | ? |
| F7 | Cây héo/chết | Không tưới 2+ ngày → héo → mất | ? |
| F8 | Mùa vụ (4 mùa) | Mỗi mùa có cây riêng, trồng sai mùa = chết | ? |
| F9 | Chất lượng nông sản (1-3★) | Tưới đều + phân bón → chất lượng cao → giá cao | ? |
| F10 | Phân bón | Item tăng tốc/tăng chất lượng cây | ? |
| F11 | Sprinkler/Auto-water | Craft item tự động tưới vùng nhỏ | ? |
| F12 | Đất có chất lượng | Đất tốt/xấu ảnh hưởng cây | ? |
| F13 | Cây tái thu hoạch | Một số cây thu hoạch nhiều lần (dâu, cà chua) | ? |

---

## NHÓM 2: CHĂN NUÔI

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| A1 | Gà | Cho ăn → trứng mỗi ngày | ? |
| A2 | Bò | Cho ăn → sữa | ? |
| A3 | Cừu | Cho ăn → lông cừu | ? |
| A4 | Chuồng trại | Xây chuồng để nuôi | ? |
| A5 | Động vật chết/bệnh | Không cho ăn → bệnh/chết | ? |
| A6 | Nhân giống | Ghép cặp → con mới | ? |

---

## NHÓM 3: THỜI GIAN & MÔI TRƯỜNG

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| T1 | Đồng hồ trong game | 1 ngày = X phút thực | ✅ Đã có |
| T2 | Ngày/đêm visual | Ánh sáng thay đổi theo giờ | ✅ Đã có |
| T3 | 4 mùa (Xuân/Hạ/Thu/Đông) | Mỗi mùa 28 ngày | ? |
| T4 | Thời tiết (nắng/mưa/bão) | Ảnh hưởng farming + gameplay | ? |
| T5 | Nhiệt độ | Ảnh hưởng cây trồng/động vật | ? |
| T6 | Thiên tai | Bão lớn phá hại cây/chuồng | ? |

---

## NHÓM 4: COMBAT & KHAI HOANG

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| C1 | Đánh bằng công cụ | Cuốc/rìu/liềm = vũ khí | ✅ Đã có |
| C2 | Enemy cơ bản (mutant) | Sinh vật biến đổi, AI đơn giản | ✅ Đã có (framework) |
| C3 | Vùng khai hoang | Vào vùng hoang → dọn dẹp → mở đất | ? |
| C4 | Enemy drop nguyên liệu | Mutant drop vật liệu đặc biệt | ? |
| C5 | Vũ khí riêng (kiếm, cung...) | Weapon ngoài tool | ? |
| C6 | Boss | Boss cuối mỗi vùng | ? |
| C7 | Enemy spawn theo mùa/đêm | Một số enemy chỉ xuất hiện điều kiện nhất định | ? |
| C8 | Player nhận damage | Bị đánh → mất HP | ✅ Đã có |
| C9 | Player chết → hậu quả | Chết = mất item? mất ngày? respawn? | ? |
| C10 | Dodge/né | Lăn né tránh | ✅ Đã có |
| C11 | Trap/bẫy | Đặt bẫy bắt/giết enemy | ? |
| C12 | Tường/hàng rào bảo vệ | Xây rào ngăn enemy vào farm | ? |

---

## NHÓM 5: CRAFTING & NÂNG CẤP

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| CR1 | Nâng cấp tool (tier) | Cuốc gỗ → sắt → thép (cần nguyên liệu) | ? |
| CR2 | Craft food | Nông sản → món ăn (hồi stamina/HP) | ? |
| CR3 | Craft phân bón | Nguyên liệu → phân bón | ? |
| CR4 | Craft vật liệu xây dựng | Gỗ + đá → plank, brick... | ? |
| CR5 | Craft equipment (đồ bảo hộ) | Nguyên liệu → mũ/găng/ủng | ? |
| CR6 | Craft sprinkler/máy móc | Nguyên liệu nâng cao → automation | ? |
| CR7 | Craft thuốc/potion | Thảo mộc → thuốc chữa/buff | ? |
| CR8 | Research/Nghiên cứu | Dùng nguyên liệu + thời gian → unlock recipe mới | ? |

---

## NHÓM 6: MINING & TÀI NGUYÊN

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| M1 | Đập đá/quặng | Pickaxe → drop ore | ✅ Đã có |
| M2 | Chặt cây | Axe → drop gỗ | ✅ Đã có |
| M3 | Hái thảo mộc | Nhặt từ vùng hoang | ? |
| M4 | Mine/hang động | Khu vực riêng để mine sâu | ? |
| M5 | Quặng theo tier | Đồng → Sắt → Vàng → Mythril | ? |
| M6 | Tài nguyên respawn | Đá/cây mọc lại sau X ngày | ? |

---

## NHÓM 7: NPC & XÃ HỘI

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| N1 | NPC shop (mua/bán) | Thương lái bán hạt, mua nông sản | ✅ Đã có |
| N2 | NPC dialogue | Nói chuyện, nhận thông tin | ✅ Đã có |
| N3 | NPC quest/đơn hàng | "Giao X item" → reward | ✅ Đã có |
| N4 | Friendship/thiện cảm | Tặng quà → tăng quan hệ → unlock | ? |
| N5 | NPC schedule | NPC di chuyển theo giờ/ngày | ? |
| N6 | Thuê NPC làm việc | Trả tiền → NPC tưới/thu hoạch giúp | ? |

---

## NHÓM 8: PROGRESSION & UNLOCK

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| P1 | Farming Mastery (thay Level) | Thu hoạch = +Mastery → unlock | ? |
| P2 | Hệ thống Level truyền thống | L1-L50, EXP từ mọi nguồn | ✅ Đã có (cân nhắc bỏ/đổi) |
| P3 | Unlock bằng nguyên liệu | Cần material cụ thể để mở khóa | ? |
| P4 | Unlock bằng tiền | Mua bằng gold | ? |
| P5 | Unlock bằng quest/story | Hoàn thành quest → mở content | ? |
| P6 | Skill tree | Nhiều nhánh skill chọn | ? |
| P7 | Tiền là thước đo chính | Gold = progression indicator | ? |
| P8 | Diện tích trang trại = progression | Mở rộng đất = tiến triển visible | ? |

---

## NHÓM 9: SAVE/LOAD & META

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| S1 | Save/Load | Lưu toàn bộ state | ✅ Đã có |
| S2 | Multiple save slots | Nhiều file save | ? |
| S3 | Auto-save | Tự lưu mỗi ngày game | ? |
| S4 | New Game+ | Chơi lại với bonus | ? |

---

## NHÓM 10: AI ASSISTANT (Mechanic đặc biệt)

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| AI1 | Gợi ý cây trồng | AI suggest cây phù hợp đất/mùa | ? |
| AI2 | Dự báo thời tiết | Biết trước ngày mai mưa/nắng | ? |
| AI3 | Tối ưu giá bán | Gợi ý khi nào bán tốt nhất | ? |
| AI4 | Nhắc nhở tưới/thu hoạch | Notification cây cần chăm | ? |
| AI5 | Auto-farming (late game) | AI điều khiển sprinkler/máy móc | ? |
| AI6 | Phân tích giống/nghiên cứu | Unlock cây mới qua research | ? |

---

## NHÓM 11: CỐT TRUYỆN & NARRATIVE

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| ST1 | Intro cinematic/text | Mở đầu game giới thiệu bối cảnh | ? |
| ST2 | Nhật ký nhân vật | Mỗi milestone → vài dòng suy nghĩ | ? |
| ST3 | Tin nhắn/điện thoại | Bạn bè, tin tức thế giới | ? |
| ST4 | Story events (scripted) | Sự kiện cốt truyện tại thời điểm nhất định | ? |
| ST5 | Multiple endings | Nhiều kết thúc tùy lựa chọn | ? |
| ST6 | Lore items | Tìm được tài liệu/ghi chú về sự kiện 2026-2029 | ? |
| ST7 | News broadcast | Đài phát thanh/TV báo tin tức thế giới | ? |

---

## NHÓM 12: UI & UX

| # | Feature | Mô tả ngắn | Quyết định |
|---|---------|-------------|-----------|
| U1 | HUD (stamina, time, money) | Thông tin cơ bản trên màn hình | ✅ Cần |
| U2 | Inventory UI | Grid, drag-drop, tooltip | ✅ Đã có |
| U3 | Shop UI | Mua/bán interface | ✅ Đã có |
| U4 | Quest log UI | Danh sách quest đang active | ✅ Đã có |
| U5 | Map/Minimap | Hiển thị vùng đã khám phá | ? |
| U6 | End-of-day summary | Tổng kết ngày: thu nhập, cây, mastery | ? |
| U7 | Crafting UI | Interface craft item | ✅ Đã có |
| U8 | AI Assistant panel | Góc màn hình hiển thị gợi ý | ? |
| U9 | Calendar UI | Xem lịch mùa, event sắp tới | ? |
| U10 | Tutorial/Onboarding | Hướng dẫn ngày đầu tiên | ? |

---

## CÁCH CHỐT

Với mỗi feature đánh dấu `?`, hãy chọn:
- **✅ GIỮ** — Cần cho MVP, làm ngay
- **❌ BỎ** — Không phù hợp game này, bỏ hẳn
- **⏳ SAU** — Hay nhưng không cần cho MVP, làm sau nếu có thời gian

Khi bạn chốt xong, tôi sẽ viết GDD chi tiết (từng item, từng con số) dựa trên danh sách feature đã confirm.
