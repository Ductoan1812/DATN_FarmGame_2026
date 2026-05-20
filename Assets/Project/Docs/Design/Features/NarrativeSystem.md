# 📖 NARRATIVE SYSTEM — Thiết kế chức năng chi tiết

> **Hệ thống lớn:** NarrativeSystem
> **Chức năng nhỏ thuộc hệ thống này:**
> 1. Story events
> 2. Nhật ký (Diary)
> 3. Tin nhắn (Messages)
> 4. News broadcast
> 5. Lore items
> 6. Phase transition

---

## TỔNG QUAN

NarrativeSystem kể câu chuyện qua nhiều kênh nhỏ (diary, messages, news, lore items) thay vì cutscenes dài. Mỗi ngày mới, NarrativeService check conditions → trigger events phù hợp. Narrative không block gameplay — chỉ thông báo/popup ngắn.

---

## 1. STORY EVENTS

### Mô tả
NarrativeService check conditions mỗi đầu ngày mới. Nếu điều kiện thỏa (ngày X, quest Y hoàn thành, mastery Z, etc.) → trigger story event (dialogue, unlock, notification). Events chỉ trigger 1 lần.

### Điều kiện
- Ngày mới bắt đầu (DayChangedPublish)
- NarrativeService check all pending story events
- Event conditions met (day, quest, mastery, items, etc.)
- Event chưa triggered trước đó

### Flow
```
TimeManager → DayChangedPublish → NarrativeService.OnNewDay():
    1. foreach storyEvent in pendingEvents:
        - Check conditions:
            - storyEvent.dayRequirement <= currentDay?
            - storyEvent.questRequired == completed?
            - storyEvent.masteryRequired <= currentMastery?
            - storyEvent.itemRequired in inventory?
        - if ALL conditions met:
            → MarkTriggered(storyEvent.id)
            → Execute storyEvent.actions[]:
                - ShowDialogue(dialogueGraphId)
                - ShowDiary(diaryText)
                - SendMessage(messageData)
                - UnlockFeature(featureId)
                - SpawnEntity(entityData, position)
            → EventBus.Publish(StoryEventTriggeredPublish(storyEvent.id))
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| NarrativeService 🆕 | Check conditions, trigger events |
| TimeManager | DayChangedPublish |
| QuestService | Check quest completion |
| ProgressionService | Check mastery level |
| InventoryService | Check items |
| DialogueSystem | Show dialogue |
| SaveSystem | Persist triggered events |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| StoryEventData | ScriptableObject | id, conditions{day, quest, mastery, item}, actions[], priority |
| StoryEvent_Day7News | ScriptableObject | day=7, actions=[SendMessage("mutation_news")] |
| StoryEvent_Day10Mutant | ScriptableObject | day=10, actions=[SpawnEntity("first_mutant"), ShowDiary("mutant_appears")] |

### UI
- Dialogue popup (existing DialogueSystem)
- Notification: "Có sự kiện mới!"

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DayChangedPublish | TimeManager | NarrativeService |
| StoryEventTriggeredPublish 🆕 | NarrativeService | QuestService, FeatureFlagService |

### Scene Objects
- NarrativeService (singleton)
- Spawned entities (from story events)

---

## 2. NHẬT KÝ (DIARY)

### Mô tả
DiaryUI hiện popup ngắn (2-3 dòng) tại các milestones quan trọng. Player đọc suy nghĩ của nhân vật. Không block gameplay — popup tự fade sau 5s hoặc player dismiss.

### Điều kiện
- Story event trigger ShowDiary action
- Hoặc milestone reached (first harvest, first craft, first kill, etc.)

### Flow
```
NarrativeService → ShowDiary(diaryText):
    → EventBus.Publish(DiaryEntryPublish(diaryText, timestamp))
    → DiaryUI.Handle(DiaryEntryPublish):
        1. Show popup panel (bottom-center, semi-transparent)
        2. Display text (2-3 lines, Vietnamese)
        3. Auto-fade after 5 seconds
        4. Player can click to dismiss early
        5. Entry saved to diary log (viewable later)

Milestone triggers (automatic):
    - First harvest: "Vụ mùa đầu tiên... Cảm giác thật tuyệt."
    - First sell: "Kiếm được tiền đầu tiên từ nông trại!"
    - First enemy kill: "Sinh vật lạ... Phải cẩn thận hơn."
    - Mastery 10: "Đã quen với cuộc sống ở đây rồi."
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| NarrativeService | Trigger diary entries |
| DiaryUI 🆕 | Display popup |
| SaveSystem | Persist diary log |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| DiaryEntryData | ScriptableObject | id, text (Vietnamese), triggerCondition |
| DiaryLog | SaveData | list of triggered entries with timestamps |

### UI
- DiaryUI popup: small panel, 2-3 lines text, fade animation
- Diary log screen (accessible from menu): list all past entries

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| DiaryEntryPublish 🆕 | NarrativeService | DiaryUI |

### Scene Objects
- DiaryUI (Canvas, bottom-center popup)

