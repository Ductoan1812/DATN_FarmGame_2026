# 03 — UI DATA FLOW (UI Component Architecture)

> Mỗi UI: Purpose → Data Source → Data Push → User Actions → Subscribe → Publish

---

## ✅ HotbarUI

**Purpose:** Display 8 hotbar slots with item icons, amounts, and selection highlight.

**Data Source:** Player's InventoryRuntime (type=Hotbar)

**Data Push Method:**
- `InventoryChangedPublish` → refresh slot icons/amounts
- `HotbarSelectionChangedPublish` → move highlight border

**User Actions:**
- Click slot → select that slot (publish HotbarSelectionChangedPublish)
- Drag item → SwapSlots (via InventoryService)

**Subscribe Events:**
- `InventoryChangedPublish` (filter: player hotbar)
- `HotbarSelectionChangedPublish`

**Publish Events:**
- `HotbarSlotClickedPublish(int slotIndex)` → PlayerInventory.SelectSlot

---

## ✅ BackpackUI

**Purpose:** Display full backpack inventory grid (expandable slots).

**Data Source:** Player's InventoryRuntime (type=Backpack)

**Data Push Method:**
- `InventoryChangedPublish` → refresh all slots

**User Actions:**
- Click item → select/info popup
- Drag item → swap/move between slots
- Right-click → use/consume item
- Sort button → InventoryService.Sort()

**Subscribe Events:**
- `InventoryChangedPublish` (filter: player backpack)

**Publish Events:**
- `InventorySlotInteractPublish(slot, action)` → InventoryService operations

---

## ✅ EquipmentUI

**Purpose:** Display equipment slots (Head, Body, Legs, Accessory, Weapon) with equipped items.

**Data Source:** Player's EquipmentRuntime

**Data Push Method:**
- `EquipmentChangedPublish` → refresh equipment slot visuals
- `InventoryChangedPublish` → refresh stat display

**User Actions:**
- Click equipped item → unequip (return to inventory)
- Drag from inventory → equip to matching slot

**Subscribe Events:**
- `EquipmentChangedPublish`
- `InventoryChangedPublish`

**Publish Events:**
- `EquipRequestEvent(item, slot)` → EquipmentRuntime

---

## ✅ ShopPanelUI

**Purpose:** Display merchant stock, buy/sell interface, gold display.

**Data Source:** ShopService.BuildView() → ShopViewData

**Data Push Method:**
- `ShopViewPublish(ShopViewData)` → populate item list
- `ShopTransactionResultPublish` → refresh stock/gold after transaction

**User Actions:**
- Click item → select for buy/sell
- Amount slider/buttons → set quantity
- Buy button → ShopService.TryBuy()
- Sell button → ShopService.TrySell()
- Close button → close panel

**Subscribe Events:**
- `ShopViewPublish`
- `ShopTransactionResultPublish`
- `InventoryChangedPublish` (refresh sell-able items)

**Publish Events:**
- None directly (calls ShopService static methods)

---

## ✅ CraftingPanelUI

**Purpose:** Display available recipes, ingredient status, craft button.

**Data Source:** CraftingService.BuildView() → CraftingViewData

**Data Push Method:**
- `CraftingViewPublish(CraftingViewData)` → populate recipe list
- `CraftingResultPublish` → show success/fail, refresh view

**User Actions:**
- Click recipe → show ingredients + output preview
- Craft button → CraftingService.TryCraft()
- Amount selector → craft multiple
- Close button → close panel

**Subscribe Events:**
- `CraftingViewPublish`
- `CraftingResultPublish`
- `InventoryChangedPublish` (refresh ingredient counts)

**Publish Events:**
- None directly (calls CraftingService instance methods)

---

## ✅ DialoguePanelUI

**Purpose:** Display dialogue text, speaker name, choices.

**Data Source:** DialogueService → DialogueViewData

**Data Push Method:**
- `DialogueViewPublish(DialogueViewData)` → show text + choices
- `DialogueEndPublish` → close panel

**User Actions:**
- Click choice → DialogueService.AdvanceDialogue(choiceId)
- Click continue → advance to next node
- Click skip → DialogueService.EndDialogue()

**Subscribe Events:**
- `DialogueViewPublish`
- `DialogueEndPublish`

**Publish Events:**
- None (calls DialogueService directly)

---

## ✅ QuestPanelUI

**Purpose:** Display single quest details (title, description, objectives, rewards).

**Data Source:** QuestService.ShowQuest() → QuestViewData

**Data Push Method:**
- `QuestViewPublish(QuestViewData)` → populate quest info

