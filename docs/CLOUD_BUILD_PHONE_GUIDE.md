# Cloud build from an Android phone (no PC)

Everything below is done in your phone's browser (Chrome). I verified the **files** here; I could NOT test GitHub or Unity from this session, so treat each step as "expected" until you see it work.

## Part 1 - run the tests (free, no Unity licence)
1. Create a free account at github.com. Tap + > New repository > name `nextplay` > **Private** > Create.
2. In the repo: Add file > **Create new file**. Name it exactly `.github/workflows/nextplay.yml` (typing the slashes creates the folders). Open `nextplay.yml` from the ZIP (`.github/workflows/`), copy all the text, paste, tap Commit changes.
3. Add file > **Upload files**: upload `nextplay_stage3.zip` itself (do not unzip). Commit.
4. Actions tab > **NextPlay** > Run workflow > task = **unpack** > Run. When green, the repo contains the whole project. (If Actions asks to be enabled, tap enable.)
5. Actions > NextPlay > Run workflow > task = **tests**. This runs the 25 JS tests, the config check and the C# tests with .NET. Open the run: green = passed, red = open the failing step. The log is also in the run's Artifacts (`test-logs`).
   Settings > Actions > General > Workflow permissions must be **Read and write** for step 4.

## Part 2 - Unity compile + EditMode tests + APK (needs a Unity licence)
The pipeline uses GameCI (Unity 2022.3 in Docker on GitHub's servers). **The licence step is the uncertain part:**
- GameCI's classic method needs a `.ulf` licence file, normally made with Unity Hub on a computer, which you don't have.
- GameCI documentation (checked 2026-11-15) says Unity Personal activation for CI is currently unreliable and a new email/password method was only recently added; I could not confirm it works.
So pick one:
- **Option A (try first, no licence file):** Unity Build Automation at cloud.unity.com (Unity's own cloud, runs in the browser). Sign in with a Unity ID, create a project, connect your GitHub repo, add an **Android** build target, Unity 2022.3, and set the pre-build "build method" to `NextPlay.EditorTools.ProjectConfigurator.Apply` if asked. I could not verify whether your plan includes it or its minutes; check on the page before relying on it.
- **Option B:** GameCI on GitHub. Repo Settings > Secrets and variables > Actions > add `UNITY_LICENSE` (contents of a .ulf) **or** `UNITY_EMAIL` + `UNITY_PASSWORD`, then run NextPlay with task = **unity**. Without a secret the job fails on purpose with a clear message (it never fakes a pass). Someone with a PC or Mac can generate the .ulf in a few minutes if you can borrow one once.
- If Unity says the Docker image for `2022.3.62f1` doesn't exist, edit `client-unity/ProjectSettings/ProjectVersion.txt` to another 2022.3 version listed at game.ci/docs/docker/versions.

## What to send back to me
Any red step: copy the red error lines (C# compile errors show as `error CSxxxx`) and send them to me. I'll fix them. Expect a first-run compile error or two.
