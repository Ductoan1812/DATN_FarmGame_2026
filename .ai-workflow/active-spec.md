# Active Spec

## Current focus

- Feature or task cluster: Menu window shell standardization pass
- Player-facing outcome: The inventory-style menu tabs share a cleaner, more consistent shell so future UI work can focus on each tab's real content structure instead of duplicating chrome and layout helpers.

## Scope

- In scope:
  - a shared shell helper for inventory-style menu windows
  - standardizing dynamically generated tabs first, starting with `QuestLogWindowUI` and `SettingsWindowUI`
  - preserving existing UI controller flow and gameplay bindings while cleaning layout creation
- Out of scope:
  - a full rewrite of `BackpackUI` or `EquipmentUI`
  - gameplay logic changes in quest, settings, inventory, or equipment systems
  - new art assets, icon packs, or animator work

## Constraints

- Technical constraints:
  - manual code edits must use `apply_patch`
  - keep generated-in-code layouts working without requiring scene or prefab surgery
  - do not break `UIController`, tab switching, or current event-driven bindings
- Design constraints:
  - tabs may share a shell, but content structures must remain separate by use case
  - structural cleanup should come before visual over-polish
- Save/load or scene-seeding concerns:
  - none for this slice beyond keeping menu state behavior unchanged

## Acceptance target

- `QuestLogWindowUI` and `SettingsWindowUI` use a shared shell/layout helper.
- The new helper compiles cleanly and is available for later `Inventory` / `Equipment` refactors.
- Existing menu behavior still builds without compile regressions.

## Validation plan

- Build:
  - `dotnet build Assembly-CSharp.csproj`
  - `dotnet build Assembly-CSharp-Editor.csproj`
- Code checks:
  - confirm `QuestLogWindowUI` and `SettingsWindowUI` both route layout creation through `MenuWindowShellUI`
  - confirm the new helper is included in the runtime project file
