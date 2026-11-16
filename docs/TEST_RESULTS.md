# NEXT PLAY - Stage 2 test results

Environment: Linux sandbox, Node 22. **No Unity, no .NET SDK, no mono/csc, no Android SDK, no device.**

## Executed and passed
| Suite | Command | Result |
|-------|---------|--------|
| Stage 1 sim-core (JS) | `npm test` (root) | 15 / 15 pass |
| Stage 1 tutorial-core (JS) | `npm test` (root) | 10 / 10 pass |
| Golden fixtures in sync with JS reference | `node tools/check_golden.mjs` | "up to date" |
| C# file bracket/paren balance | python script | 20 / 20 files balanced (this is NOT a compile) |
| C# rules-test expectations cross-checked against the JS reference | ad-hoc node script | set/match/deciding-set and block-not-a-touch scenarios behave as the C# tests assert |

Total executed tests: **25 / 25 passing** (identical to Stage 1; none removed).

## Written but NOT run (could not run here)
| Suite | Tests | Why not run | How to run |
|-------|-------|-------------|------------|
| SimPhysicsTests (incl. 3 golden parity tests) | 8 | no C# toolchain | Unity Test Runner or `dotnet test` |
| SimRulesTests | 9 | " | " |
| SimGameplayTests | 9 | " | " |
| GoldenParityTests (3 all-bot matches + 1 scripted-human match, event-by-event) | 2 | " | " |
| ControlLayoutTests (Unity only) | 2 | needs UnityEngine | Unity Test Runner |
| **Total C#** | **30** | | |

## Not tested at all
Unity compilation, scene/prefab generation, touch controls, rendering, camera, Android build, on-device performance.
Nothing in this package should be considered working until `dotnet test` / Unity Test Runner report green.

## What a green C# run would prove
That the C# sim reproduces the JS reference match-by-match (events, timing, final positions) for 4 recorded scenarios and obeys the unit tests.
It would NOT prove the Unity scene, controls or feel are correct.


---
# STAGE 3 RESULTS (added; everything above is unchanged)
Date of this run: 2026-11-15. Environment: cloud Linux container with Node 22 only. **No .NET SDK, no Unity, no Android SDK, no network for the shell.**

## Actually executed (real output saved in docs/logs/js-test-log.txt)
| Check | Command | Result |
|-------|---------|--------|
| JavaScript sim-core tests | `npm test` | 15 / 15 pass |
| JavaScript tutorial-core tests | `npm test` | 10 / 10 pass (**25 / 25 total, all existing JS tests**) |
| Golden fixtures vs JS reference (simulation parity inputs) | `node tools/check_golden.mjs` | "up to date" |
| Stage 3 config check (branding options, six characters, face textures, icons, config copies in sync) | `node tools/check_config.mjs` | OK: 3 name options, 3 taglines, 2 icon options, 6 characters |
| Static sanity of every new/edited .cs file (lexer errors, brace/paren balance) | python + pygments | 21 / 21 files clean. **This is NOT a compile.** |

## Written but NOT run (no toolchain here)
| Suite | Tests | Runs where |
|-------|-------|-----------|
| Existing C# sim tests (Physics 8, Rules 9, Gameplay 9, Golden parity 2) | 28 | `dotnet test` (GitHub Actions job `tests`) or Unity |
| Existing ControlLayoutTests (Unity only) | 2 | Unity EditMode (job `unity`) |
| **Existing C# total** | **30** | |
| NEW Stage3Tests: BrandingConfig 11, CharacterRoster 8, LoadingFlow 11 | 30 | `dotnet test` and Unity EditMode |

## NOT done at all (honest list)
- Unity compile of the project: **not performed**. No compilation log exists. Do not treat any C# as compiled.
- Unity EditMode run: **not performed**.
- Android APK: **not built. No APK is included in this package.**
- Simulation parity (C# vs JS): the fixtures are current, but the C# side has **not been executed**, so parity is **unverified**.
- Visual check of the 3D loading screen, characters, stadium, touch HUD and frame rate: **not performed** (nothing could be rendered).
- PlayMode tests: none written; the 3D scene needs a manual check on the phone (see docs/ANDROID_INSTALL_TEST.md).

## Create Room scoring (custom target score 7..25) - added after Stage 3
**Run and passed (real output):** `npm test` -> sim-core 52/52 (37 of them new, `packages/sim-core/test/scoring.test.js`) and tutorial-core 10/10. Covers: every whole number 7..25 accepted; 6, 26, 0, -1, 7.5, NaN, "15" rejected; a match ends exactly at the chosen N (all 19 values); win-by-2; optional 2-set mode; ranked stays 25/25/15 and rejects any custom target; only the creator of an unstarted custom room can change the score; full bot-played 1v1, 2v2 and 6v6 matches to 7, 12 and 25.

**Written but NOT compiled or run:** C# port (`Rules.cs`, `Game.cs`, `Bot.cs`, `RoomConfig.cs`), `Tests/EditMode/ScoringTests.cs` (mirror of the JS tests, 28 cases with parameterised targets), and UI wiring (`LobbyScreen` CREATE ROOM panel, `GameBootstrap`, `MatchController` scoreboard line). Only a brace/lexer check was done. Run `dotnet test tools/SimTests` or the GitHub Actions workflow.

**Assumptions:** custom room = single game to N, win by 2, no cap; 6v6 is functional (new formation table) but not tuned; the UI is not visually checked.
