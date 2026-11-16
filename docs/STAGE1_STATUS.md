# NEXT PLAY - Stage 1 status (honest report)

## Completed AND tested (Node 22, `npm test` at repo root: 25/25 pass)
- sim-core gameplay (engine-independent): movement, jump physics, serve/pass/set/spike/block with reach/airborne/cooldown
  validation, 120 Hz deterministic ball physics, shot planner using the real physics, rules (rally point, 3 touches,
  block not counted, faults, sets to 25 win-by-2, best of 3), seeded bots that serve/receive/set/spike/block/reposition.
  A full 2v2 all-bot match completes (86 serves, 54 blocks, 85 points in one run).
- tutorial-core: shared control layout (validated on 5 screen sizes incl. notch), en+ur localisation (RTL flag, fallback,
  new language = new JSON), first-login logic, per-tutorial per-account progress (survives re-login in file test),
  Next/Previous/Skip/Practice Again/replay, map/room guide registry (any new map auto-gets a guide, once per map,
  suppressed for experienced players / hints off), practice match with 1 human + 1 bot per team where all 7 steps
  complete from real game events.

## NOT done / NOT verified
- **No Unity project, no APK, no device test.** The C# in client-unity/Assets/Scripts/Tutorial was never compiled.
- sim-core is JavaScript. Unity needs a C# port of sim-core (or a server-side sim). Not started.
- No 3D court/characters/animation/camera/HUD/pause/settings UI. No animated arrows, hand-pointer art or demo replays
  (data and targeting exist; rendering does not). No Urdu font/RTL layout work in Unity.
- Practice match "human" was an autopilot in tests; real touch control feel is unverified.
- Team 1's "human" slot is a bot (no second local player yet). No rotation. Rules are my prototype defaults.
- Backend endpoints for account-stored progress not built (JSON-file adapter only, for tests/dev).
- Weak tests: 'out-of-reach pass fails' and 'block not a touch' are loose. Bot skill/rally length are only roughly tuned.
- Net contact model is simple; no player-vs-ball collision, only reach checks.

## Run tests
`npm test` (Node 18+; no dependencies).

## Open the Unity project (when it exists)
1. Install Unity Hub + Unity 2022 LTS or newer with Android Build Support (SDK/NDK/OpenJDK).
2. Create a project in `client-unity/` (URP 3D), add package `com.unity.nuget.newtonsoft-json`.
3. Keep Assets/Scripts/Tutorial and Assets/StreamingAssets/controls.layout.json (copy from /config when it changes).
4. Port sim-core to C# and run its 25 tests' scenarios as Unity Test Runner tests.

## Android APK build (when the project exists)
File > Build Settings > Android > Switch Platform; Player Settings: IL2CPP, ARM64, min API 24; set package id and
keystore (never commit it); Build. Install with `adb install -r game.apk`. Record device model + OS + build number
in the test report before calling it tested.