---

## 3. TIN NHẮN (MESSAGES)

### Mô tả
MessageNotificationUI hiện thông báo "tin nhắn mới" từ NPC friends, shop owners, quest givers. Player mở phone/mailbox để đọc. Messages chứa hints, quest hooks, world-building.

### Điều kiện
- Story event trigger SendMessage action
- Hoặc NPC gửi message (friendship milestone, quest available, etc.)

### Flow
```
NarrativeService → SendMessage(messageData):
    → MessageService.AddMessage(messageData)
    → EventBus.Publish(NewMessagePublish(messageData.sender, messageData.preview))
    → MessageNotificationUI.Handle(NewMessagePublish):
        1. Show notification icon (phone icon + badge count)
        2. Play notification sound
    
Player mở Messages (từ menu hoặc interact mailbox):
    → MessageListUI:
        1. Display all messages (newest first)
        2. Unread = bold, Read = normal
        3. Player tap message → full text
        4. Mark as read
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| MessageService 🆕 | Store/manage messages |
| NarrativeService | Trigger messages |
| SaveSystem | Persist messages |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| MessageData | ScriptableObject | sender, subject, body (Vietnamese), day sent |
| Message_Day3_ShopOpen | | sender="Cô Mai", body="Cửa hàng đã mở! Ghé mua hạt giống nhé." |

### UI
- Notification icon (HUD, top-right): phone + unread count badge
- MessageListUI panel: sender, subject, preview
- MessageDetailUI: full message text

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| NewMessagePublish 🆕 | MessageService | MessageNotificationUI |

### Scene Objects
- MessageNotificationUI (HUD icon)
- MessageListUI (menu panel)
- Mailbox entity (optional physical interact point)

---

## 4. NEWS BROADCAST

### Mô tả
NewsBroadcastUI hiện text overlay ngắn về world events (thời sự game world). Dùng để foreshadow story beats, build tension. Hiện như ticker/banner ở top screen, auto-scroll.

### Điều kiện
- Story event trigger ShowNews action
- Specific days (Day 7: mutation news, Day 14: more reports, etc.)

### Flow
```
NarrativeService → ShowNews(newsText):
    → EventBus.Publish(NewsBroadcastPublish(newsText, priority))
    → NewsBroadcastUI.Handle(NewsBroadcastPublish):
        1. Queue news text
        2. Show banner at top of screen
        3. Text scrolls/types in (typewriter effect)
        4. Display for 8 seconds
        5. Fade out
        6. If multiple queued → show next after 2s delay

News examples:
    Day 7: "TIN TỨC: Phát hiện sinh vật lạ ở vùng rừng phía Bắc..."
    Day 10: "KHẨN CẤP: Cư dân báo cáo sinh vật đột biến tấn công nông trại..."
    Day 14: "Chính quyền khuyến cáo trang bị vũ khí khi ra ngoài."
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| NarrativeService | Trigger news |
| NewsBroadcastUI 🆕 | Display banner |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| NewsData | ScriptableObject | text (Vietnamese), day, priority |

### UI
- NewsBroadcastUI: banner top-screen, dark background, white text
- Typewriter text animation
- Auto-dismiss after 8s

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| NewsBroadcastPublish 🆕 | NarrativeService | NewsBroadcastUI |

### Scene Objects
- NewsBroadcastUI (Canvas, top banner)

---

## 5. LORE ITEMS

### Mô tả
Items tìm được trong cleared zones (notes, journals, artifacts). Khi pickup, player có thể đọc qua dialogue system. Chứa backstory về mutation, world history, NPC backgrounds.

### Điều kiện
- Lore item entity tồn tại trong cleared zone
- Player interact (E/chuột phải) → pickup + read option

