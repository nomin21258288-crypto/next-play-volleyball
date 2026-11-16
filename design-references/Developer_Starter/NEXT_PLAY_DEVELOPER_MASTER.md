# NEXT PLAY VOLLEYBALL GAME — Developer Master Instructions

## Mission
Build a real, playable, online 3D volleyball game. Do not deliver only concept art, static mockups, fake gameplay videos, or a UI prototype presented as a finished game. The game must use a real-time game engine, deterministic/authoritative match logic where appropriate, real volleyball movement and ball physics, networked multiplayer, persistent player accounts, and testable builds.

## Brand and visual identity
- Confirmed project name: **NEXT PLAY VOLLEYBALL GAME**.
- App icon: exactly 3 distinct player faces/characters, no duplicates.
- Loading / opening screen: exactly 6 distinct players. First supplied reference is front-most; second supplied reference is in position two; the two-person photo must be split into two separate people; use the remaining supplied references for the other positions.
- Never duplicate a person to fill a composition.
- Preserve identity from supplied reference photos. Use each photo only for its matching character. Do not invent, swap, merge, or duplicate faces. If the production pipeline cannot preserve identity reliably, use the original photo as a texture/reference or request a human-approved 3D likeness workflow; do not silently substitute a different face.
- Volleyball clothing only. Do not add invented jersey numbers. Use only user-approved real numbers.
- Loading screen includes brand, volleyball stadium, six-person lineup, close X, and a short progress bar. The app should preload efficiently; do not fake 100% while required resources are not ready.
- Keep app icon, store feature artwork, loading screen, and in-game lobby as separate assets/states.

## Core engineering requirement: real game, not AI-looking fake
- Use a production-capable 3D engine (recommend evaluating Unity or Unreal against target Android performance, team skills, and licensing before committing).
- Implement a real 3D court, net, ball, player rigs, animation controller, collision, gravity, jumping, serving, passing, setting, blocking, spiking, landing, and player repositioning.
- Ball trajectory and collisions must be driven by physics/gameplay code, not pre-rendered animation.
- Use animation blending and responsive controls. Animations must respond to actual player input and authoritative match state.
- Build genuine multiplayer networking with server-authoritative match state, anti-cheat validation, latency handling, reconnect, and match result verification.
- AI may assist developers with code, tests, documentation, placeholder art, and debugging. AI must not be used as a substitute for the actual game engine or real match simulation.
- Do not claim a feature is complete until it builds and passes a reproducible test.

## Delivery stages
### Stage 0 — Technical foundation
1. Propose engine and backend options with reasons and current constraints.
2. Define project folder structure, coding standards, version control, build pipeline, asset pipeline, and test plan.
3. Create a minimal playable vertical slice before adding all menus:
   - one court, net, ball, two teams, one human-controlled player per side or local test players
   - serve, receive/pass, set, spike, block, rally, score, set end
   - camera, basic UI, pause/settings
4. Build and run on a real Android device. Report exact tested device/build details and known limitations.
5. Keep a clean separation between gameplay, UI, networking, data/configuration, and admin tools.

### Stage 1 — Core game
- Login/account, UID, player profile and separate Lobby.
- Match formats: 1v1, 2v2, and 6v6.
- Ranked and Custom matches.
- Training/practice mode.
- Matchmaking, reconnect after connection loss, fair team assignment.
- Solo queue can fill empty slots with competent volleyball AI bots. Bots must serve, receive, pass, set, block, spike, rotate/reposition and respect rules; no idle placeholder bots.
- Settings: graphics, sound, controls, sensitivity, language.
- Friends, online status, invites, remove/block/report.
- Team text chat and team voice chat with mic/speaker toggles, mute, report, block, chat filtering, permissions and privacy controls. Voice must use a real supported voice service; do not fake microphone functionality.

### Stage 2 — Progression and community
- Trophy/score progression starts at zero, has no hard maximum, grows quickly at early levels and progressively slows at higher tiers.
- Original rank names and thresholds; do not copy another game's protected rank system.
- Global, Pakistan and supported regional leaderboards. Competitive rankings use verified online match results.
- Daily missions, rewards, shop, character skins and season pass.
- Pakistan-first launch. Design region configuration so more regions can be added later; do not claim unavailable servers are live.
- Lobby decorations and flags follow the selected supported region. Pakistan launch uses Pakistani flag assets.
- Profile is a separate player-information/statistics screen. Wall, Emotes and Rewards remain separate Lobby options.

