# ⚔️ COMBAT SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** CombatSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Đánh bằng tool
> 2. Đánh bằng weapon
> 3. Enemy AI
> 4. Enemy drop nguyên liệu
> 5. Vùng khai hoang
> 6. Player nhận damage
> 7. Player chết
> 8. Dodge

---

## INPUT ROUTING (áp dụng cho toàn bộ CombatSystem)

Combat dùng PrimaryAction (chuột trái) cho tấn công:

```
Player nhấn Chuột Trái (cầm weapon/tool)
    → PrimaryActionEvent → ActionRuntime forward → item entity
        → WeaponRuntime.Handle() hoặc DamageToolRuntime.Handle()
            → Validate() → ToolActionBridge.Request() → animation
            → AnimStrikeEvent → Execute() → TakeDamageEvent tới target

Dodge dùng Shift:
    → PlayerControler.TryStartDodge() → DodgeRoutine (invincibility frames)
```

---

## 1. ĐÁNH BẰNG TOOL

### Mô tả
Người chơi cầm tool (Pickaxe, Axe, Scythe) trên hotbar, nhấn chuột trái. Tool gây damage lên entity gần nhất trong range (enemy, rock, tree). Dùng DamageToolRuntime đã có.

### Điều kiện
- Player đang cầm item có ToolModule (toolType = Pickaxe/Axe/Scythe)
- Có target entity trong range (EntityScanSystem)
- Player có đủ stamina (≥ tool stamina cost)
- ToolActionBridge không busy

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → DamageToolRuntime.Handle()
    → DamageToolRuntime.Validate():
        → return true (always valid, miss = no target in range)
    → ToolActionBridge.Request(actor, item, animTrigger) → play animation
    → AnimStrikeEvent → DamageToolRuntime.Execute():
        1. float range = item.stats.Get(Range) ?? defaultRange
        2. float damage = item.stats.Get(Attack) ?? defaultDamage
        3. List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range)
        4. if hitAllTargets: foreach target → ApplyDamage()
           else: FindNearest() → ApplyDamage()
        5. ApplyDamage(target, actor, damage):
            → target.TriggerEvent(new TakeDamageEvent(actor, damage, toolType))
        6. Trừ stamina (tool cost)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| DamageToolRuntime | Đã có — shared cho Pickaxe/Axe/Scythe |
| EntityScanSystem | Tìm targets trong range |
| ToolActionBridge | Animation + AnimStrikeEvent |
| HealthRuntime (target) | Nhận TakeDamageEvent, trừ HP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Pickaxe EntityData | ScriptableObject | ToolModule(toolType=Pickaxe), stats(Attack=2, Range=1.5) |
| Axe EntityData | ScriptableObject | ToolModule(toolType=Axe), stats(Attack=2, Range=1.5) |

### UI
- Damage number popup trên target (optional)
- Stamina bar giảm

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → DamageToolRuntime |
| AnimStrikeEvent | ToolActionBridge | DamageToolRuntime |
| TakeDamageEvent | DamageToolRuntime | HealthRuntime (target) |

### Scene Objects
- Player prefab (ToolActionBridge)
- Enemy/Resource prefabs (HealthRuntime)

---

## 2. ĐÁNH BẰNG WEAPON

### Mô tả
Người chơi cầm weapon (Sword, Spear) trên hotbar, nhấn chuột trái. Weapon có animation riêng, damage cao hơn tool, có thể hit multiple enemies. Dùng WeaponRuntime (extends ToolRuntime pattern).

### Điều kiện
- Player đang cầm item có WeaponModule
- ToolActionBridge không busy
- Player có đủ stamina (≥ weapon stamina cost)

