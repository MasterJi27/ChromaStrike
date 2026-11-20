# Test procedure - Chroma Strike

After activating Unity, run `Tools/Run-Checks.ps1`. It runs EditMode logic tests and PlayMode GameplayTests, then builds Windows only if both suites pass. The suites cover initial distribution/positions, 5-second spawning, exact 5/2/3 replacement, ray hits/misses, expiry and restart. The expiry test also checks that the spawner component is disabled.

Manual acceptance:

1. Launch to the Start screen. Confirm title/Start button and no live round before Start.
2. Start: top-right counts must be 15/10/5 and top-center timer 02:00. Check WASD/mouse, sprint, obstacles and center-ray popping.
3. Wait five seconds and observe one new sphere and its counter increment.
4. Destroy 10 spheres with a known color mix. Confirm one 10-sphere replacement with that mix and batch progress reset.
5. Check repeated spawning for overlapping spheres/player and invalid spawn heights or positions.
6. Escape must free the cursor without pausing the timer. The recapture click should not shoot.
7. Let an actual 120-second round end. Confirm final live color counts, frozen movement/shooting/physics and no delayed spawn changes.
8. Restart three times: fresh 15/10/5, full clock, zero batch history and reset player pose.
9. Inspect Start, HUD and End at 1280x720, 1600x900 and 1920x1080; refresh screenshots.
10. Build the player, compile Installer/ChromaStrike.iss with Inno Setup, install into a clean directory, launch and replay. Verify optional desktop/Start Menu shortcuts and uninstall.

Current automated result: 7 EditMode tests, 5 PlayMode tests and 1 screenshot walkthrough passed. The Windows build succeeded without errors or warnings. See VALIDATION.md for the scope of visual and installer checks.
