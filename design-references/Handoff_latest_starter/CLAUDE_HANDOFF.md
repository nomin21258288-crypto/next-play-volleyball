# NEXORA PLAY — Volleyball Game Development Handoff

## Purpose
This package is the reusable project brief and source-reference bundle for Claude AI (or another development assistant). Treat the supplied photographs as identity references. Do not regenerate, beautify, replace, or reinterpret faces.

## 1. Brand and naming
- Website/platform brand supplied by the user: **NEXORA PLAY**.
- Confirmed game name: **NEXORA PLAY VOLLEYBALL GAME**. Brand lockup: **NEXORA PLAY** with **VOLLEYBALL** prominent and **PLAY ELEVEN** smaller beneath it.
- The user has confirmed the name **NEXORA PLAY VOLLEYBALL GAME**. The final vector logo artwork still needs production and approval; do not rename the game.
- Logo direction: premium volleyball identity; use a volleyball, net, court/stadium geometry, and energetic player silhouette/shape. No guns, military imagery, black tactical suits, parachutes, or unrelated combat visuals.
- Preferred visual orientation for character/loading artwork: **9:16 portrait**.

## 2. Character lineup — exactly six
Use the files in `Character_References/`. Numbering is authoritative:
1. `01_main_front_reference.png` — main/front-most player. This is the first and most prominent character.
2. `02_second_position_reference.png` — second character, placed in the second position on either left or right side as composition requires.
3. `04A_two_person_photo_left_player_crop.jpg` — the left person from the original two-person photo, cropped as a separate reference.
4. `04B_two_person_photo_right_player_crop.jpg` — the right person from the original two-person photo, cropped as a separate reference.
5. `05_cream_kurta_reference.png` — fifth character reference.
6. `06_circular_portrait_reference.png` — sixth character reference.
- `03_two_players_original.jpg` is included as the unmodified source for the two separate crops.
- Show exactly six players in the hero lineup: one main player in front and five teammates behind/around him, all visible as a team. Do not add extra foreground people.
- Build realistic, high-quality 3D animated game characters based on the real photographs. The body, uniform, pose, lighting, and rendering may be adapted for the game, but each person's face identity must remain recognizably faithful to the source.
- **Face lock is critical:** preserve face shape, eyes, nose, lips, ears, skin tone, hairline, hairstyle, facial hair, and distinguishing proportions. No face swapping, no mixing identities, no AI-invented faces, and no changing one person's face to resemble another. Do not apply filters that alter identity.
- Keep each character linked to their own correct source image. Do not use the two-person photo as one combined character.

## 3. Volleyball roles / skill-based labels
The user wants character names/labels to be based on volleyball skills, not arbitrary names. Use these six role labels as the initial proposed roster:
1. Smasher
2. All-Rounder
3. Defender
4. Blocker
5. Setter
6. Server
These are gameplay archetypes/role labels, not confirmed personal names. Match each character's in-game skill kit to the label. The user specifically gave “Smasher” and “All-Rounder” as examples. Keep skill balance fair; no role should be unbeatable.

## 4. App icon, store artwork, logo, opening banner and loading screen
- Use `NEXORA_PLAY_Volleyball_Loading_Banner_Sample.png` as the visual direction/sample, not as final production artwork.
- Create a separate square app icon (1024x1024 source) that remains readable when displayed small: a distinctive volleyball + net/court mark, with a compact NEXORA PLAY identity. Avoid tiny text in the icon.
- Create a matching store feature/banner graphic and in-game loading screen using the same logo, colors, and three-character hero composition.
- For the three-character hero sample: main first white-kurta reference in the center/front; use the selected two side-player references from the supplied photos on the left and right. Exactly three characters in this app/store hero artwork. Other game screens may show the full six-player roster.
- Preserve each selected person's own face identity. The generated sample is only a layout/style guide; recreate the final art using the original reference photos and manually review faces. Never treat an AI approximation as proof of exact identity preservation.
- Continue with the opening/loading requirements below.

## 4A. Opening sequence and loading screen
- On app launch, show a dedicated full-screen 9:16 team intro/loading banner.
- Stadium/interior volleyball arena background, court markings, net, volleyball, dramatic sports lighting.
- Six 3D characters arranged as specified above, with character 1 clearly frontmost and character 2 second in the lineup.
- Brand text: **NEXORA PLAY**; smaller line: **PLAY ELEVEN** (proposed placement beneath the brand).
- Add a short volleyball-themed tagline such as **“RISE. SET. SMASH.”** or **“THE VOLLEYBALL ARENA.”** Treat tagline as design copy pending user approval.
- Add a loading indicator at the bottom showing 1%–100%, with a smooth progress bar and percentage.
- Loading should complete within a few seconds on a normal supported device when assets are already cached. Do not fake completion while required assets are still unavailable; use asset preloading, compressed textures, and asynchronous loading.
- A clear **X / Close** button appears at the top. When tapped, close the intro banner and continue to the next game view/lobby. The banner must not trap the player.
- Avoid unnecessary long loading. Show a responsive progress state and handle slow network gracefully.