### Flow
```
Chuột Trái → PrimaryActionEvent → ActionRuntime forward → WeaponRuntime.Handle()
    → WeaponRuntime.Validate():
        1. Check stamina >= weaponCost
        → return true
    → ToolActionBridge.Request(actor, item, weapon.animTrigger) → play attack animation
    → AnimStrikeEvent → WeaponRuntime.Execute():
        1. float range = item.stats.Get(Range)
        2. float damage = item.stats.Get(Attack)
        3. List<EntityRuntime> targets = EntityScanSystem.GetAll(actorGO, range)
        4. Filter: chỉ lấy targets có tag Enemy hoặc Destructible
        5. foreach target in range:
            → target.TriggerEvent(new TakeDamageEvent(actor, damage, ToolType.Weapon))
        6. Trừ stamina
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| WeaponRuntime 🆕 | Extends ToolRuntime, weapon-specific logic |
| EntityScanSystem | Tìm enemies trong range |
| ToolActionBridge | Animation |
| HealthRuntime (target) | Nhận damage |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| IronSword EntityData | ScriptableObject | WeaponModule(animTrigger="Slash"), stats(Attack=8, Range=1.2, StaminaCost=5) |
| Spear EntityData | ScriptableObject | WeaponModule(animTrigger="Thrust"), stats(Attack=6, Range=2.0, StaminaCost=4) |

### UI
- Attack animation (weapon sprite swing)
- Damage numbers trên enemies

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PrimaryActionEvent | PlayerControler | ActionRuntime → WeaponRuntime |
| AnimStrikeEvent | ToolActionBridge | WeaponRuntime |
| TakeDamageEvent | WeaponRuntime | HealthRuntime (enemies) |

### Scene Objects
- Player prefab
- Enemy prefabs

---

## 3. ENEMY AI

### Mô tả
Enemy có state machine đơn giản: Idle → Chase → Attack → Cooldown. EnemyObject (MonoBehaviour) đã có sẵn, điều khiển movement + attack pattern. Khi player vào aggro range → chase. Khi trong attack range → attack → cooldown → lặp lại.

### Điều kiện
- Enemy entity tồn tại trong scene
- Player trong aggro range → trigger chase
- Player trong attack range → trigger attack

### Flow
```
EnemyObject.Update() — State Machine:
    
    IDLE:
        - Patrol random hoặc đứng yên
        - Check: Vector2.Distance(player, self) <= aggroRange?
            → YES: SetState(Chase)
    
    CHASE:
        - MoveTowards(player.position, moveSpeed)
        - Check: distance <= attackRange?
            → YES: SetState(Attack)
        - Check: distance > aggroRange * 1.5?
            → YES: SetState(Idle) (mất aggro)
    
    ATTACK:
        - Play attack animation
        - AnimStrikeEvent → EnemyAttackRuntime.Execute():
            → player.TriggerEvent(new TakeDamageEvent(enemy, attackDamage, ToolType.None))
        - SetState(Cooldown)
    
    COOLDOWN:
        - Wait attackCooldown seconds
        - SetState(Chase)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| EnemyObject | MonoBehaviour — AI state machine |
| EnemyAttackRuntime | Damage logic |
| HealthRuntime (player) | Nhận damage |
| EntityScanSystem | Distance check |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Slime EntityData | ScriptableObject | EnemyModule(aggroRange=4, attackRange=1, attackCooldown=2), stats(HP=10, Attack=3, MoveSpeed=2) |
| Goblin EntityData | ScriptableObject | EnemyModule(aggroRange=5, attackRange=1.5, attackCooldown=1.5), stats(HP=25, Attack=6, MoveSpeed=3) |

### UI
- Enemy HP bar (hiện khi bị hit, ẩn khi full HP)
- Aggro indicator (optional: ! icon)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| TakeDamageEvent | EnemyAttackRuntime | HealthRuntime (player) |
| DieEvent | HealthRuntime (enemy) | DropRuntime, MortalRuntime |

### Scene Objects
- Enemy prefabs (EnemyObject, EntityRoot, HealthRuntime, Collider2D)
- Spawn points / spawn zones

---

## 4. ENEMY DROP NGUYÊN LIỆU

### Mô tả
Khi enemy chết (HP <= 0 → DieEvent), DropRuntime spawn drop items tại vị trí enemy. Drops = nguyên liệu craft (slime_drop, goblin_ear, etc.) + gold + EXP.

### Điều kiện
- Enemy HP <= 0
- DieEvent triggered
- DropModule có drops[] configured

### Flow
```
Enemy HP <= 0 → HealthRuntime → DieEvent
    → DropRuntime.Handle(DieEvent):
        1. Lấy drops[] từ DropModule
        2. Foreach drop:
            - Random chance check (drop.chance)
            - Random quantity (drop.minAmount ~ drop.maxAmount)
            - EventBus.Publish(SpawnRequestPublish(position, DropItem, dropEntityData))
        3. SpawnSystem → Instantiate drop items tại enemy position
        4. DropMotionObject → scatter animation (nhỏ, random offset)
    → ExpRewardRuntime.Handle(DieEvent):
        - ProgressionService.GrantMasteryExp(killer, expAmount, CombatSource)
    → MortalRuntime.Handle(DieEvent):
        - DestroyEntityRequestPublish → enemy biến mất
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| DropRuntime | Spawn drops |
| SpawnSystem | Instantiate drop items |
| ExpRewardRuntime | Grant EXP |
| MortalRuntime | Destroy enemy |
| ProgressionService | Nhận EXP |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Slime EntityData | ScriptableObject | DropModule(drops[{slime_drop, chance=80%, 1-2}, {gold, chance=50%, 5-15}]), ExpRewardModule(exp=5) |
| SlimeDrop EntityData | ScriptableObject | sellPrice=10, category=MonsterDrop |

### UI
- Drop items hiện trên ground (DropMotionObject)
- "+5 Mastery" popup
- Gold pickup notification

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DieEvent | HealthRuntime | DropRuntime, ExpRewardRuntime, MortalRuntime |
| SpawnRequestPublish | DropRuntime | SpawnSystem |
| DestroyEntityRequestPublish | MortalRuntime | SpawnSystem |

### Scene Objects
- Drop item prefabs (EntityRoot, SpriteRenderer, PickupTrigger)
- DropMotionObject (scatter animation component)

---

## 5. VÙNG KHAI HOANG

### Mô tả
Một vùng đất bị khóa (blocked zone) chứa enemies + obstacles (rocks, stumps). Khi player clear hết tất cả enemies và obstacles trong zone → zone mở ra (unlock new farmland/area). Tracked bằng ClearZoneTracker.

### Điều kiện
- ClearZone entity tồn tại trong scene
- Zone chứa N enemies + M obstacles
- Tất cả đều bị destroy → zone cleared

### Flow
```
ClearZoneTracker (MonoBehaviour trên zone object):
    - Init: scan children/registered entities → count total targets
    - Listen: DestroyEntityRequestPublish → check if target belongs to this zone
        → remainingCount--
        → if remainingCount <= 0:
            → ZoneClearedEvent(zoneId)
            → Disable barrier collider/visual
            → EventBus.Publish(ZoneClearedPublish(zoneId))
            → ProgressionService.GrantMasteryExp(player, zoneExp, ClearSource)
            → Unlock adjacent farmland tiles

