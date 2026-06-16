# DATN_FarmGame - Full Completion Plan v2

Ngay cap nhat: 2026-06-16
Pham vi: hoan thien combat feel, enemy polish, player feel, quest kill objectives, audio, save/load polish, va content cleanup theo dung kien truc hien co.

## 0. Ket luan tu audit hien trang

Da co:
- `HealthRuntime`, `WeaponRuntime`, `AttackRuntime`, `EnemyObject`, `NavAgent2D`, `SpawnSystem`, `SceneContentScanner`, `ProgressionService`, `QuestService`, `PlayerDeathHandler`, `TimeManager`.
- 6 enemy moi: `Slime1`, `Slime2`, `Slime3`, `Orc1`, `Orc2`, `Orc3`, kem animation, prefab, EntityData, marker, world object.
- 20 mine RuleRegion da co enemy progression tu Slime1 den Orc3.
- `ProgressionChangedPublish` va `LevelUpPublish` da ton tai.
- `FloatingCombatTextSpawner.prefab` da ton tai.

Can sua lai so voi ban nhap cu:
- Khong duoc subscribe `TakeDamageEvent` tren `EventBus`. `TakeDamageEvent` la entity-local `IGameEvent`, chi di qua `EntityRuntime.TriggerEvent`.
- Enemy HP bar khong nen dung `StatsChangedPublish`, vi event nay hien chu yeu do player/progression publish. Enemy HP bar nen bind truc tiep `StatsRuntime.OnChanged`.
- Floating damage, hit flash, combat audio, combo, camera shake nen dung mot event global moi duoc publish tu `HealthRuntime` sau khi tinh final damage.
- `HealthRuntime.OnDied` la event tren tung runtime, khong phai global event. Neu can audio/quest/night spawn nghe death tren toan game, them global `EntityDiedPublish`.
- `PlayerControler` co `isDodging` private, chua co `IsDodging` public property.
- `WeaponRuntime` hien chua tinh crit; `AttackRuntime` co tinh crit nhung khong truyen flag sang `HealthRuntime`.
- Rat/Boar/Snake co EntityData va ExpReward, nhung khong thay sprite/animation/prefab trong audit nhanh. Khong nen tao prefab that neu chua co art.
- `enemy_t2_bat` duoc reference trong `GameManager.SpawnNarrativeMutant`, nhung data chua ton tai. Day la bug content can xu ly som.
- Quest monster objectives hien dang la inventory objective (`requiredEntityDataId`), khong phai kill objective.

## 1. Nguyen tac thuc hien

- Global cross-system event: `struct ...Publish` trong `SystemEvents.cs`, di qua `EventBus`.
- Entity-local combat/module event: `class ...Event` trong `ModuleEvent.cs`, di qua `EntityRuntime.TriggerEvent`.
- Data tinh cua entity nam trong `EntityData`; runtime state nam trong `EntityRuntime` va module runtime.
- UI chi subscribe global `Publish` event hoac bind runtime thong qua `EntityRoot.OnEntityReady`; UI khong tu goi service gameplay de xu ly logic.
- Khong tao singleton rieng le tuy tien. Neu can manager runtime, uu tien component do `GameManager` khoi tao hoac scene bootstrap co chu dich.
- Moi slice phai compile sach va co smoke test nho.

## 2. P0 - Combat telemetry spine

Muc tieu: tao nguon su that chung cho toan bo combat feedback.

### File sua

- `Assets/Project/Scripts/Core/SystemEvents.cs`
- `Assets/Project/Scripts/Core/ModuleEvent.cs`
- `Assets/Project/Scripts/Data/Runtime/HealthRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/WeaponRuntime.cs`
- `Assets/Project/Scripts/Data/Runtime/AttackRuntime.cs`
- `Assets/Project/Scripts/Features/Objects/EnemyObject.cs`
- `Assets/Project/Scripts/Features/Player/PlayerControler.cs`

### Them global events

```csharp
public readonly struct DamageAppliedPublish
{
    public readonly EntityRuntime attacker;
    public readonly EntityRuntime target;
    public readonly EntityRuntime sourceItem;
    public readonly float rawDamage;
    public readonly float finalDamage;
    public readonly float hpBefore;
    public readonly float hpAfter;
    public readonly bool isCrit;
    public readonly bool wasFatal;
    public readonly Vector3 worldPosition;
}

public readonly struct EntityDiedPublish
{
    public readonly EntityRuntime entity;
    public readonly EntityRuntime killer;
    public readonly Vector3 worldPosition;
    public readonly bool isPlayer;
}

public readonly struct EnemyAttackStartedPublish
{
    public readonly EntityRuntime enemy;
    public readonly Vector3 worldPosition;
}
```