### Stage 3 — Tournaments and operations
- Village/city/Pakistan tournament levels as configured and supported.
- Special Tournament mode is separate from Ranked progression. Tournament scores must not add Ranked trophies/points.
- Owner scheduling, tournament configuration and result management.
- In-game announcements and mail delivery to selected tournament participants.
- Live scoreboard is populated from authoritative match-server scores; Owner cannot edit live match scores.
- Owner-approved team/player showcase images for tournament venues.

## Owner Control Panel — configurable, modular, optional
Build the Owner Panel as a separate, secure module, not as a hard dependency of the client game. The game must continue to launch and play if the panel is disabled, removed, or temporarily unavailable. Use secure defaults and cached last-known-good configuration.

### Panel controls
- Game announcements, maintenance notices and event schedules.
- Tournament creation, schedules, brackets, participant management and tournament-only results.
- Rewards, daily missions, event missions, season content and shop catalogue configuration.
- Approved character skins, cosmetics, banners, stadium assets and lobby decorations.
- Configurable gameplay parameters only within safe, tested bounds: match duration, scoring/rules variants, cooldowns, bot difficulty, matchmaking thresholds and supported physics tuning.
- Character skill values and balance presets with versioning and rollback.
- Supported server/region status and matchmaking configuration.
- Moderation tools: reports, mutes, suspensions, bans, audit logs and appeals workflow.
- Feature flags to enable/disable optional modules safely.
- Diagnostics, crash/bug reports, server health, maintenance mode and release/configuration history.
- Backup/export/restore for configuration and content metadata.

### Panel security and separation
- Role-based access control: Super Owner, Owner, Moderator, Tournament Admin, Content Admin, Read-only support.
- Multi-factor authentication for privileged accounts where supported.
- Every privileged action logged with actor, timestamp, before/after values and reason.
- Require confirmation for destructive/high-impact actions.
- Never allow Owner Panel to alter authoritative live match scores, fabricate ranked results, bypass anti-cheat, expose private player data, or change client-side currency balances without validated server transactions.
- Do not put secret admin credentials or privileged keys inside the mobile app.
- API authorization and server-side validation are mandatory.
- Panel can be disabled by a secure deployment/configuration switch. Player login, matchmaking, gameplay, profile, and existing content continue using the game backend independently.
- If the panel is removed, retain safe server defaults and a documented manual deployment/configuration path.

## Quality and anti-cheat
- Server validates score, match result, rewards, ranked points, purchases and tournament outcomes.
- Validate movement, action timing, cooldowns and ball interactions to reduce cheating.
- Do not rely on client-submitted scores.
- Provide report/block/mute, chat moderation and privacy controls.
- Add crash logging, analytics with privacy safeguards, automated tests, load tests and device performance profiling.
- Test low-end Android devices and unstable mobile networks, not just high-end development machines.

## Required first response from the coding AI
Before writing a huge code dump, return:
1. Recommended engine/backend architecture and trade-offs.
2. A realistic milestone plan.
3. Folder/project structure.
4. The smallest playable vertical slice to build first.
5. What can be configured through Owner Panel and what must remain server/code controlled.
6. Risks, external services, costs/licensing considerations, and what requires human assets or credentials.
Then start implementing the first playable slice in small, buildable increments. Do not claim a full online game can be completed in one prompt.

## Acceptance criteria for the first playable slice
- Android build launches on a real device.
- Player can control movement and perform serve/pass/set/spike/block actions in a test match.
- Ball uses real-time physics and collisions.
- Rally and score update correctly under volleyball rules selected for the prototype.
- At least one complete point can be played from serve to point awarded.
- No fake buttons: every visible control works or is clearly marked as not yet implemented.
- Automated or repeatable test steps are documented.
- Build errors and known limitations are reported honestly.

## Open decisions
- Final engine and backend.
- Exact volleyball rules/rotation and control scheme.
- Voice provider and account authentication provider.
- Final mapping of six characters to gameplay roles (Smasher, All-Rounder, Defender, Blocker, Setter, Server are proposed archetypes).
- Economy, currencies, season length, store policies and regional rollout schedule.
Do not invent user approval for unresolved decisions.