**User Actions:**
- Accept button → QuestService.AcceptQuest()
- Complete button → QuestService.CompleteQuest()
- Close button → close panel

**Subscribe Events:**
- `QuestViewPublish`
- `QuestStateChangedPublish` (refresh objective progress)

**Publish Events:**
- None (calls QuestService directly)

---

## ✅ QuestLogWindowUI

**Purpose:** Display all active/completed quests in a list.

**Data Source:** Player's QuestLogRuntime

**Data Push Method:**
- `QuestStateChangedPublish` → refresh quest list
- Manual open → read QuestLogRuntime.GetAllStates()

**User Actions:**
- Click quest → show details (QuestPanelUI)
- Filter tabs (Active/Completed)

**Subscribe Events:**
- `QuestStateChangedPublish`

**Publish Events:**
- `QuestViewPublish` (when selecting a quest to view)

---

## ✅ InventoryUI (Combined)

**Purpose:** Full inventory window combining Hotbar + Backpack + Equipment in one panel.

**Data Source:** Player's InventoryRuntime (all types) + EquipmentRuntime

**Data Push Method:**
- `InventoryChangedPublish` → refresh all sections
- `EquipmentChangedPublish` → refresh equipment section

**User Actions:**
- All actions from HotbarUI + BackpackUI + EquipmentUI
- Tab switching between sections

**Subscribe Events:**
- `InventoryChangedPublish`
- `EquipmentChangedPublish`

**Publish Events:**
- Various slot interaction events

---

## ✅ HealthBarUI

**Purpose:** Display player HP bar (current/max) on HUD.

**Data Source:** Player EntityRuntime.stats (HP, MaxHP)

**Data Push Method:**
- `PlayerDamagedPublish` → animate HP decrease
- `ProgressionChangedPublish` → update max HP bar size

**User Actions:** None (display only)

**Subscribe Events:**
- `PlayerDamagedPublish`
- `ProgressionChangedPublish`
- `PlayerRespawnedPublish` (reset to full)

**Publish Events:** None

---

## ✅ HudStatusMapUI

**Purpose:** Display stamina bar, time, weather icon, money on HUD.

**Data Source:** Player stats, TimeManager, WeatherSystem

**Data Push Method:**
- `GameHourChangedPublish` → update clock display
- `WeatherChangedPublish` → update weather icon
- `InventoryChangedPublish` / stat change → update money display
- Stamina polled per frame or on action events

**User Actions:** None (display only)

**Subscribe Events:**
- `GameHourChangedPublish`
- `WeatherChangedPublish`
- `ProgressionChangedPublish`

**Publish Events:** None

---

## ✅ PlayerInfoHUDUI

**Purpose:** Display Mastery level, EXP bar on HUD.

**Data Source:** Player stats (Level, Exp, MaxExp)

**Data Push Method:**
- `ProgressionChangedPublish` → update EXP bar fill + level number
- `LevelUpPublish` → celebration animation

**User Actions:** None (display only)

**Subscribe Events:**
- `ProgressionChangedPublish`
- `LevelUpPublish`

**Publish Events:** None

---

## ✅ InteractionMenuUI

**Purpose:** Display interaction options when multiple options available (popup near target).

**Data Source:** InteractionContext.Options[]

**Data Push Method:**
- `InteractionOptionsReadyPublish(actor, target, options[])` → show menu

**User Actions:**
- Click option → execute option callback
- Click away → dismiss menu

**Subscribe Events:**
- `InteractionOptionsReadyPublish`

**Publish Events:**
- None (executes callback directly)

---

## ✅ SettingsWindowUI

**Purpose:** Game settings (volume, controls, AI toggle, save/load).

**Data Source:** PlayerPrefs / GameConfig

**Data Push Method:** Manual open (from pause menu)

**User Actions:**
- Sliders → adjust volume
- Toggles → enable/disable features (AI Assistant, etc.)
- Save button → EventBus.Publish(SaveGameRequestPublish)
- Load button → EventBus.Publish(LoadGameRequestPublish)

**Subscribe Events:** None

**Publish Events:**
- `SaveGameRequestPublish`
- `LoadGameRequestPublish`
- `SettingsChangedPublish`

---

## ✅ HUDManager

**Purpose:** Orchestrate HUD visibility — show/hide HUD elements based on game state.

**Data Source:** Game state (in menu, in dialogue, in shop, etc.)

**Data Push Method:**
- `DialogueViewPublish` → hide HUD
- `DialogueEndPublish` → show HUD
- `ShopViewPublish` → hide HUD
- Panel close events → show HUD

**User Actions:** None (automatic)

**Subscribe Events:**
- All panel open/close events
- `GameStateChangedPublish`

