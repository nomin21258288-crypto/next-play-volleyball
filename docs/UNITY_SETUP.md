# Unity setup (NOT yet verified on a real Unity install)

## Requirements
Unity 2022.3 LTS (any 2022.3.x; Unity may offer to upgrade `ProjectSettings/ProjectVersion.txt`) with Android Build Support
(Android SDK/NDK + OpenJDK) if you want to build for Android. Built-in render pipeline is assumed (URP also works; the court
builder picks whichever lit shader exists).

## Open the project
1. Unity Hub > Add > select the `client-unity` folder. Let Unity import (it generates Library/ and the missing .meta/ProjectSettings files).
2. Package Manager should resolve `manifest.json` (Newtonsoft Json, Test Framework, uGUI). If it complains, add them manually.
3. If you see compile errors, please send them to me - the C# has never been compiled. Likely candidates are small typos or an asmdef reference name.
4. Menu **NextPlay > Create Foundation (prefabs + Court scene)** - creates materials, `PlayerPlaceholder_Team0/1`, `Ball`, and `Assets/Scenes/Court.unity`.
5. Open `Assets/Scenes/Court.unity`, press Play. Expected: court with net/poles/lines, 4 placeholder players, ball, on-screen joystick + buttons.
   Joystick moves H0; Serve when H0 is at the back line; Pass/Set/Spike/Block/Jump as in the layout. (Expected, not observed.)
6. Fallback: add an empty GameObject with `MatchController` to any scene; prefabs are optional (primitives are used).
   The HUD needs the old Input Manager (Project Settings > Player > Active Input Handling = Input Manager (Old) or Both).

## Run tests
- Unity: Window > General > Test Runner > EditMode > Run All (30 tests).
- Without Unity: install .NET 8 SDK, then `cd tools/SimTests && dotnet test` (28 tests; the 2 Unity-layout tests are excluded).
- JS (Stage 1 + fixtures): `npm test` at the repo root.
- After changing sim-core JS: `npm run golden` and commit the new `golden.json`; the C# parity tests must still pass or the port is out of sync.

## Android (later, only after the above works)
File > Build Settings > Android > Switch Platform; Player Settings: IL2CPP, ARM64, min API 24, landscape; set package id + keystore
(never commit keystores); Build. No APK exists in this package.
