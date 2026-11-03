# Startup loading

`Assets/Scenes/Loading.unity` is the first enabled build scene. It uses a copy of Stage1's experience bar and then opens the existing CutSceen flow.

DataManager reads game data before any pooled copies are created. Objects are instantiated below an inactive staging object, moved under `PoolManager/<PrefabName>Mom`, and remain disabled until requested. Returned objects go back to their own Mom, including temporarily reparented UI objects.

Prewarm targets per prefab:
- Enemy and Boss folders: 200
- Exp: 200
- Upgrade: 5, except Snail: 10
- SkillEffect (basic attacks): 5
- Bullet: 100
- BossPattern and other pools: no added prewarm target

Retention capacity is at least the prewarm target. Runtime demand can still create additional objects.

Verification in Unity 6000.3.10f1 on 2026-11-01:
- Loading bar visually verified at 70%.
- Prewarm completed and transitioned to CutSceen.
- 3,185 distinct inactive objects; all have the correct Mom parent.
- Checked out every prewarmed object simultaneously without activation: total stayed 3,185. Returned all; inactive count returned to 3,185.
- Bullet IDs 600/601/602 each retained 100; Snail retained 10; basic attacks each retained 5.
- Pooled enemy data matched the loaded CSV data.
- Stage1 transition retained the same PoolManager instance and the 3,185 prewarmed objects.
- No gameplay or compilation errors during the final loading and Stage1 transition test.

`CreateLoadingScene.cs` is an Editor eval script; it requires Stage1 open as the experience-bar source. `VerifyPrewarm.cs` checks a completed runtime pool without activating gameplay objects.

## Resident scene cache — 2026-11-02

Loading prepares every other enabled build scene additively alongside pool prewarming. `PreparedScene` keeps each scene's authored roots disabled, so its gameplay Awake/Start methods do not run while it waits. Scene objects and assets remain resident.

A fresh pointer press fades Loading to black and activates the prepared CutSceen roots. CutSceen, Lobby, and Stage1 are subsequently selected by hiding the previous scene's roots, changing the active scene, and enabling the desired roots. These transitions do not load or unload scenes. The startup scene itself remains resident with its camera/UI disabled.

The scene gate restores authored child visibility and root transforms on reuse; existing retry/return callbacks reset progression, upgrades and stage spawning. Persistent managers remain active in DontDestroyOnLoad. EXP event bindings, lobby music/preview, selected character and game-over state support repeated activation.

Loading and both cutscene fade directions share `GameSceneManager.TransitionFadeSeconds`, initialized from the loading screen's inspector fade duration (currently 0.8 seconds). All use SmoothStep, unscaled time and a maximum 0.05-second step. Cutscene covers use pure black.

Direct Editor play of CutSceen/Lobby/Stage1 activates its roots automatically. Without the startup cache, scene transitions fall back to asynchronous loading.

`PrepareSceneGates.cs` configures the three saved scene gates. `VerifySceneCacheFlow.cs` exercises startup, fades, lobby, stage, retry, a second game-over and lobby return; results are in `scene-cache-flow.txt`.
