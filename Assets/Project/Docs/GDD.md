# 🌾 GAME DESIGN DOCUMENT — FARM LIFE RPG

> **Thể loại:** Farm Simulation + Action RPG (2D Top-Down)  
> **Tham chiếu:** Stardew Valley × Moonlighter × Rune Factory  
> **Engine:** Unity 2022.3 | **Visual:** HeroEditor4D (Pixel Art 4-Direction)  
> **Trạng thái:** Prototype — đang xây dựng core loop

---

## 1. CONCEPT CỐT LÕI

### 1.1 Elevator Pitch
> *"Ban ngày bạn là nông dân — trồng trọt, chăn nuôi, xây dựng trang trại. Ban đêm bạn là chiến binh — khám phá hang động, chiến đấu quái vật, thu thập tài nguyên quý hiếm để nâng cấp trang trại và bản thân."*

### 1.2 Core Fantasy (Trải nghiệm cốt lõi)
Người chơi cảm thấy **mỗi ngày đều có ý nghĩa** — mỗi quyết định (trồng gì, đi đâu, nâng cấp gì) đều tạo ra tiến triển rõ ràng và mở ra khả năng mới.

### 1.3 Điểm thu hút người chơi (Hook)
| Hook | Mô tả |
|------|-------|
| **Vòng lặp ngày/đêm** | Ngày = farming an toàn, Đêm = combat nguy hiểm. Hai gameplay bổ trợ nhau. |
| **Tiến triển kép** | Trang trại phát triển + Nhân vật mạnh lên song song. |
| **Tự do lựa chọn** | Muốn focus farming? OK. Muốn focus combat? OK. Cả hai? Tối ưu nhất. |
| **Khám phá** | Mỗi tầng hang động = biome mới, quái mới, tài nguyên mới. |

---

## 2. GAME LOOP

### 2.1 Core Loop (Vòng lặp chính)
```
┌─────────────────────────────────────────────┐
│                  MỘT NGÀY                    │
│                                              │
│  🌅 SÁNG        → Chăm sóc trang trại       │
│  ☀️ TRƯA/CHIỀU  → Khám phá / Combat / NPC   │
│  🌙 TỐI         → Hang động (tùy chọn)      │
│  💤 NGỦ         → Kết thúc ngày              │
│                                              │
│  → Cây phát triển, Season thay đổi           │
│  → Mở khóa content mới theo tiến trình       │
└─────────────────────────────────────────────┘
```

### 2.2 Engagement Loop (Vòng lặp gắn kết)
```
Thu hoạch/Chiến đấu → Bán/Chế tạo → Nâng cấp công cụ/trang trại
    → Mở khóa khu vực mới → Tài nguyên mới → Craft mạnh hơn → ...
```

### 2.3 Session Flow (1 phiên chơi ~20-40 phút)
1. Thức dậy → check trang trại (tưới nước, thu hoạch) — 5 phút
2. Bán hàng / mua hạt giống / nói chuyện NPC — 3 phút
3. Khám phá hang động HOẶC mở rộng trang trại — 15-25 phút
4. Về nhà, ngủ, xem tổng kết ngày — 2 phút

---

## 3. HỆ THỐNG GAMEPLAY

### 3.1 Farming (Nông trại) ✅ Đã có cơ bản

| Feature | Trạng thái | Mô tả |
|---------|-----------|-------|
| Cuốc đất | ✅ Done | HoeRuntime → đổi tile thành plowed |
| Gieo hạt | ✅ Done | PlacementRuntime → spawn plant entity |
| Phát triển cây | ✅ Done | StageRuntime → NextDayEvent → đổi sprite |
| Thu hoạch | ✅ Done | HarvestRuntime + check canHarvest stage |
| Tưới nước | ❌ Chưa có | Cần WateringCanRuntime → đổi tile plowed → watered |
| Mùa vụ (Season) | ❌ Chưa có | 4 mùa, mỗi mùa 28 ngày, cây chỉ trồng đúng mùa |
| Chất lượng nông sản | ❌ Chưa có | Sao (1-3★) dựa trên tưới đều + phân bón |

