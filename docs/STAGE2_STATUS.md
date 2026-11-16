# NEXT PLAY - Stage 2 status (honest)

Legend: TESTED = actually executed here and passed. WRITTEN = code exists, never compiled/run (no Unity, no .NET in this sandbox).

| Area | Status | Notes |
|------|--------|-------|
| Stage 1 JS (sim-core, tutorial-core) | TESTED | 25/25 pass after the hypot change; nothing deleted |
| Golden fixtures from JS | TESTED | generated; `check_golden` confirms they match the current JS sim |
| Unity project skeleton (folders, asmdefs, manifest, ProjectVersion) | WRITTEN | ProjectSettings is minimal; Unity will regenerate the rest |
| C# sim port: physics, planner, rules, game, bots | WRITTEN | 623 lines; **never compiled**. Brace/paren balance checked only |
| C# tests (30) incl. JS-parity replay | WRITTEN | **0 executed** (see TEST_RESULTS.md) |
| 3D court: ground, net+poles, lines, two team areas, ball/player spawn markers, lighting | WRITTEN | generated at runtime by CourtBuilder; never viewed |
| Camera | WRITTEN | fixed behind team-0 baseline with slight z-follow |
| Player placeholder: movement, jump, rotation, team identity, animator hooks | WRITTEN | capsule + facing marker; Animator parameters driven only if a controller is assigned |
| Touch controls: joystick, jump, serve, pass, set, spike, block, pause | WRITTEN | built from shared layout; never touched on a device/editor |
| MatchController (fixed 120 Hz sim, human H0 + 3 bots, score HUD) | WRITTEN | |
| Editor builder (prefabs, materials, scene) | WRITTEN | |
| Android APK | NOT DONE | none exists |

## Known gaps / risks
- Compile errors are likely somewhere in ~1,400 lines of never-compiled C#. Expect a fix round after first import.
- Parity relies on IEEE double math; Mono/IL2CPP on ARM may fuse multiply-add and drift in the last bits. Desktop `dotnet test` should be exact (tolerance 1e-9);
  a long trace could diverge on-device. Not an issue for server-authoritative play, but do not use lockstep.
- The practice-tutorial driver (`tutorial-core/src/practice.js`) and tutorial UI are NOT ported to C#. Only the sim is.
- Two parity-sensitive spots: `Math.Sign` throws on NaN (JS returns NaN); C# `Rules.OnLanded` would index out of range if `LastTeam == -1` (cannot occur after a serve).
- Placeholder visuals only: no final characters, textures, audio, animation clips, or UI art. Joystick/buttons are flat boxes with text.
- HUD scaling (dp->px, safe area) is a first attempt and unverified on real phones/notches.
- Some tests are shallow: `BotsSpikeAndBlockInRealRallies` only counts actions; `NoUnityTypesLeakIntoSim` checks public fields only.
- Team 1 has no second human; no rotation; rules are still the Stage 1 prototype defaults.