### Sua `TakeDamageEvent`

Them optional metadata:
- `bool isCrit`
- `EntityRuntime sourceItem`

Tat ca call site cu van compile bang default parameters.

### Sua `HealthRuntime.Handle(TakeDamageEvent)`

Sau khi tinh `finalDamage`, set HP xong, publish `DamageAppliedPublish` voi:
- `rawDamage = e.damage`
- `finalDamage = finalDamage`
- `hpBefore`, `hpAfter`
- `isCrit = e.isCrit`
- `wasFatal = newHp <= 0`
- `worldPosition = _entity.Owner?.GameObject?.transform.position ?? Vector3.zero`

Trong `Die()`, publish `EntityDiedPublish`.

### Sua `WeaponRuntime`

- Tinh crit tu `e.item.stats.Get(StatType.CritChance)` va `CritDamage`.
- Truyen `isCrit` va `sourceItem: e.item` vao `TakeDamageEvent`.
- Chi hit target combat hop le nhu hien tai.

### Sua `AttackRuntime`

- Giu logic crit hien co nhung truyen `isCrit` vao `TakeDamageEvent`.
- Kiem tra target co `HealthRuntime`; tranh gay damage len resource/harvest runtime neu khong mong muon.

### Sua `EnemyObject`

- Khi `BeginAttack()`, publish `EnemyAttackStartedPublish`.
- Them public event C# neu can local component:
  - `public event Action<EnemyState> StateChanged;`
  - `public event Action AttackStarted;`
  - `public event Action DeathStarted;`
  - `public event Action DeathAnimationCompleted;`
- Doi `EnemyState` thanh public nested enum hoac tao enum rieng neu component UI/VFX can tham chieu compile-time.

### Sua `PlayerControler`

- Them `public bool IsDodging => isDodging;`

### Acceptance

- Damage tu enemy va player deu tao `DamageAppliedPublish`.
- Enemy death tao `EntityDiedPublish`.
- Crit flag di tu `WeaponRuntime`/`AttackRuntime` den floating text.
- Compile sach.

## 3. P1 - Combat feedback co ban

Lam sau P0.

### 3.1 Floating damage number

File tao:
- `Assets/Project/Scripts/UI/FloatingDamageNumber.cs`
- `Assets/Project/Scripts/UI/FloatingCombatTextSpawner.cs`

Dung prefab:
- `Assets/Project/Prefabs/UI/FloatingCombatTextSpawner.prefab`

Behavior:
- Subscribe `DamageAppliedPublish`.
- Spawn tai `worldPosition + random x +/-0.3`.
- Pool 20 object.
- So thuong: trang, bay len 1 unit trong 0.8s, fade cuoi 0.3s.
- Crit: vang `#FFD700`, scale 1.3x, them text nho `CRIT!`.
- Khong spawn neu `finalDamage <= 0`.

Acceptance:
- Player danh enemy hien so damage dung final damage.
- Enemy danh player cung hien so neu muon debug/feedback.
- Khong GC spike do instantiate lien tuc.

### 3.2 Hit flash

File tao:
- `Assets/Project/Scripts/Features/Objects/HitFlashEffect.cs`

Behavior:
- Gan vao enemy prefab.
- Subscribe `DamageAppliedPublish`, loc `target.id`.
- Dung `MaterialPropertyBlock`, flash trang 0.08s.
- Neu damage lien tuc, restart flash nhung khong stack coroutine vo han.

Acceptance:
- Slime/Orc nhap nhay trang khi bi danh.
- Khong tao material instance moi trong runtime.

### 3.3 Enemy HP bar

File tao:
- `Assets/Project/Scripts/UI/EnemyHealthBarUI.cs`

Prefab tao:
- `Assets/Project/Prefabs/UI/EnemyHealthBar.prefab`

Behavior:
- World-space canvas hoac child object voi sorting cao.
- Bind `EntityRoot.OnEntityReady`.
- Subscribe `entity.stats.OnChanged`.
- An neu HP day; hien khi mat HP; tu an sau 3s khong doi HP.
- Doi mau xanh -> vang -> do theo HP%.
- Subscribe `EntityDiedPublish` hoac local `HealthRuntime.OnDied` de an ngay khi chet.

Gan vao:
- `Slime1-3`, `Orc1-3`.
- Rat/Boar/Snake chi gan sau khi co prefab that.

### 3.4 Camera shake va hit stop nhe

File sua:
- `Assets/Project/Scripts/Features/Camera/CameraFollow.cs`

