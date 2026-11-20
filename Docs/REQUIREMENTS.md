# Assignment coverage - Task 1

Reference: user-supplied `VR-Unity Developer Assignment-130825-103137.pdf`, Task 1. Source and existing artifacts were inspected on 1 November 2026. Items below describe implementation coverage; 7 EditMode tests, 5 PlayMode tests and the screenshot walkthrough passed.

## Gameplay and UI

- **Clean Start UI with title and Start button:** scene builder/UIManager and existing start-screen capture.
- **Flat arena and WASD/mouse FPS controller:** FirstPersonController, PlayerInputReader and Game scene.
- **Initial 30 spheres with 15 red / 10 green / 5 blue:** GameConfig.InitialTally and SpawnerManager.SpawnInitialBatch. Covered by GameplayTests.
- **Spawn bounds X/Z [-10,10], Y=1:** GameConfig + SpawnPointFinder; tests inspect actual instantiated positions.
- **No sphere/player overlap:** same-frame occupied-position checks, player clearance and Physics.CheckSphere against blockers. Requests queue if no valid point is found.
- **Camera-center ray on left click:** ClickHandler. Gameplay test checks an actual ray hit and a sky miss.
- **10-destroyed replacement with exact color mix:** BatchTracker and SpawnerManager. The test uses the assignment's 5/2/3 example.
- **One random extra sphere every 5 seconds:** RepeatingTimer/SpawnerManager.Tick. Counts update with the spawned color.
- **Top-right live red/green/blue counters:** UIManager/CounterRow and scene layout.
- **Top-center 120-second timer:** TimerView and CountdownTimer.
- **Stop gameplay at expiry:** GameManager disables input and spawner retries and freezes time. The retry-disable guard was added in this pass.
- **End UI with final counts and Restart:** UIManager. Final counts mean live spheres remaining, consistent with the in-game counter.
- **Responsibility-based scripts, encapsulation and configurable values:** Core/Player/Spheres/UI/Feedback folders and GameConfig asset. Layout/environment generation constants are in editor scripts.

## Deliverables

- **Unity project excluding Library/Temp/Logs:** project source exists; archive script excludes those folders and other generated artifacts.
- **Windows standalone application:** current `Builds/Windows/Chroma Strike.exe` rebuilt with the expiry fix; 0 errors and 0 warnings.
- **Installer with game name, optional shortcut:** `Installer/ChromaStrike-Setup-1.0.0.exe` and `.iss` present. Script includes app name and optional desktop/Start Menu shortcuts. The installer was rebuilt and installed into a clean project-local folder; full launch and uninstall checks remain pending.
- **Documentation:** README now includes summary, gameplay, controls, packages, key classes, limitations and embedded screenshot links.
- **Screenshots:** five files present; start/end captures were visually reviewed. They were refreshed from the current scene after the expiry guard change.
- **Optional short gameplay recording:** not required for Task 1 and not claimed as delivered.

## Evidence limits

Runtime, editor and test assemblies compile with zero errors using Unity's installed compiler and cached reference assemblies. Unity tests passed: 7 EditMode and 5 PlayMode. The Windows build and installer were rebuilt. See VALIDATION.md for scope and remaining checks.

