# Agent Handoff

## Last completed slice

- Summary:
  - traced marker seeding through `SceneContentScanner -> SpawnSystem -> WorldEntityService -> SaveLoadManager`
  - confirmed marker random start stage is only injected through `SceneSpawnPayload.startStageIndex` on fresh seed / respawn
  - fixed a persistence gap where destroyed `Persistent` marker entities could reseed after reload because the world save did not remember consumed marker ids
  - added saved tombstones for removed persistent marker ids in `WorldEntityService`
  - wired `SpawnSystem` destroy flow to mark consumed persistent markers before unregistering them
- Build / verification result:
  - `dotnet build Assembly-CSharp.csproj` passed after marker persistence fix

## Files changed

- `Assets/Project/Scripts/Core/Service/WorldEntityService.cs`
- `Assets/Project/Scripts/Systems/SpawnSystem.cs`
- `.ai-workflow/master-backlog.md`
- `.ai-workflow/agent-handoff.md`

## Current state

- What is working:
  - marker random stage selection is applied before spawn and persisted through normal entity save/load
  - destroyed `Persistent` marker entities can now leave behind a saved tombstone so they do not reseed on reload
- What is partially done:
  - runtime verification of the new tombstone behavior still needs playtest coverage
  - marker asset audit for correct `Persistent` vs `Regenerating` usage has not started yet
- Known risks or blockers:
  - repo worktree contains many unrelated user changes; future commits must stay tightly scoped
  - some already-dirty marker/random-stage changes exist in the worktree; future diffs should be reviewed carefully before commit

## Exact next step

- Continue the `Marker seeding reliability pass` with runtime-oriented verification and asset audit:
  - verify a `Persistent` marker tree destroyed once does not reseed after save + reload
  - verify a `Regenerating` marker tree waits for respawn, then reseeds with a new randomized stage
  - list any marker assets that are using the wrong save policy for their intended gameplay

## User-owned follow-up items

- Replace placeholder or temporary sprites where gameplay logic is complete but visuals are not final
- Create or refine object-specific animations where systems are working but presentation is incomplete
- Design scene tilemaps / dressing when logic is done but world presentation still needs art ownership

## Notes for next unattended run

- Read `.ai-workflow/active-spec.md`
- Read `.ai-workflow/master-backlog.md`
- Continue the top in-progress or highest-priority todo item
- Schedule the following continuation before doing substantive work
- Current heartbeat automation: `datn-gameplay-completion-follow-up`
