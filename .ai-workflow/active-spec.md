# Active Spec

## Current focus

- Feature or task cluster: Gameplay completion pass after core farming implementation
- Player-facing outcome: Existing systems feel complete, readable, and reliable instead of merely functional

## Scope

- In scope:
  - marker-based world seeding reliability
  - plant/tree taxonomy clarity (`Plant01`, `Plant02`, `TreeNode`, marker usage)
  - regrow tree / crop data completion
  - interaction feedback and readability
  - backlog curation for missing gameplay-completion work
- Out of scope:
  - broad new prototype systems with unclear gameplay value
  - final art production by Codex

## Constraints

- Technical constraints:
  - work must fit unattended 20-30 minute slices
  - prefer existing architecture over new parallel systems
  - automation must update `.ai-workflow` state every slice
- Design constraints:
  - do not redesign the game vision
  - only extend backlog with concrete completion work
- Save/load or scene-seeding concerns:
  - marker-seeded world objects must preserve save/load correctness
  - random stage seeding must only happen on initial seed / respawn, not on normal load

## Acceptance target

- The backlog reflects completion-oriented work, not prototype invention
- Each automated slice has a clear next task
- Missing user-owned art/scene tasks are recorded explicitly instead of being silently ignored

## Validation plan

- Build: `dotnet build Assembly-CSharp-Editor.csproj`
- Runtime test: user playtests specific slices
- Regression check: preserve farming, harvest, watering, and marker-based spawn behavior