**Cây trồng hiện có:** Potato, Onion, Greenonions, Strawberry

**Cần thêm:**
- [ ] WateringCanRuntime (tưới nước → tăng tốc phát triển)
- [ ] SeasonSystem (quản lý mùa, restrict cây theo mùa)
- [ ] Thêm 4-6 loại cây mỗi mùa

### 3.2 Combat (Chiến đấu) ⚠️ Có framework, chưa có gameplay

| Feature | Trạng thái | Mô tả |
|---------|-----------|-------|
| Vung vũ khí | ✅ Done | ScytheRuntime → damage entities trong range |
| Nhận damage | ✅ Done | HealthRuntime → trừ HP → DieEvent |
| Drop loot | ✅ Done | DropRuntime → spawn EntityDrop |
| Enemy AI | ❌ Chưa có | Cần patrol, chase, attack pattern |
| Dungeon/Hang động | ❌ Chưa có | Cần scene riêng, tầng procedural |
| Vũ khí đa dạng | ❌ Chưa có | Kiếm, cung, phép... |

**Cần thêm:**
- [ ] EnemyAI (state machine: Idle → Patrol → Chase → Attack → Flee)
- [ ] AttackRuntime cho enemy (đã có AttackModule, cần implement)
- [ ] 3-5 loại enemy cơ bản (Slime, Skeleton, Bat, Goblin, Boss)
- [ ] Dungeon scene + DungeonManager

### 3.3 Inventory & Equipment ✅ Đã có

| Feature | Trạng thái |
|---------|-----------|
| Hotbar (10 slot) | ✅ Done |
| Backpack | ✅ Done |
| Equipment slots | ✅ Done |
| Drag & Drop | ✅ Done |
| Stack / Split | ✅ Done |
| Visual equipment (HeroEditor4D) | ✅ Done (AppearanceModule) |

### 3.4 NPC & Social ❌ Chưa có

| Feature | Mô tả | Ưu tiên |
|---------|-------|---------|
| NPC cơ bản | Đứng tại chỗ, có dialogue | 🔴 Cao |
| Shop | Mua/bán item | 🔴 Cao |
| Quest đơn giản | "Mang cho tôi 5 khoai tây" | 🟡 TB |
| Friendship | Tặng quà → tăng thiện cảm | 🟢 Thấp |

### 3.5 Crafting & Upgrade ❌ Chưa có

| Feature | Mô tả | Ưu tiên |
|---------|-------|---------|
| Nâng cấp công cụ | Cuốc gỗ → sắt → vàng (tăng range, speed) | 🔴 Cao |
| Chế tạo đồ ăn | Nông sản → món ăn (hồi HP/Energy) | 🟡 TB |
| Chế tạo vật phẩm | Tài nguyên → đồ trang trí, máy móc | 🟢 Thấp |

### 3.6 Time & Calendar ❌ Chưa có

| Feature | Mô tả |
|---------|-------|
| Đồng hồ trong game | 1 ngày = 15-20 phút thực |
| Lịch | 4 mùa × 28 ngày = 112 ngày/năm |
| Sự kiện mùa | Lễ hội, boss đặc biệt, cây hiếm |

---

## 4. CONTENT ROADMAP

### Phase 1: Core Loop Hoàn Chỉnh (Ưu tiên cao nhất)
> Mục tiêu: Người chơi có thể chơi 1 ngày hoàn chỉnh và muốn chơi ngày tiếp theo.

- [x] **Hệ thống thời gian** — TimeManager (đồng hồ, ngày/đêm)
- [ ] **NPC Shop** — mua hạt giống, bán nông sản
- [ ] **Energy system** — mỗi hành động tốn energy, ngủ hồi energy
- [ ] **UI thời gian** — hiển thị giờ, ngày, mùa trên HUD
- [ ] **Kết thúc ngày** — màn hình tổng kết (thu nhập, cây phát triển)

### Phase 2: Combat & Exploration
> Mục tiêu: Người chơi có lý do rời trang trại.