Player kills enemies / destroys obstacles in zone:
    → DieEvent → MortalRuntime → DestroyEntityRequestPublish
    → ClearZoneTracker nhận → decrement count
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| ClearZoneTracker 🆕 | Count targets, detect cleared |
| MortalRuntime | Destroy targets |
| ProgressionService | Grant EXP khi clear |
| WorldEntityService | Unlock tiles |
| QuestService | Quest "clear zone X" completion |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| ClearZone Config | ScriptableObject | zoneId, totalTargets, expReward, unlockedTiles[] |

### UI
- Zone progress indicator: "Khai hoang: 3/8 đã dọn"
- Notification khi clear: "Đã khai hoang vùng đất mới!"
- Barrier visual (fence/rocks) biến mất khi cleared

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DestroyEntityRequestPublish | MortalRuntime | ClearZoneTracker |
| ZoneClearedPublish 🆕 | ClearZoneTracker | ProgressionService, QuestService, WorldEntityService |

### Scene Objects
- ClearZone object (Collider2D trigger, ClearZoneTracker)
- Barrier visuals (sprites/tilemaps)
- Enemies + obstacles spawned inside zone

---

## 6. PLAYER NHẬN DAMAGE

### Mô tả
Player có HealthRuntime. Khi nhận TakeDamageEvent (từ enemy attack, trap, etc.), HP giảm. Visual feedback: flash đỏ, knockback nhẹ. Nếu HP <= 0 → PlayerDeathHandler.

### Điều kiện
- Player entity có HealthRuntime
- TakeDamageEvent received
- Player không đang trong invincibility frames (sau dodge hoặc sau bị hit)

### Flow
```
Enemy attack / Trap → TakeDamageEvent(attacker, damage, type) tới player entity
    → HealthRuntime.Handle(TakeDamageEvent):
        1. if isInvincible → return (i-frames active)
        2. float finalDamage = damage - defense (from equipment)
        3. finalDamage = Mathf.Max(1, finalDamage) (minimum 1)
        4. currentHP -= finalDamage
        5. Set isInvincible = true, start i-frame timer (0.5s)
        6. EventBus.Publish(PlayerDamagedPublish(currentHP, maxHP, damage))
        7. Visual: flash red, small knockback
        8. if currentHP <= 0:
            → entity.TriggerEvent(new DieEvent(entity))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| HealthRuntime | HP tracking, i-frames |
| EquipmentRuntime | Defense stat |
| PlayerDamageVisual 🆕 | Flash + knockback |
| PlayerDeathHandler | Handle HP <= 0 |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| Player EntityData | ScriptableObject | HealthModule(baseHP=100), stats(Defense from equipment) |

### UI
- HP bar (HUD, top-left)
- Damage number popup trên player
- Screen flash đỏ nhẹ khi bị hit
- HP bar flash khi low HP (< 25%)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| TakeDamageEvent | EnemyAttackRuntime | HealthRuntime (player) |
| PlayerDamagedPublish 🆕 | HealthRuntime | HUD (update HP bar), PlayerDamageVisual |
| DieEvent | HealthRuntime | PlayerDeathHandler |

### Scene Objects
- Player prefab (HealthRuntime, PlayerDamageVisual)
- HP bar UI (HUD)

---

## 7. PLAYER CHẾT

### Mô tả
Khi player HP <= 0, PlayerDeathHandler xử lý: fade to black → respawn tại nhà → mất 50% stamina hiện tại → skip tới ngày hôm sau. Không mất items (casual farming game).

### Điều kiện
- Player HP <= 0
- DieEvent triggered trên player entity

### Flow
```
Player HP <= 0 → DieEvent → PlayerDeathHandler.Handle(DieEvent):
    1. PlayerControler.InputEnabled = false (disable input)
    2. Fade to black (UI transition)
    3. Wait 1s
    4. Respawn:
        - player.transform.position = homeSpawnPoint
        - HealthRuntime.SetHP(maxHP) → full heal
        - Stamina = currentStamina * 0.5 (mất 50%)
    5. TimeManager.SkipToNextDay() → DayChangedPublish
    6. Fade in
    7. PlayerControler.InputEnabled = true
    8. Show message: "Bạn đã kiệt sức và được đưa về nhà..."
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerDeathHandler 🆕 | Orchestrate death sequence |
| TimeManager | SkipToNextDay() |
| HealthRuntime | Reset HP |
| PlayerControler | Disable/enable input |
| SceneTransitionService | Nếu chết ở mine scene → load farm scene |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| GameConfig | ScriptableObject | deathStaminaPenalty=0.5, homeSpawnPoint |

