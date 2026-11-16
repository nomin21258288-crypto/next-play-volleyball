// UNTESTED in Unity. App entry point (starts itself in the "Boot" scene, so the scene file can be empty).
// Flow: real asset loading (faces -> characters -> arena -> controls layout) behind a 3D loading screen -> fade -> lobby -> match.
// The match itself is the unchanged Stage 2 MatchController + C# sim; this class only adds characters, stadium, ball and menus around it.
using System.Collections;
using System.Collections.Generic;
using NextPlay.Core;
using NextPlay.Gameplay;
using NextPlay.Sim;
using NextPlay.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NextPlay.App
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static bool SkipIntroOnce;               // set when coming back from a match: straight to the lobby
        public BrandingConfig Brand; public CharacterRoster Roster; public LoadingFlow Flow;
        public IntroScreen Intro; public LobbyScreen Lobby;
        public readonly List<GameObject> Characters = new List<GameObject>();
        public bool InLobby; public string LoadError;

        const float LineX0 = -4.7f;
        static readonly float[] LineS = { 0f, 1.25f, -1.25f, -2.5f, 0f, 2.5f };      // screen-right offset (c1 centre/front, c2 second position, ...)
        static readonly float[] LineD = { 0f, 1.0f, 1.0f, 2.0f, 2.0f, 2.0f };        // distance behind the front player
        static readonly float[] LineY = { 0f, 0f, 0f, 0.5f, 0.5f, 0.5f };            // back row stands on a 0.5 m riser

        static readonly Color Navy = new Color(0.04f, 0.09f, 0.31f), Gold = new Color(1f, 0.78f, 0.12f), Crimson = new Color(0.70f, 0.12f, 0.18f), White = new Color(0.96f, 0.96f, 0.96f);

        GameObject arenaRoot; ArenaCamera arenaCam; Camera cam; string stage = "Starting..."; bool matchStarted;
        readonly List<GameObject> roleLabels = new List<GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (SceneManager.GetActiveScene().name != "Boot") return;
            if (FindObjectOfType<GameBootstrap>() != null) return;
            new GameObject("NextPlayBootstrap").AddComponent<GameBootstrap>();
        }

        IEnumerator Start()
        {
            PerfProfile.Apply();
            try { Brand = BrandingStore.LoadBranding(); Roster = BrandingStore.LoadRoster(); }
            catch (System.Exception e) { LoadError = e.Message; Debug.LogError("NextPlay: config load failed: " + e); }
            if (LoadError != null) yield break;

            SetupCamera();
            Flow = new LoadingFlow();
            Flow.AddStep("faces", 6); Flow.AddStep("characters", 3); Flow.AddStep("arena", 3); Flow.AddStep("match", 1);
            if (SkipIntroOnce) { SkipIntroOnce = false; Flow.RequestClose(); }
            else Intro = IntroScreen.Create(Brand, () => Flow.RequestClose());
            StartCoroutine(LoadAll());

            float shown = 0f;
            while (!InLobby)
            {
                float dt = Time.unscaledDeltaTime;
                shown = (float)LoadingFlow.SmoothDisplay(shown, Flow.Progress, dt);
                if (Intro != null) Intro.SetProgress(shown, LoadingFlow.PercentFor(shown, Flow.AllComplete), LoadError != null ? LoadError : (Flow.AllComplete ? "Ready" : stage));
                if (Flow.Tick(dt, Brand.loadingScreen.minHoldSeconds)) yield return EnterLobby();
                else yield return null;
            }
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null) { GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); go.tag = "MainCamera"; cam = go.GetComponent<Camera>(); }
            StadiumBuilder.ConfigureCamera(cam);
            arenaCam = cam.gameObject.AddComponent<ArenaCamera>();
        }

        // Real loading work, spread over frames so the progress bar moves. Progress is reported per step, never faked.
        IEnumerator LoadAll()
        {
            arenaRoot = new GameObject("IntroArena");
            int n = Roster.characters.Count;

            stage = "Loading player faces...";
            for (int i = 0; i < n; i++)
            {
                CharacterDef c = Roster.characters[i];
                ResourceRequest rq = Resources.LoadAsync<Texture2D>("Characters/" + c.faceTexture);
                while (!rq.isDone) { Flow.Report("faces", (i + rq.progress) / n); yield return null; }
                Texture2D t = rq.asset as Texture2D;
                if (t == null) { LoadError = "Missing face texture: " + c.faceTexture; yield break; }
                FaceLibrary.Put(c.faceTexture, t); Flow.Report("faces", (i + 1f) / n);
            }
            Flow.Complete("faces");

            stage = "Building players...";
            for (int i = 0; i < n; i++)
            {
                CharacterDef c = Roster.characters[i];
                GameObject go = CharacterFactory.Build(c, FaceLibrary.Get(c.faceTexture), Navy, Gold);
                CharacterRig rig = go.GetComponent<CharacterRig>(); rig.Lineup = true; rig.PhaseOffset = i * 1.3f;
                go.transform.SetParent(arenaRoot.transform, false);
                go.transform.position = new Vector3(LineX0 + LineD[i], LineY[i], -LineS[i]);
                go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);   // face the camera (camera looks along +x)
                Characters.Add(go);
                GameObject label = StadiumBuilder.Label(arenaRoot.transform, c.roleLabel.ToUpperInvariant(), new Vector3(LineX0 + LineD[i] - 0.1f, LineY[i] + 2.02f, -LineS[i]), Quaternion.Euler(0f, 90f, 0f), 0.04f, Gold).gameObject;
                label.SetActive(false); roleLabels.Add(label);
                Flow.Report("characters", (i + 1f) / n); yield return null;
            }
            Flow.Complete("characters");

            stage = "Building arena...";
            GameObject court = CourtBuilder.Build(); court.transform.SetParent(arenaRoot.transform, true);
            Flow.Report("arena", 0.35f); yield return null;
            StadiumBuilder.Build(arenaRoot.transform, Brand.Name.display);
            Flow.Report("arena", 0.7f); yield return null;
            GameObject riser = GameObject.CreatePrimitive(PrimitiveType.Cube); riser.name = "Riser"; riser.transform.SetParent(arenaRoot.transform, false);
            riser.transform.position = new Vector3(LineX0 + 2.0f, 0.25f, 0f); riser.transform.localScale = new Vector3(1.5f, 0.5f, 7.2f);
            Destroy(riser.GetComponent<Collider>()); riser.GetComponent<Renderer>().sharedMaterial = ShaderLib.Lit(Navy, 0.4f);
            GameObject showcase = BallFactory.Build(2.4f); showcase.transform.SetParent(arenaRoot.transform, false); showcase.transform.position = new Vector3(-1.2f, 2.7f, 3.2f);
            showcase.AddComponent<Spinner>();
            AccentLight(new Vector3(-8f, 3.2f, 3.5f), new Color(0.25f, 0.45f, 1f)); AccentLight(new Vector3(-8f, 3.2f, -3.5f), new Color(1f, 0.8f, 0.3f));
            Flow.Complete("arena");

            stage = "Preparing match...";
            bool layoutOk = true;
            try { ControlLayout.Load(); } catch (System.Exception e) { LoadError = "Controls layout: " + e.Message; layoutOk = false; }
            if (!layoutOk) yield break;
            Flow.Complete("match");
        }

        void AccentLight(Vector3 pos, Color c)
        {
            GameObject g = new GameObject("AccentLight"); g.transform.SetParent(arenaRoot.transform, false); g.transform.position = pos;
            Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = 1.4f; l.range = 11f; l.shadows = LightShadows.None;
        }

        IEnumerator EnterLobby()
        {
            Lobby = LobbyScreen.Create(Brand, StartMatch, null);
            foreach (GameObject l in roleLabels) l.SetActive(true);
            foreach (GameObject c in Characters) { CharacterRig r = c.GetComponent<CharacterRig>(); if (r != null) r.Lineup = false; }
            arenaCam.Distance = 6.0f; arenaCam.Height = 1.45f; arenaCam.HalfWidth = 3.4f; arenaCam.HalfHeight = 1.7f;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / 0.6f);
                if (Intro != null) Intro.Group.alpha = 1f - k;
                Lobby.Group.alpha = k; yield return null;
            }
            if (Intro != null) { Intro.Destroy(); Intro = null; }
            Lobby.Group.alpha = 1f; InLobby = true;
        }

        void Update() { if (Lobby != null && Flow != null) Lobby.Refresh(Flow.AllComplete, (float)Flow.Progress); }

        void StartMatch()
        {
            if (matchStarted || Flow == null || !Flow.AllComplete) return;
            matchStarted = true;
            RoomConfig room = Lobby != null ? Lobby.Room : null;
            if (Lobby != null) Lobby.Destroy();
            Destroy(arenaRoot); Destroy(arenaCam);
            GameObject m = new GameObject("Match"); MatchController mc = m.AddComponent<MatchController>();
            GameObject ball = BallFactory.Build(); ball.transform.position = new Vector3(0f, -500f, 0f);   // hidden template the controller instantiates
            mc.BallPrefab = ball; mc.VisualFactory = MakeMatchVisual; mc.ExtraEnvironment = ConfigureMatchEnvironment; mc.OnExit = ExitToLobby; mc.Room = room;
        }

        GameObject MakeMatchVisual(Player p)
        {
            int idx = (p.Team * 2 + p.Idx) % Roster.characters.Count;   // 6v6 reuses the six characters across both teams
            CharacterDef def = Roster.characters[idx];
            bool home = p.Team == 0;
            GameObject go = CharacterFactory.Build(def, FaceLibrary.Get(def.faceTexture), home ? Navy : Crimson, home ? Gold : White);
            go.GetComponent<CharacterRig>().Lineup = false;
            return go;
        }

        void ConfigureMatchEnvironment(Transform parent)
        {
            StadiumBuilder.Build(parent, Brand.Name.display);
            StadiumBuilder.ConfigureCamera(Camera.main);
        }

        void ExitToLobby() { SkipIntroOnce = true; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        void OnGUI()
        {
            if (LoadError == null) return;
            GUI.color = Color.red; GUI.Label(new Rect(20, 20, Screen.width - 40, 220), "NextPlay load error: " + LoadError);
        }
    }
}