- [ ] **Enemy cơ bản** — 3 loại enemy với AI đơn giản
- [ ] **Khu vực ngoài trang trại** — rừng, mỏ đá
- [ ] **Drop tài nguyên** — gỗ, đá, quặng từ enemy/object
- [ ] **Nâng cấp công cụ** — dùng tài nguyên để upgrade

### Phase 3: Depth & Variety
> Mục tiêu: Giữ chân người chơi lâu dài.

- [ ] **Season system** — 4 mùa, cây theo mùa
- [ ] **Thêm cây trồng** — 4-6 cây mỗi mùa
- [ ] **Crafting** — chế tạo đồ ăn, vật phẩm
- [ ] **NPC quest** — nhiệm vụ đơn giản
- [ ] **Dungeon** — hang động nhiều tầng

### Phase 4: Polish & Feel
> Mục tiêu: Game cảm thấy hoàn chỉnh.

- [ ] **Âm thanh** — SFX hành động, nhạc nền theo mùa
- [ ] **VFX** — particle tưới nước, thu hoạch, level up
- [ ] **Tutorial** — hướng dẫn ngày đầu tiên
- [ ] **Save/Load hoàn chỉnh** — tất cả state được lưu

---

## 5. KIẾN TRÚC KỸ THUẬT HIỆN TẠI

### 5.1 Entity-Module System (ECS-like)
```
EntityData (ScriptableObject) — config tĩnh
    └── modules: List<IModuleData>
            ├── ToolModule
            ├── PlacementModule
            ├── HealthModule
            ├── HarvestModule
            ├── StageModule
            ├── DropModule
            ├── MortalModule
            ├── EquipmentModule
            ├── InventoryModule
            ├── ActionModule
            ├── AppearanceModule
            ├── AttackModule
            └── RespawnModule

EntityRuntime — instance runtime
    └── modules: List<IModuleRuntime>
            └── Mỗi module handle events riêng (IHandleEvent<T>)
```

### 5.2 Event Flow
```
Input → PlayerControler
    → PrimaryActionEvent (lên Player entity)
        → ActionRuntime (tìm item đang cầm, forward)
            → ToolRuntime.Validate() → ToolActionBridge.Request()
                → Animation play → AnimStrikeEvent
                    → ToolRuntime.Execute() (logic thực thi)
```

### 5.3 Spawn Flow
```
PlacementRuntime.Validate() → bridge.Request() → AnimStrikeEvent
    → SpawnRequestPublish → SpawnSystem
        → PlacementValidator.CanPlace() → Instantiate prefab
            → EntityRoot.Add() → SpawnedEvent → modules init
```

---

## 6. VIỆC CẦN LÀM NGAY (Tuần này)

| # | Task | Lý do | Estimate |
|---|------|-------|----------|
| 1 | **TimeManager** (đồng hồ + ngày/đêm) | Nền tảng cho mọi hệ thống khác | 1-2 ngày |
| 2 | **WateringCanRuntime** | Hoàn thiện farming loop | 0.5 ngày |
| 3 | **Energy system** (stat Energy, trừ khi action) | Tạo tension/quyết định cho người chơi | 0.5 ngày |
| 4 | **NPC Shop đơn giản** | Người chơi cần mua hạt, bán nông sản | 1-2 ngày |
| 5 | **UI thời gian + Energy** | Người chơi cần thấy thông tin | 0.5 ngày |
| 6 | **Sleep/End day** | Kết thúc vòng lặp 1 ngày | 0.5 ngày |

**Sau khi hoàn thành 6 task trên → bạn có 1 core loop hoàn chỉnh để test và iterate.**

---

## 7. NGUYÊN TẮC THIẾT KẾ

1. **Mỗi hành động phải có feedback rõ ràng** — animation, sound, particle, UI change
2. **Không bao giờ để người chơi không biết làm gì** — luôn có gợi ý/quest
3. **Tiến triển phải visible** — trang trại thay đổi, nhân vật mạnh lên, NPC phản ứng
4. **Respect thời gian người chơi** — 20 phút chơi = phải có tiến triển đáng kể
5. **Farming và Combat bổ trợ nhau** — farming cung cấp đồ ăn/tiền cho combat, combat cung cấp tài nguyên cho farming
