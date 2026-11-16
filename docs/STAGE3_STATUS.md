# Stage 3 status  (2026-11-15)

**Bottom line:** Stage 3 code, assets, configs, tests and a cloud pipeline are written. **Nothing has been compiled in Unity, no C# test has run, and no APK exists.** The first real compile happens in the cloud job (see CLOUD_BUILD_PHONE_GUIDE.md). Expect to fix a few compile errors on the first run: this code was written without a compiler.

## Preserved (nothing removed)
All Stage 1 + Stage 2 files, the C# sim, physics, rules, Bot AI, JS reference, 25 JS tests, 30 C# tests, touch layout. Original design files are in `design-references/` untouched (`Handoff_latest_starter`, `Developer_Starter`, `Handoff_first_package`). Stage 1 docs are in `docs/stage1_original/`.

## Added
| Area | Files | Notes |
|------|-------|-------|
| Pure core (testable without Unity) | `Scripts/Core/BrandingConfig.cs, CharacterRoster.cs, LoadingFlow.cs` | branding choices, six-character rules, real-progress loading state machine |
| Loading screen | `App/IntroScreen.cs, GameBootstrap.cs, ArenaCamera.cs` | live 3D scene behind the UI: six characters, court, net, stadium, spinning ball; real weighted progress 1-100%; X button works any time; fade to lobby |
| Lobby | `App/LobbyScreen.cs` | PLAY MATCH (enabled only when loading really finished) and in-app BRANDING OPTIONS (name/logo/tagline/icon), saved per phone. No fake buttons |
| Characters | `App/CharacterFactory.cs, CharacterRig.cs` | primitive 3D body, procedural animation (idle, run, jump, serve, pass, set, spike, block), own-photo face on a head cap |
| Stadium / ball / perf | `App/StadiumBuilder.cs, BallFactory.cs, PerfProfile.cs, ShaderLib.cs` | crowd tiers, brand ad boards, light rigs, procedural volleyball, Android quality profile |
| Branding config | `config/branding.json` (+ copy in `Assets/Resources/Config/`) | edit it, no code change |
| Face textures, icons | `Assets/Resources/Characters/*.png`, `Assets/Branding/Icons/*.png`, `tools/make_branding_assets.py` | made only by cropping + soft mask of your original photos |
| Editor / CI | `Editor/ProjectConfigurator.cs`, `.github/workflows/nextplay.yml`, `Assets/Scenes/Boot.unity` | applies name, package id, icon, IL2CPP/ARM64, shader list from config |
| Hooks in existing code (additive, defaults keep Stage 2 behaviour) | `PlayerView.cs`, `MatchController.cs` | VisualFactory, ExtraEnvironment, OnExit (MENU button), rig driver interface |
| **Stage 2 Android bug fixed** | `ControlLayout.cs` | it read StreamingAssets with File.ReadAllText, which cannot work inside an APK; now reads a Resources copy first |

## Decisions I did NOT make for you (preserved for approval)
1. **Game name** - three options kept; default is provisional. Newest handoff says NEXORA PLAY VOLLEYBALL GAME, the Developer Master says NEXT PLAY VOLLEYBALL GAME, the first package only gives NEXORA PLAY.
2. **Logo** - only a generated text lockup exists. The banner PNG is a concept mock-up, not production art. A `final_vector_logo` slot is reserved (disabled).
3. **Tagline** - three options kept.
4. **App icon** - two options generated (volleyball+net mark, three faces). The handoff documents disagree on which is intended.
5. **Role labels** (Smasher, All-Rounder, ...) - proposed only; person-to-role mapping unapproved. No jersey numbers (none supplied).
6. **Android package id** `com.nexoraplay.volleyball` is a placeholder; it cannot change after a store release.
7. **Orientation** - the loading banner brief says 9:16 portrait but the Stage 2 controls are landscape. The app is landscape; the loading camera adapts to any aspect. Say if you want a portrait loading screen.

## Known limits / not done
- Faces are flat photo projections on a head sphere, not real 3D likenesses; 3/4-angle photos (characters 3-6) look turned on a front-facing head. Photo backgrounds leave a faint halo. A proper likeness needs your approval of an approach.
- Characters are simple primitive-built bodies, not modelled/rigged meshes.
- Ball physics are the existing Stage 2 sim (unchanged). Nothing new was claimed or tested there.
- Pakistan-flag decorations, flags, jersey crest, audio, store art: not done.
- Stadium ad-board text updates on next launch after a name change (lobby title/tagline update instantly).
- Performance on Android is untested; PerfProfile is a reasoned default only.
- Android build is debug-signed (no keystore): installable for testing, not store-ready.