### Flow
```
E / Chuột Phải → SecondaryActionEvent → ActionRuntime
    → Forward sang LoreItem entity
    → LoreItemRuntime.Handle(SecondaryActionEvent):
        → context.AddOption("read", "Đọc")
        → context.AddOption("pickup", "Nhặt")
        
    Player chọn "Đọc":
        → DialogueService.ShowDialogue(loreDialogueGraph)
        → Hiện text content (1-3 paragraphs)
        
    Player chọn "Nhặt":
        → InventoryService.TryAdd(player, loreItemData, 1)
        → entity.TriggerEvent(new DieEvent()) → destroy from world
        → LoreRegistry.MarkFound(loreId)
        → Player có thể đọc lại từ inventory
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| LoreItemRuntime 🆕 | Handle interact |
| DialogueSystem | Show lore text |
| InventoryService | Pickup |
| LoreRegistry 🆕 | Track found lore |
| SaveSystem | Persist found lore |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| LoreItem EntityData | ScriptableObject | LoreItemModule(loreId, dialogueGraphId), category=Lore |
| LoreDialogueGraph | DialogueGraphData | Text content (Vietnamese backstory) |
| Lore_MutationOrigin | | "Nhật ký nghiên cứu: Thí nghiệm #47 đã thất bại..." |

### UI
- Dialogue popup (existing system) for reading
- Lore collection screen (menu): list all found lore items

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| SecondaryActionEvent | ActionRuntime | LoreItemRuntime |
| LoreFoundPublish 🆕 | LoreItemRuntime | LoreRegistry, DiaryUI (optional reaction) |

### Scene Objects
- Lore item prefabs (EntityRoot, SpriteRenderer, Collider2D) — placed in cleared zones
- Dialogue UI (existing)

---

## 6. PHASE TRANSITION

### Mô tả
Game narrative chia phases. Phase transition xảy ra khi conditions met (specific day + quest + mastery). Mỗi phase mở content mới (enemies, areas, mechanics). Transition = news + diary + spawn changes.

### Điều kiện
- Phase conditions met (checked by NarrativeService)
- Previous phase completed

### Flow
```
NarrativeService.OnNewDay() → CheckPhaseTransition():
    
    Phase 1: "Peaceful Farming" (Day 1-6)
        - Chỉ farming, shop, basic quests
        - Không có enemies
        
    Phase 2: "First Signs" (Day 7-9)
        - Trigger: Day 7
        - Actions:
            → ShowNews("Sinh vật lạ phát hiện...")
            → SendMessage(from: "Trưởng thôn", "Cẩn thận khi ra ngoài")
            → ShowDiary("Có gì đó không ổn...")
        
    Phase 3: "Combat Unlocks" (Day 10+)
        - Trigger: Day 10
        - Actions:
            → SpawnEntity("first_mutant", farmEdgePosition)
            → ShowNews("KHẨN CẤP: Sinh vật đột biến...")
            → UnlockFeature("combat")
            → UnlockFeature("weapon_shop")
            → ShowDiary("Phải tự bảo vệ nông trại...")
            → QuestService.ActivateQuest("kill_first_mutant")
    
    Phase 4: "Expansion" (Day 15+ & quest "kill_first_mutant" done)
        - Trigger: Quest complete + Day 15
        - Actions:
            → UnlockFeature("mine_access")
            → UnlockFeature("clear_zones")
            → SendMessage(from: "Thợ mỏ", "Mỏ đã mở cửa trở lại!")

    Phase 5: "Endgame" (Mastery 30+ & specific quests done)
        - Advanced content, boss fights, mythril tier
```

### Phụ thuộc
| Hệ thống | Vai trò |
|-----------|---------|
| NarrativeService | Phase tracking + transition |
| TimeManager | Day check |
| QuestService | Quest completion check |
| ProgressionService | Mastery check |
| FeatureFlagService | Enable features per phase |
| SpawnSystem | Spawn new entities |

### Data
| Asset | Loại | Nội dung |
|-------|------|----------|
| PhaseData | ScriptableObject | phaseId, conditions{day, quest, mastery}, actions[], nextPhaseId |
| Phase_FirstSigns | | day=7, actions=[news, message, diary] |
| Phase_CombatUnlock | | day=10, actions=[spawn, news, unlock, quest] |

### UI
- News broadcast (phase announcement)
- Diary entry (player reaction)
- Message notification
- New feature unlock popup

### Events
| Event | Publisher | Subscriber |
|-------|-----------|-----------|
| PhaseTransitionPublish 🆕 | NarrativeService | FeatureFlagService, SpawnSystem, QuestService |
| StoryEventTriggeredPublish | NarrativeService | Various |

### Scene Objects
- Spawned entities (mutants at farm edge)
- New NPCs (weapon shop, etc.)

---

## TỔNG KẾT: NARRATIVE SYSTEM DEPENDENCY

```
TimeManager → DayChangedPublish
    │
    ▼
NarrativeService.OnNewDay()
    │
    ├── Check StoryEvents conditions
    │       │
    │       ├── ShowDiary() → DiaryEntryPublish → DiaryUI (popup 2-3 lines)
    │       ├── SendMessage() → NewMessagePublish → MessageNotificationUI
    │       ├── ShowNews() → NewsBroadcastPublish → NewsBroadcastUI (banner)
    │       ├── UnlockFeature() → FeatureFlagService
    │       ├── SpawnEntity() → SpawnSystem
    │       └── ActivateQuest() → QuestService
    │
    └── CheckPhaseTransition()
            │
            ├── Phase 1 (Day 1-6): Peaceful farming
            ├── Phase 2 (Day 7-9): News + warnings
            ├── Phase 3 (Day 10+): Combat unlocks + first enemy
            ├── Phase 4 (Day 15+): Mine + clear zones
            └── Phase 5 (Mastery 30+): Endgame

Player Interaction:
    │
    ├── Lore Items (in cleared zones)
    │       → E → LoreItemRuntime → Read (dialogue) / Pickup
    │
    ├── Messages (from menu/mailbox)
    │       → MessageListUI → read full text
    │
    └── Diary Log (from menu)
            → All past diary entries

Data Flow:
    Conditions (day, quest, mastery, items)
        → NarrativeService checks
            → Triggers actions
                → Multiple UI channels deliver narrative
```