### UI
- Fade to black overlay
- Message box: "Bạn đã kiệt sức..."
- Day summary (nếu có)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DieEvent | HealthRuntime | PlayerDeathHandler |
| DayChangedPublish | TimeManager | All day-based systems |
| PlayerRespawnedPublish 🆕 | PlayerDeathHandler | HUD (reset bars) |

### Scene Objects
- Home spawn point (Transform marker)
- Fade overlay (UI Canvas)

---

## 8. DODGE

### Mô tả
Player nhấn Shift → dash nhanh theo hướng đang di chuyển. Tốn stamina, có invincibility frames trong suốt dodge. Đã implement trong PlayerControler.

### Điều kiện
- Player nhấn Shift (dodgeKey)
- Player có đủ stamina (≥ dodgeStaminaCost = 12)
- Player không đang dodge hoặc action busy
- Có input direction (hoặc dùng lastMoveDirection)

### Flow
```
Shift → PlayerControler.TryStartDodge():
    1. Check !isDodging && !IsActionBusy
    2. TrySpendStamina(entity, dodgeStaminaCost=12) → trừ stamina
    3. ReadInputDirection() → direction (hoặc lastMoveDirection)
    4. StartCoroutine(DodgeRoutine(direction)):
        - isDodging = true
        - Set isInvincible = true trên HealthRuntime
        - Lerp position: start → start + direction * dodgeDistance (1.25f)
        - Duration: dodgeDuration (0.16s)
        - isDodging = false
        - Set isInvincible = false (sau short delay)
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| PlayerControler | Đã có — dodge logic |
| HealthRuntime | i-frames flag |
| EntityRuntime.stats | Stamina check/spend |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| PlayerControler (serialized) | MonoBehaviour | dodgeDistance=1.25, dodgeDuration=0.16, dodgeStaminaCost=12 |

### UI
- Stamina bar giảm
- Dodge visual: player sprite blur/trail (optional)

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| (Không có event riêng — logic nội bộ PlayerControler) | | |

### Scene Objects
- Player prefab (PlayerControler đã có dodge)

---

## TỔNG KẾT: COMBAT SYSTEM DEPENDENCY

```
PlayerControler (Input)
    │
    ├── Chuột Trái → PrimaryActionEvent
    │       │
    │       ▼
    │   ActionRuntime → forward to held item
    │       │
    │       ├── DamageToolRuntime → TakeDamageEvent → target HealthRuntime
    │       └── WeaponRuntime → TakeDamageEvent → target HealthRuntime
    │
    ├── Shift → TryStartDodge() → DodgeRoutine (i-frames)
    │
    └── (Nhận damage từ enemies)
            │
            ▼
        HealthRuntime (player)
            │
            ├── HP > 0: PlayerDamagedPublish → HUD, visual feedback
            └── HP <= 0: DieEvent → PlayerDeathHandler
                                        │
                                        ▼
                                    Respawn + SkipDay + StaminaPenalty

EnemyObject (AI State Machine)
    │
    ├── Idle → Chase → Attack → Cooldown → Chase...
    │
    ├── Attack → TakeDamageEvent → player HealthRuntime
    │
    └── Nhận damage → HP <= 0 → DieEvent
                                    │
                                    ├── DropRuntime → spawn drops
                                    ├── ExpRewardRuntime → Mastery EXP
                                    └── MortalRuntime → destroy

ClearZoneTracker
    │
    ├── Count enemies + obstacles in zone
    ├── Listen DestroyEntityRequestPublish → decrement
    └── All cleared → ZoneClearedPublish → unlock land + EXP
```
