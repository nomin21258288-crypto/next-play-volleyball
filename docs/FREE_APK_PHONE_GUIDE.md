# Free Android APK build - phone only, zero cost

**Status: NO APK HAS BEEN BUILT.** Nothing here was run against Unity. The workflow files are prepared and YAML-checked only. An APK exists only after a run of "NextPlay Android APK (free)" finishes green AND the run page shows the artifact `NextPlay-android-apk`.

## 1. What is free (checked 15 Nov 2026 against the linked docs)
| Item | Free terms | Source |
|---|---|---|
| GitHub Actions, GitHub Free | $12/month of included compute (about 2,000 Linux minutes), 500 MB artifact storage, 10 GB cache. When used up, runs are **blocked** unless a payment method is on file. Do not add a card and you cannot be charged. | docs.github.com/en/billing/concepts/product-billing/github-actions |
| Public repositories | Standard runners are free and do not use your included minutes | same page |
| Unity Personal | Free (revenue/funding under Unity's limit). Licence is the hard part, see section 3 | game.ci/docs/github/activation |
| GameCI `unity-builder@v4` | Free, open source. Docker images carry Unity 2022.3 + Android SDK/NDK; the project uses `2022.3.62f1`. **I could not confirm that the exact image tag for 2022.3.62f1 exists**; if the run says the image is not found, change `ProjectVersion.txt` to a 2022.3 version GameCI lists on hub.docker.com/r/unityci/editor/tags | not verifiable from here |
| Android SDK | Nothing to install: it is inside the GameCI Android image | game.ci |

Public vs private repo: a **public** repo costs no minutes but exposes everything, including `design-references/` (photos of real people) and the face textures. Use a **private** repo (about 2,000 free minutes). Estimate, not measured: a first IL2CPP Android build is probably 25-50 minutes, later ones less with the Library cache, so roughly 40-80 builds per month at best. Check Settings > Billing > Usage on GitHub.

## 2. One-time setup (phone browser, github.com in "Desktop site" mode helps)
1. Create a free GitHub account and a **private** repository (empty).
2. Add file > Upload files: upload `nextplay_stage3_scoring.zip` renamed to `nextplay_stage3.zip`. Also create `.github/workflows/` and upload the two `.yml` files from this ZIP there (GitHub cannot unpack workflow files for you). Easiest: open `nextplay.yml`, `android-apk-free.yml` and `unity-license-request.yml` in the ZIP and paste each into "Add file > Create new file" at `.github/workflows/<name>.yml`.
3. Actions > NextPlay > Run workflow > task `unpack`. This unzips the project into the repo.
4. Actions > NextPlay > task `tests`. Free, needs no Unity licence. Read the result honestly.

## 3. The Unity licence - the real obstacle (phone-only)
GameCI needs three repo secrets (Settings > Secrets and variables > Actions): `UNITY_LICENSE` (contents of a `.ulf` file), `UNITY_EMAIL`, `UNITY_PASSWORD`. Use a dedicated free Unity ID.

The problem: Unity says manual activation is **no longer supported for Personal licences**, and a `.ulf` normally comes from the Unity Hub desktop app (Preferences > Licenses > Add > Get a free personal license). You have no computer. Routes, honest odds:

- **Route A - Unity Hub in a free cloud desktop (best chance, unverified).** GitHub Codespaces gives free accounts 120 compute hours/month and 15 GB-months of storage, and **blocks** use after that if you have no card. Open a Codespace with a browser desktop (devcontainer `desktop-lite` feature), install Unity Hub for Linux, sign in, activate Personal, then copy `~/.local/share/unity3d/Unity/Unity_lic.ulf` into the `UNITY_LICENSE` secret. I did NOT test this, and I did not write scripts for it because I cannot run them. Delete the Codespace afterwards.
- **Route B - manual activation page from a phone.** Run `Unity licence request file (optional)`, download the `.alf`, upload at license.unity3d.com/manual. The Personal option is hidden on that page; reports say you must remove a `display: none` style, which is awkward on a phone, and GameCI issue 408 says this is brittle and was last confirmed in Aug 2025.
- **Route C - email + password only.** GameCI's CLI merged a headless Personal activation on 5 Sep 2026, but its own author says it was never run against a real Unity account, it cannot handle 2FA, and it is not confirmed for the `unity-builder` action. Do not rely on it.
- **Route D - a friend with a PC** activates once and gives you the `.ulf` text. Two minutes of their time, no cost.

If none works, no free Unity cloud build is possible. I will not fake one.

## 4. Build the APK
Actions > **NextPlay Android APK (free)** > Run workflow (leave tests off for the first try). When it is green, open the run page, scroll to **Artifacts**, tap `NextPlay-android-apk` (a zip containing `NextPlay.apk`). The run summary shows size and sha256. The workflow fails if no real APK (over 1 MB, with `libil2cpp.so` for arm64) exists.

## 5. Install on your phone
Unzip the download (Files app), open `NextPlay.apk`, allow "Install unknown apps" for that app when asked. It is debug-signed: fine for your own phone, not for Google Play. Then follow `docs/ANDROID_INSTALL_TEST.md`. Artifacts are kept 7 days.

## 6. If it fails
Open the failing step's log. Common: licence error (section 3), image tag not found (section 1), out of disk, a C# compile error (the C# has never been compiled; expect to fix some). Send me the log text and I will fix the code.