**Publish Events:** None

---

## 🆕 AIAssistantUI

**Purpose:** Small panel (bottom-right) showing 1-2 line tips from AI Assistant.

**Data Source:** AIAssistantService.GetCurrentTip()

**Data Push Method:**
- `AITipChangedPublish(string tipText)` → update displayed text with fade animation
- `GameHourChangedPublish` → periodic refresh

**User Actions:**
- Dismiss button (X) → hide current tip
- Toggle in settings → enable/disable panel entirely

**Subscribe Events:**
- `AITipChangedPublish`
- `GameHourChangedPublish`
- `DayChangedPublish`
- `LevelUpPublish` (check new tip unlocks)

**Publish Events:**
- `AITipDismissedPublish` (analytics/tracking)

---

## 🆕 EndOfDaySummaryUI

**Purpose:** Show daily summary after sleep (income, crops grown, EXP gained, tomorrow weather).

**Data Source:** DailyTracker.GetSummary(), WeatherSystem.GetTomorrow()

**Data Push Method:**
- `DayChangedPublish` → triggered during sleep flow → show summary panel
- Called explicitly by BedRuntime/ExhaustionHandler after AdvanceToNextDay

**User Actions:**
- Continue button → dismiss panel, fade in to new day
- (Auto-dismiss after 5s optional)

**Subscribe Events:**
- `DayChangedPublish` (with flag indicating sleep-triggered)

**Publish Events:**
- `DaySummaryClosed` → signal to complete fade-in

---

## 🆕 CalendarUI

**Purpose:** Display current day, season, upcoming events/forecasts.

**Data Source:** TimeManager (day, season), WeatherSystem (forecast), NarrativeService (upcoming events)

**Data Push Method:**
- `DayChangedPublish` → update day number + season
- `SeasonChangedPublish` → update season display
- Manual open → read current state

**User Actions:**
- Open from HUD clock click or menu
- Scroll through days (view past/future)
- Close button

**Subscribe Events:**
- `DayChangedPublish`
- `SeasonChangedPublish`

**Publish Events:** None

---

## 🆕 MinimapUI

**Purpose:** Small overhead map showing farm layout, player position, key locations.

**Data Source:** Tilemap data, player transform, building positions

**Data Push Method:**
- Polled per frame (camera follow player)
- `ZoneClearedPublish` → reveal new area on minimap

**User Actions:**
- Click minimap → expand to full map view
- Toggle visibility

**Subscribe Events:**
- `ZoneClearedPublish` (reveal areas)

**Publish Events:** None

---

## 🆕 DiaryUI

**Purpose:** Popup (bottom-center) showing diary entries — 2-3 lines, auto-fade.

**Data Source:** NarrativeService → DiaryEntryPublish

**Data Push Method:**
- `DiaryEntryPublish(string text)` → show popup with typewriter effect

**User Actions:**
- Click to dismiss early
- Open diary log from menu → view all past entries

**Subscribe Events:**
- `DiaryEntryPublish`

**Publish Events:** None

---

## 🆕 MessageNotificationUI

**Purpose:** Notification icon (HUD) + message list panel for NPC messages.

**Data Source:** NarrativeService message inbox

**Data Push Method:**
- `NewMessagePublish(sender, preview)` → show notification badge + sound

**User Actions:**
- Click notification icon → open message list
- Click message → read full text, mark as read
- Close button

**Subscribe Events:**
- `NewMessagePublish`

**Publish Events:**
- `MessageReadPublish(messageId)` (mark read)

---

## 🆕 NewsBroadcastUI

**Purpose:** Top-screen banner showing world news (typewriter text, auto-dismiss).

**Data Source:** NarrativeService → NewsBroadcastPublish

**Data Push Method:**
- `NewsBroadcastPublish(string text)` → queue and display banner

**User Actions:**
- Click to dismiss early
- (Auto-dismiss after 8s)

**Subscribe Events:**
- `NewsBroadcastPublish`

**Publish Events:** None

---

## 🆕 TutorialUI

**Purpose:** Step-by-step tutorial hints for new players — highlight UI elements + show instructions.

**Data Source:** TutorialService (step list, current step)

**Data Push Method:**
- `TutorialStepPublish(stepData)` → show hint text + highlight target element

**User Actions:**
- Complete action → auto-advance to next step
- Skip button → skip all tutorials
- (Tutorial auto-detects completion via game events)

**Subscribe Events:**
- `TutorialStepPublish`
- Various game events (to detect step completion)

**Publish Events:**
- `TutorialSkippedPublish`
- `TutorialCompletedPublish`