File tao:
- `Assets/Project/Scripts/Systems/HitStopManager.cs`

Behavior:
- `CameraFollow.Shake(float intensity, float duration)` dung offset Perlin/noise, khong pha intro follow.
- Subscribe `DamageAppliedPublish` trong component/manager combat feedback:
  - Player hit enemy: shake 0.05/0.1s.
  - Player nhan damage > 5: shake 0.15/0.2s.
- `HitStopManager.TriggerHitStop(0.05f)` chi khi player hit enemy, khong stack, dung realtime wait.

Acceptance:
- Hit co cam giac nhung khong lam UI/time manager loi.
- TimeScale restore chinh xac ve 1.

## 4. P1 - Player survivability feedback

### 4.1 Player i-frame

File tao:
- `Assets/Project/Scripts/Features/Player/PlayerInvincibilityHandler.cs`

Behavior:
- Gan vao Player.
- Bind `EntityRoot.OnEntityReady` va lay `HealthRuntime`.
- Subscribe `DamageAppliedPublish`, loc target la player.
- Khi player nhan damage va khong dang dodge:
  - `HealthRuntime.CanTakeDamage = false` trong 0.6s.
  - Blink sprite/visual moi 0.08s.
  - Restore sau khi het coroutine.
- Neu player chet, stop i-frame va restore visibility.

Acceptance:
- Enemy khong hit player lien tuc moi frame.
- Dodge state khong bi can thiep ngoai y muon.

### 4.2 Low HP vignette

File tao:
- `Assets/Project/Scripts/UI/LowHpVignetteUI.cs`

Behavior:
- Subscribe `StatsChangedPublish` cho player, dong thoi refresh snapshot khi `PlayerReadyPublish`.
- HP < 30% fade vignette do alpha 0.35, pulse nhe.
- HP < 20% co heartbeat SFX neu AudioManager da san sang.
- HP >= 30% fade out.

Acceptance:
- HUD phan hoi ro khi player nguy hiem.
- Khong hien sai cho enemy vi chi loc player entity id.

### 4.3 Dodge afterimage

File tao:
- `Assets/Project/Scripts/Features/Player/DodgeAfterimageEffect.cs`

Behavior:
- Dung `PlayerControler.IsDodging`.
- Pool 10 ghost sprites.
- Moi 0.04s tao ghost tai vi tri hien tai, alpha 0.4 -> 0 trong 0.2s.

## 5. P1 - Enemy polish

### 5.1 Alert indicator

File tao:
- `Assets/Project/Scripts/Features/Objects/EnemyAlertIndicator.cs`

Prerequisite:
- `EnemyObject.StateChanged` co public state.

Behavior:
- Khi state vao Chase lan dau, pop `!` tren dau.
- Scale 0 -> 1.2 -> 1, giu 0.5s, fade out.

### 5.2 Death VFX

File tao:
- `Assets/Project/Scripts/Features/Objects/EnemyDeathEffect.cs`

Prerequisite:
- `EnemyObject.DeathStarted` va `DeathAnimationCompleted`.

Behavior:
- Khi death started: fade alpha + scale nhe hoac particle burst.
- Khong destroy truc tiep; `MortalRuntime.destroyDelay` da cho death animation chay truoc despawn.

### 5.3 Combat audio

Nen lam sau AudioManager P1 trong muc 7.

File tao:
- `Assets/Project/Scripts/Systems/CombatAudioManager.cs`

Behavior:
- Subscribe `DamageAppliedPublish`, `EntityDiedPublish`, `EnemyAttackStartedPublish`.
- Play hit, crit, enemy death, player hurt, enemy attack.
- AudioSource pool 8.
- 3D sound max distance 10.

## 6. P1 - Progression, EXP popup, level up

### 6.1 EXP popup

File tao:
- `Assets/Project/Scripts/UI/ExpPopupSpawner.cs`

Behavior:
- Subscribe `ProgressionChangedPublish`.
- Loc target la player.
- Hien `+{amount} EXP` mau `#4FC3F7`, bay len 1.5 units trong 1.2s.
- Vi tri spawn: player transform hoac HUD anchor gan player.

### 6.2 Level up effect

File tao:
- `Assets/Project/Scripts/UI/LevelUpEffect.cs`

Behavior:
- Subscribe `LevelUpPublish`.
- Hien `LEVEL UP!` giua man hinh, slide/scale/fade.
- Play sound.
- Particle ring quanh player neu co prefab particle.
- Refresh UI da co thong qua `StatsChangedPublish` cua `ProgressionService`.

