// UNTESTED. Runs the C# sim at its fixed 120 Hz step, feeds it touch input for the human player (H0) and bots for the rest,
// and mirrors sim state into placeholder views. Uses NO physics from Unity: the sim is the source of truth.
using System.Collections.Generic;
using NextPlay.Controls;
using NextPlay.Sim;
using NextPlay.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NextPlay.Gameplay
{
    public sealed class MatchController : MonoBehaviour
    {
        public GameObject PlayerPrefabTeam0, PlayerPrefabTeam1, BallPrefab;   // optional; primitives are used when null
        public uint Seed = 12345;
        // Stage 3 hooks (all optional; null = exact Stage 2 behaviour):
        public System.Func<Player, GameObject> VisualFactory;   // builds the 3D character for a sim player
        public System.Action<Transform> ExtraEnvironment;       // stadium, branding boards, camera tweaks (called after the camera exists)
        public RoomConfig Room;                                 // Create Room settings; null = default ranked 2v2 practice
        public System.Action OnExit;                            // MENU button -> back to the lobby
        Game game; TouchInput touch; Transform ball; readonly List<PlayerView> views = new List<PlayerView>();
        double acc; Text scoreText; bool paused;

        void Start()
        {
            Application.targetFrameRate = 60;
            Coach coach = new Coach { Seed = Seed };
            game = Room != null ? Room.StartGame(new[] { "H0" }, coach) : new Game(new[] { "H0" }, 2, coach);
            CourtBuilder.Build();
            foreach (Player p in game.Players)
            {
                GameObject built = VisualFactory != null ? VisualFactory(p) : null;
                views.Add(built != null ? PlayerView.CreateWithVisual(p, transform, built) : PlayerView.Create(p, transform, p.Team == 0 ? PlayerPrefabTeam0 : PlayerPrefabTeam1));
            }
            GameObject b = BallPrefab != null ? Instantiate(BallPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "Ball"; if (BallPrefab == null) { b.transform.localScale = Vector3.one * (float)(BallCfg.Radius * 2); Destroy(b.GetComponent<Collider>()); }
            ball = b.transform;
            CameraRig.Create(ball);
            BuildHud();
            if (ExtraEnvironment != null) ExtraEnvironment(transform);
        }

        void BuildHud()
        {
            if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cgo.GetComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; cs.scaleFactor = 1f;
            LayoutFile layout = ControlLayout.Load();
            touch = ControlHud.Build(canvas, layout, () => paused = !paused);
            GameObject sgo = new GameObject("Score", typeof(RectTransform), typeof(Text)); sgo.transform.SetParent(cgo.transform, false);
            scoreText = sgo.GetComponent<Text>(); scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); scoreText.fontSize = 36; scoreText.alignment = TextAnchor.UpperCenter; scoreText.color = Color.white;
            RectTransform r = (RectTransform)sgo.transform; r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1); r.anchoredPosition = new Vector2(0, -10); r.sizeDelta = new Vector2(0, 60);
            scoreText.raycastTarget = false;
            if (OnExit != null) BuildMenuButton(cgo.transform);
        }

        void BuildMenuButton(Transform canvas)
        {
            Rect safe = Screen.safeArea; float px = Mathf.Max(1f, Screen.dpi / 160f);
            GameObject go = new GameObject("HUD_menu", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(canvas, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            RectTransform rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(safe.xMin + 16f * px, -(Screen.height - safe.yMax) - 16f * px); rt.sizeDelta = new Vector2(84f * px, 44f * px);
            go.GetComponent<Button>().onClick.AddListener(() => { if (OnExit != null) OnExit(); });
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(go.transform, false);
            Text t = label.GetComponent<Text>(); t.text = "MENU"; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = Mathf.RoundToInt(18f * px);
            RectTransform lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (game == null || paused) return;
            acc += System.Math.Min(Time.unscaledDeltaTime, 0.25f);
            Dictionary<string, PlayerInput> inputs = new Dictionary<string, PlayerInput>();
            while (acc >= Const.DT && game.Phase != Phase.Over)
            {
                // Camera sits behind team 0 looking toward +x: screen-up = court +x, screen-right = court -z.
                inputs["H0"] = touch.Consume(s => new Vector2(s.y, -s.x));
                game.Tick(inputs, Bot.Input); acc -= Const.DT;
            }
            foreach (PlayerView v in views) v.Sync(game);
            ball.position = new Vector3((float)game.Ball.Pos.X, (float)game.Ball.Pos.Y, (float)game.Ball.Pos.Z);
            MatchRules rl = game.Match.Rules;
            string rule = rl.Mode == "custom" ? "First to " + rl.TargetFor(game.Match.SetIndex) + (rl.SetsToWin > 1 ? "  (best of " + (rl.SetsToWin * 2 - 1) + ")" : "") : "RANKED  to " + rl.TargetFor(game.Match.SetIndex);
            scoreText.text = game.Match.Score[0] + " - " + game.Match.Score[1] + "   sets " + game.Match.Sets[0] + "-" + game.Match.Sets[1] + "\n" + rule + (game.Phase == Phase.Over ? "   MATCH OVER" : "");
        }
    }
}