## 5. Main game direction
- Genre: online multiplayer volleyball game, not a shooter or military game.
- Initial server rollout: Pakistan only. Architect region/server configuration so other regions can be added later through updates; do not claim every region is available at launch.
- Core match formats discussed: 1v1, 2v2, and 6v6. Ranked and Custom match experiences are part of the plan.
- Solo matchmaking should be able to fill missing team slots with capable AI teammates/opponents when needed. In 6v6, AI must perform real volleyball actions: receive/pass, set, block, smash, serve, and reposition—not stand idle.
- Training/practice mode, daily missions, friends/invites, online status, reconnect after internet disconnection, and game settings are required features.
- Settings include graphics, sound, controls, sensitivity, and language.
- Team voice chat and team text chat were approved as planned features, with microphone/speaker toggles, mute, report, block, chat filtering, and privacy controls. Proximity voice chat was only a later possibility, not a launch commitment.

## 6. Lobby, profile, and navigation
- Keep the Lobby and Profile as separate navigation destinations.
- Profile opens as a dedicated player-information/statistics screen only.
- Lobby options such as Wall, Emotes, and Rewards remain separate Lobby features; opening Profile must not automatically open or embed those features.
- Lobby environment should use volleyball court/stadium elements and region-appropriate decorations. Pakistan launch environment uses Pakistani flag decorations. Future regional environments can be added with later server updates.
- Character skins/uniforms and customization should fit volleyball. Do not use combat/military outfits.

## 7. Progression, ranks, seasons, and leaderboards
- Trophy/score progression starts at zero and has no hard maximum. Early progression should feel accessible, then higher tiers should require increasingly more effort.
- Trophy totals must continue increasing even at very large values. Allow multiple small trophy icons/badges to accumulate/display, while the main trophy can grow in visual size/prestige at milestones.
- Use original rank names and thresholds rather than copying another game's protected rank system.
- Include online UID/profile, global/Pakistan/regional rankings, season rankings, and real online-match results only for competitive leaderboards.
- Pakistan location hierarchy discussed: village, city, district, Pakistan. Village selection should come from a predefined list. Location visibility requires player permission.
- Season-linked rankings were discussed with a six-month season period. Season banners: Season 1 top banner and Season 2 lower banner were accepted as a visual concept; future season banners should be supported. Banners need a close X.

## 8. Tournaments, announcements, and scoreboards
- Support village/city/Pakistan tournament levels as planned.
- Special Tournament mode is separate from Ranked progression: it may use Ranked-style gameplay/scoring, but tournament results must not add Ranked trophies/points.
- Owner schedules/runs tournaments and controls tournament settings/results.
- Owner can upload approved winner/team showcase images (team/player photos, character images, or player ID/profile images) for tournament venue/stadium/village backgrounds.
- In-game automatic announcements and scoreboards should work across Ranked, Custom, Tournament, and regular matches.
- Owner can create/publish global and tournament announcements. Tournament notices can be public and delivered to selected participants' mailboxes.
- Live scoreboard must use authoritative game-server scores, not editable Owner-entered values. Show both team names/logos, player names and UIDs, live team scores, per-set scores, current set/status, and leading team.

## 9. Owner/admin and safety systems
- Owner/Super Owner role and UID-based administration were part of the project plan.
- Keep admin tools separate from player-facing controls. Important match results and live scores should be server-authoritative.
- Include privacy controls, reporting/blocking, chat filtering, reconnect handling, and anti-cheat protections.
- Do not expose private player information or precise location without permission.

## 10. Deliverables requested from the development AI
1. Preserve this handoff and all six supplied character references.
2. Produce a 9:16 loading-screen/banner concept and implementation-ready layout.
3. Produce a logo concept using NEXORA PLAY with smaller PLAY ELEVEN beneath it, pending final approval.
4. Create six separate 3D character models/skins faithful to the supplied identities and the six volleyball role labels.
5. Keep the opening banner, Close X behavior, loading progress, and transition to Lobby as separate, testable UI states.
6. Provide a clear project structure, asset naming, implementation plan, and runnable code/build instructions where the target engine is known.
7. Never claim that a feature is implemented or tested unless it actually is.

## 11. Items still requiring user approval / technical decisions
- Final game title and final logo.
- Whether the tagline is “RISE. SET. SMASH.” or another phrase.
- Final mapping of each of the six people to the six skill roles (the role labels above are proposed starting labels).
- Game engine, backend, hosting, database, and exact matchmaking architecture.
- Volleyball rules/physics tuning, skill cooldowns, currencies/shop economy, and final season schedule.
- Exact visual design and placement of lobby/profile screens.
Do not invent user approval for these unresolved items. Ask only when implementation genuinely requires a decision.

## 12. Non-negotiable visual rules
- Six players exactly.
- First reference is the main front character.
- Second reference is second in the lineup.
- Split the two-person photo into two separate player references.
- Preserve all real faces; do not change identity.
- Volleyball-only sports theme: ball, net, court, stadium, players.
- 9:16 portrait for the loading artwork.
- No fake jersey/chest numbers: do not add numbers unless the user supplies/approves the real numbers. If numbers appear in any generated draft, remove them.