Acceptance:
- Kill enemy cap EXP hien popup.
- Level up co UI va sound, stat HUD cap nhat.

## 7. P1 - Audio system nen tang

### 7.1 AudioManager trung tam

File tao:
- `Assets/Project/Scripts/Systems/AudioManager.cs`

Khoi tao:
- Them vao `GameManager` hoac bootstrap scene mot component duy nhat.

Behavior:
- Channels: Music, SFX, Ambient.
- Volume rieng tung channel, save `PlayerPrefs`.
- `PlaySFX(AudioClip clip, Vector3 pos)`.
- `PlayMusic(AudioClip clip, bool loop)`.
- `PlayAmbient(AudioClip clip)`.
- Crossfade music/ambient 1.5-2s.

### 7.2 Tool sound

File sua:
- `Assets/Project/Scripts/Features/Player/ToolActionBridge.cs`

Behavior:
- Neu `ToolInfo.useSound` da co clip, play SFX khi action bat dau.
- Khong play trong moi frame animation.

### 7.3 Ambient sound

File tao:
- `Assets/Project/Scripts/Systems/AmbientSoundManager.cs`

Behavior:
- Subscribe `GameTimeChangedPublish` hoac `DayChangedPublish`.
- Doi track theo scene + ngay/dem.
- Farm day: birds/wind.
- Farm night: crickets/owl.
- Mine: cave ambience.

## 8. P1/P2 - Quest kill objectives

Van de hien tai:
- `QuestService` chi check inventory objectives qua `requiredEntityDataId`.
- Quest mixed monster ids nhu `monster_t1_slime`, `monster_t2_bat` khong phai kill counters.

Huong sua dung kien truc:
- Mo rong quest objective data bang type:
  - `Inventory`
  - `KillEnemy`
  - `SurviveNight`
- Them runtime/log progress cho objective, luu trong quest log save data neu can.
- Subscribe `EntityDiedPublish`, neu killer la player thi tang kill count theo `entity.entityData.id`.

File can inspect/sua:
- Quest graph/objective data class.
- `QuestLogRuntime`
- `QuestService`
- Quest UI objective view.
- Quest assets `Quest_M5_Mixed_T1..T5`.

Acceptance:
- Quest kill slime tang khi giet `enemy_slime1`/tier tuong ung, khong can loot item gia.
- Save/load giu progress kill.
- Quest complete UI hien current/required dung.

## 9. P2 - Content cleanup enemy cu va bat

### 9.1 Bat reference

Van de:
- `GameManager.SpawnNarrativeMutant` tim `enemy_t2_bat`, fallback `enemy_t1_slime`; ca hai khong khop data moi.

Quick fix an toan:
- Doi fallback sang `enemy_slime2` hoac `enemy_orc1` va ObjectType tuong ung, khong tao bat gia neu chua co art.

Long-term:
- Tao `enemy_bat.asset`, prefab, animation, ObjectType neu co art.

### 9.2 Rat/Boar/Snake prefabs

Hien trang:
- Data co: `enemy_rat`, `enemy_snake`, `enemy_boar`.
- Audit nhanh khong thay art/prefab/animation source cho rat/boar/snake.

Ke hoach:
- Chi tao prefab khi co sprite/animation source.
- Neu can dung tam, co the tao variant tu Slime/Orc voi icon/visual placeholder, nhung phai danh dau `placeholder` va khong dua vao scene production.

### 9.3 ExpReward

Hien trang audit:
- Rat/Snake/Boar va Slime/Orc deu da co `ExpRewardModule`.
- Gia tri hien tai:
  - Slime1 20, Slime2 35, Slime3 55
  - Orc1 90, Orc2 140, Orc3 220
  - Rat 22, Snake 37, Boar 90

Khong nen ha ve ban nhap cu neu chua balance lai mine progression va player weapon curve.

## 10. P2 - Night enemy spawn

Khuyen nghi:
- Khong tao wave system rieng truoc khi quest kill va audio feedback on dinh.
- Uu tien dung marker/RuleRegion co san thay vi random point thuan tuy.

File tao:
- `Assets/Project/Scripts/Systems/NightEnemySpawnManager.cs`

Behavior:
- Subscribe `GameTimeChangedPublish` de biet `normalizedTime`.
- Night window nen la `normalizedTime >= 0.78 || normalizedTime <= 0.22`.
- Max active night enemies: 5.
- Spawn tu marker `SceneMarkerKind.Enemy` hoac pool level-appropriate.
- Sang ngay: publish despawn/destroy cho night-only enemies.
- Stat multiplier nen di qua clone/runtime modifier, khong mutate `EntityData`.

