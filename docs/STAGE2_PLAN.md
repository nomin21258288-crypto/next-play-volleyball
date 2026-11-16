# NEXT PLAY - Stage 2 plan: Unity 3D foundation + C# gameplay port

## Goals (from the Stage 2 brief)
1. Keep Stage 1 intact (25 JS tests, all files). 2. Unity project structure. 3. Port sim-core to C#, with tests.
4. Initial 3D court. 5. Placeholder player prefabs. 6. Touch controls from the shared layout. 7. Honest testing. 8. Docs + ZIP.

## Design decisions
- **Two C# layers.** `NextPlay.Sim` is pure C# (asmdef `noEngineReferences`), a line-by-line port of sim-core using `double`
  and the same operation order. Unity code (`Gameplay`, `Controls`) only mirrors sim state into views and feeds input.
  Unity physics is NOT used for the ball/players: the sim stays the single source of truth (needed for server authority later).
- **JS stays the reference.** `tools/gen_golden.mjs` records physics, planner results and full-match event traces from the JS sim into
  `golden.json`; C# tests replay the same inputs and compare event-by-event. `tools/check_golden.mjs` keeps the fixture in sync.
- **One tiny JS change for parity:** `Math.hypot` replaced by `sqrt(a*a+b*b)` (exactly rounded in both languages). Stage 1 tests still 25/25.
- **Tests run two ways:** Unity Test Runner (EditMode) and plain `dotnet test` (tools/SimTests) so the sim can be verified without Unity.
- **Generated assets:** prefabs/scene/materials are produced by an editor menu (`NextPlay > Create Foundation`) via Unity APIs,
  because hand-written .prefab/.unity YAML would be unreliable.
- **Shared layout:** HUD is built from `controls.layout.json` (same file the JS tests validate) and registers each control with the
  Stage 1 tutorial `ControlRegistry`, so tutorial arrows will point at the real buttons.

## Work breakdown
| # | Item | Output |
|---|------|--------|
| A | Unity folder/project skeleton, asmdefs, manifest | client-unity/ |
| B | C# port: physics, planner, rules, game, bots | Assets/Scripts/Sim |
| C | Golden fixtures + parity tests + unit tests | tools/, Assets/Tests/EditMode |
| D | Court, camera, lighting, player placeholder, ball | Assets/Scripts/Gameplay |
| E | Touch controls from shared layout | Assets/Scripts/Controls |
| F | Editor builder for prefabs + scene | Assets/Editor |
| G | Docs, ZIP | docs/, nextplay_stage2.zip |

## Out of scope for Stage 2 (planned later)
Practice-tutorial port to C#, tutorial UI rendering, final characters/animations, audio, networking/server, accounts, Owner Panel, APK.
