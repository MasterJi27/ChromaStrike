# Feature coverage

Implementation coverage for the current build. 7 EditMode tests, 5 PlayMode tests and the screenshot walkthrough pass.

## Gameplay and UI

- **Clean start UI with title and Start button:** scene builder/UIManager and the start-screen capture.
- **Flat arena and WASD/mouse FPS controller:** FirstPersonController, PlayerInputReader and Game scene.
- **Initial 30 spheres with 15 red / 10 green / 5 blue:** GameConfig.InitialTally and SpawnerManager.SpawnInitialBatch. Covered by GameplayTests.
- **Spawn bounds X/Z [-10,10], Y=1:** GameConfig + SpawnPointFinder; tests inspect actual instantiated positions.
- **No sphere/player overlap:** same-frame occupied-position checks, player clearance and Physics.CheckSphere against blockers. Requests queue if no valid point is found.
- **Camera-center ray on left click:** ClickHandler. Gameplay test checks an actual ray hit and a sky miss.
- **10-destroyed replacement with exact color mix:** BatchTracker and SpawnerManager. The test uses the 5/2/3 example.
- **One random extra sphere every 5 seconds:** RepeatingTimer/SpawnerManager.Tick. Counts update with the spawned color.
- **Top-right live red/green/blue counters:** UIManager/CounterRow and scene layout.
- **Top-center 120-second timer:** TimerView and CountdownTimer.
- **Stop gameplay at expiry:** GameManager disables input and spawner retries and freezes time.
- **End UI with final counts and Restart:** UIManager. Final counts mean live spheres remaining, consistent with the in-game counter.
- **Responsibility-based scripts, encapsulation and configurable values:** Core/Player/Spheres/UI/Feedback folders and GameConfig asset. Layout/environment generation constants are in editor scripts.

## Build and documentation

- **Unity project without generated folders:** project source is tracked; generated folders and build artifacts are excluded by `.gitignore`.
- **Windows standalone application:** `Builds/Windows/Chroma Strike.exe` builds with 0 errors and 0 warnings.
- **Installer with game name and optional shortcut:** Inno Setup recipe with app name and optional desktop/Start Menu shortcuts.
- **Documentation:** README includes summary, gameplay, controls, packages, key classes, limitations and embedded screenshots.
- **Screenshots:** five files in `Docs/Screenshots`, captured from the current scene.
- **Gameplay recording:** `Docs/ChromaStrike_Gameplay.mp4` (68 seconds).

## Evidence limits

Runtime, editor and test assemblies compile with zero errors using Unity's installed compiler and cached reference assemblies. Unity tests passed: 7 EditMode and 5 PlayMode. The Windows build and installer were rebuilt. See VALIDATION.md for scope and remaining checks.