Acceptance:
- Khong duplicate spawn moi minute.
- Save/load khong giu lai enemy dem qua ngay neu policy la temporary.

## 11. P2 - Save/load polish

### 11.1 Auto-save cuoi ngay

File sua:
- `Assets/Project/Scripts/Systems/SaveLoadManager.cs`

Behavior:
- Subscribe `DayChangedPublish`.
- Goi save non-blocking neu manager da boot xong.
- Publish toast UI `Game saved`.

Can them:
- `ToastPublish` hoac UI toast system neu chua co.

### 11.2 Death screen

File tao:
- `Assets/Project/Scripts/UI/DeathScreenUI.cs`

File sua:
- `PlayerDeathHandler.cs`

Behavior:
- `PlayerDeathHandler` publish `PlayerDeathPublish`.
- UI fade den, text `You died`, delay 2s.
- Penalty: stamina da co; gold penalty chi them neu money/currency flow da ro.

## 12. P2/P3 - Player extra polish

- Weapon swing trail:
  - Tao `WeaponSwingTrail.cs`.
  - Dung state `ToolActionBridge.IsBusy`, bat TrailRenderer trong attack window.
- Combo counter:
  - Subscribe `DamageAppliedPublish`, loc attacker la player va target co `EnemyObject`.
  - Combo reset sau 2s.
  - Neu bonus damage, phai chen vao `WeaponRuntime` truoc `TakeDamageEvent`, khong chi UI.
  - Nen de P3 vi can balance damage.

## 13. Thu tu thuc hien de it loi nhat

### Slice 1 - Combat telemetry spine

Files:
- `SystemEvents.cs`
- `ModuleEvent.cs`
- `HealthRuntime.cs`
- `WeaponRuntime.cs`
- `AttackRuntime.cs`
- `EnemyObject.cs`
- `PlayerControler.cs`

Validation:
- Compile.
- Play Mode: player hit Slime1 -> log `DamageAppliedPublish`.
- Kill Slime1 -> `EntityDiedPublish`, EXP van grant.

### Slice 2 - Damage text + hit flash + HP bar

Files:
- `FloatingDamageNumber.cs`
- `FloatingCombatTextSpawner.cs`
- `HitFlashEffect.cs`
- `EnemyHealthBarUI.cs`
- enemy prefabs

Validation:
- Hit enemy -> number + flash + HP bar.
- Enemy death -> HP bar hidden.

### Slice 3 - Camera shake + hit stop + player i-frame

Files:
- `CameraFollow.cs`
- `HitStopManager.cs`
- `PlayerInvincibilityHandler.cs`

Validation:
- Enemy attack player -> HP drops once, blink i-frame.
- Player hit enemy -> small shake/hit stop.

### Slice 4 - EXP/level UI

Files:
- `ExpPopupSpawner.cs`
- `LevelUpEffect.cs`

Validation:
- Kill enemy -> EXP popup.
- Force grant EXP -> LevelUp UI.

### Slice 5 - Audio

Files:
- `AudioManager.cs`
- `CombatAudioManager.cs`
- `ToolActionBridge.cs`
- `AmbientSoundManager.cs`

Validation:
- SFX overlap without cutting off.
- Scene/time ambient crossfade.

### Slice 6 - Quest kill objective

Files:
- quest objective data/runtime/service/UI save.

Validation:
- Kill enemy count persists after save/load.
- Mixed quest can combine kill + inventory objectives.

### Slice 7 - Content cleanup

Files:
- `GameManager.cs` narrative mutant reference.
- Optional bat/rat/boar/snake assets only when art exists.

Validation:
- No missing enemy data warning.
- No placeholder prefab in production scene unless intentional.

### Slice 8 - Night spawns and death/save polish

Files:
- `NightEnemySpawnManager.cs`
- `SaveLoadManager.cs`
- `DeathScreenUI.cs`
- `PlayerDeathHandler.cs`

Validation:
- Night spawns cap at 5 and despawn in morning.
- Auto-save at day rollover.
- Death screen flow does not fight `RespawnRuntime`.

## 14. Definition of done

- No Unity compile errors.
- No new persistent save incompatibility without migration/default handling.
- No global EventBus subscription to entity-local `IGameEvent` classes.
- Combat feedback works for both player weapon hits and enemy attacks.
- Enemy UI/VFX components bind through `EntityRoot.OnEntityReady`.
- Quest kill progress is saved and restored.
- All generated/prefab changes are repeatable or documented.
- `.ai-workflow/active-spec.md`, `master-backlog.md`, and `agent-handoff.md` are updated after each implementation slice.

