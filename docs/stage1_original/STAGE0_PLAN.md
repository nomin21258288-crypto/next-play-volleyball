# NEXT PLAY VOLLEYBALL — Stage 0 Plan (status: proposal, awaiting owner decisions)

## 1. Architecture recommendation
**Client: Unity (URP, C#, IL2CPP, Android ARM64).** Best fit for mid/low-end Android, large talent pool,
mature animation (Animator/blend trees), Addressables for asset preload. Unreal rejected for now: heavy APK, weak on low-end Android.
**Netcode:** Unity client runs a *prediction/visual* copy; **authoritative sim runs on server** (dedicated headless).
**Server:** Node.js (TypeScript) game server (WebSocket/UDP-style tick, 60 Hz sim / 20-30 Hz snapshots) + PostgreSQL + Redis.
Alternative: Nakama (open-source) for accounts/matchmaking/leaderboards/chat, with the match sim as a custom module.
**Sim core is engine-independent** (`packages/sim-core`, already started) so the SAME rules run on server and are ported/bound in the client.
Auth: Firebase Auth or Nakama auth (decision needed). Voice: Agora or Photon Voice (decision needed; paid, needs credentials).
Owner Panel: separate web app + separate admin API; game backend reads published config snapshots; panel off => last-known-good config.

## 2. Milestones (realistic)
- M0 Foundation: repo, CI, sim-core + tests (**started**), Unity project skeleton.
- M1 Local vertical slice (Unity, Android): court, net, ball (uses sim-core rules), 1 human + local bots, serve/pass/set/spike/block, point, set end, pause/settings.
- M2 Real-device test + performance profile (low-end Android).
- M3 Dedicated server runs same sim; 1v1 online with server authority, reconnect.
- M4 Accounts/UID/profile/lobby; 2v2, 6v6; bot AI; matchmaking.
- M5+ Stage 1-3 items (chat, voice, progression, tournaments, Owner Panel modules). Each is weeks, not one prompt.

## 3. Folder structure
```
nextplay/
  packages/sim-core/     engine-independent physics + rules + tests   <-- exists
  client-unity/          Unity project (Gameplay / UI / Net / Data assemblies)
  server/                authoritative match server, matchmaking, accounts API
  owner-panel/           optional separate web app + admin API (RBAC, MFA, audit log)
  config/                versioned, schema-validated game config (safe bounds)
  assets/                art pipeline, character references (identity-locked)
  docs/  tools/  ci/
```

## 4. Smallest vertical slice
One court, net, ball, 2 teams (1 human + bot per side), serve -> receive -> set -> spike/block -> point -> set end, camera, pause/settings. Local only first.

## 5. Owner Panel: configurable vs. locked
Configurable (bounded, versioned, rollback): announcements, events, tournaments, missions/rewards, shop catalogue, skins, bot difficulty, matchmaking thresholds, match duration, tuning within tested ranges, feature flags.
Server/code only: live scores, ranked results, anti-cheat, currency transactions, auth secrets, physics core, private player data.

## 6. Risks / needs from you
- No Unity/Android SDK/device in this sandbox: **no APK has been built or tested.** Needs a machine with Unity + Android SDK, and a real device.
- Brand conflict: Developer Master says NEXT PLAY; handoff says NEXORA PLAY. Pick one.
- 3D character likeness from 6 real photos needs a human-approved workflow (scan/artist); cannot be guaranteed by code.
- Costs: hosting, voice service, Apple/Google accounts, Unity licensing tier. Credentials must never be put in the app.
- Open: rules/rotation, control scheme, role-to-person mapping, economy, auth + voice providers.

## Test steps (sim-core)
`cd packages/sim-core && npm test` -> 8 tests (physics, determinism, net, serve->point, faults, set/match end).
