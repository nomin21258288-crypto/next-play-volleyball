# Install and test on your Android phone (only possible after a REAL APK exists)
No APK is included in this package. If a cloud run succeeds you get `NextPlay-android-apk` under the run's **Artifacts**.
1. On the run page tap the artifact to download the ZIP; open it with Files and extract `NextPlay.apk`.
2. Tap the APK. Android asks to allow installs from this app (Chrome/Files): allow it, then Install. (It is debug-signed, which is fine for testing.)
3. Open the app and check each item. Tell me which fail:
   - [ ] Loading screen: 3D scene, six players, logo + tagline, progress goes 1% to 100%, never jumps backwards
   - [ ] X button (top right) skips to the lobby at any moment; PLAY stays disabled until loading really finishes
   - [ ] Smooth fade into the lobby; role labels under players
   - [ ] BRANDING OPTIONS: switching name/tagline changes the lobby text; choice remembered after restart
   - [ ] PLAY MATCH: stadium, court, net, ball, four characters with different faces
   - [ ] Touch controls: move, jump, serve, pass, set, spike, block work; no overlap with notch
   - [ ] Characters animate (run, jump, spike, block); MENU returns to the lobby
   - [ ] Frame rate feels smooth, phone does not overheat in 5 minutes
   - [ ] Launcher shows the chosen icon and label
