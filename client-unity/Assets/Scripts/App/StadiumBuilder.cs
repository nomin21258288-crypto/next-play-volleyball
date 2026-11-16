// UNTESTED in Unity. Indoor arena around the Stage 2 court: ad boards (brand name), crowd bleachers, light rigs, dark ceiling colour.
// Cheap on purpose for phones: ~40 renderers, shared materials, one tiled crowd texture, NO extra realtime lights.
using NextPlay.Core;
using UnityEngine;

namespace NextPlay.App
{
    public sealed class Spinner : MonoBehaviour
    {
        public Vector3 Axis = new Vector3(0.3f, 1f, 0.1f); public float DegPerSec = 60f; public float BobAmp = 0.12f, BobSpeed = 1.4f; Vector3 basePos;
        void Start() { basePos = transform.position; }
        void Update() { transform.Rotate(Axis, DegPerSec * Time.deltaTime, Space.Self); transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobAmp); }
    }

    public static class StadiumBuilder
    {
        static Texture2D crowdTex;

        static Texture2D Crowd()
        {
            if (crowdTex != null) return crowdTex;
            const int W = 128, H = 32; Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false); t.name = "CrowdTex"; t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Repeat;
            System.Random rng = new System.Random(20240611);
            Color[] palette = { new Color(0.10f, 0.20f, 0.55f), new Color(0.95f, 0.78f, 0.15f), new Color(0.90f, 0.90f, 0.95f), new Color(0.75f, 0.15f, 0.20f), new Color(0.15f, 0.55f, 0.35f), new Color(0.05f, 0.08f, 0.20f) };
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool person = (x % 4) < 3 && (y % 4) < 3;
                    px[y * W + x] = person ? palette[rng.Next(palette.Length)] * (0.55f + 0.45f * (float)rng.NextDouble()) : new Color(0.03f, 0.04f, 0.10f);
                    px[y * W + x].a = 1f;
                }
            t.SetPixels(px); t.Apply(false, true); crowdTex = t; return t;
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = size;
            Collider c = g.GetComponent<Collider>(); if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
            g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }

        public static TextMesh Label(Transform parent, string text, Vector3 pos, Quaternion rot, float charSize, Color color, int fontSize = 48)
        {
            GameObject go = new GameObject("Label_" + text); go.transform.SetParent(parent, false); go.transform.position = pos; go.transform.rotation = rot;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = UiKit.Font; tm.text = text; tm.fontSize = fontSize; tm.characterSize = charSize; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color; tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
            return tm;
        }

        // brandText: the SELECTED game name (shown on the ad boards, so changing the name in branding.json re-brands the arena).
        public static GameObject Build(Transform parent, string brandText)
        {
            GameObject root = new GameObject("Stadium"); if (parent != null) root.transform.SetParent(parent, false);
            Material floor = ShaderLib.Lit(new Color(0.05f, 0.08f, 0.20f), 0.35f), board = ShaderLib.Lit(new Color(0.04f, 0.10f, 0.35f), 0.4f), lamp = ShaderLib.Unlit(new Color(1f, 0.97f, 0.85f));
            Material crowd = ShaderLib.Textured(Crowd(), Color.white, 14f, 1f), crowdEnd = ShaderLib.Textured(Crowd(), Color.white, 6f, 1f);

            Box(root.transform, "ArenaFloor", new Vector3(0, -0.12f, 0), new Vector3(90, 0.1f, 70), floor);
            // ad boards ring (inside face carries the brand text)
            Box(root.transform, "Board_N", new Vector3(0, 0.45f, 10.6f), new Vector3(31.2f, 0.9f, 0.12f), board);
            Box(root.transform, "Board_S", new Vector3(0, 0.45f, -10.6f), new Vector3(31.2f, 0.9f, 0.12f), board);
            Box(root.transform, "Board_E", new Vector3(15.6f, 0.45f, 0), new Vector3(0.12f, 0.9f, 21.2f), board);
            Box(root.transform, "Board_W", new Vector3(-15.6f, 0.45f, 0), new Vector3(0.12f, 0.9f, 21.2f), board);
            Color gold = new Color(1f, 0.82f, 0.12f);
            for (int i = -1; i <= 1; i++)
            {
                Label(root.transform, brandText, new Vector3(i * 9.5f, 0.45f, 10.52f), Quaternion.Euler(0, 0, 0), 0.07f, gold);
                Label(root.transform, brandText, new Vector3(i * 9.5f, 0.45f, -10.52f), Quaternion.Euler(0, 180, 0), 0.07f, gold);
            }
            Label(root.transform, brandText, new Vector3(15.52f, 0.45f, 0), Quaternion.Euler(0, 90, 0), 0.07f, gold);

            // crowd tiers: long sides and far end (the near end holds the Stage 2 camera, so it stays open)
            for (int i = 0; i < 4; i++)
            {
                float h = (i + 1) * 0.9f, off = 12.2f + i * 1.4f;
                Box(root.transform, "Tier_N" + i, new Vector3(0, h / 2, off), new Vector3(36f, h, 1.4f), crowd);
                Box(root.transform, "Tier_S" + i, new Vector3(0, h / 2, -off), new Vector3(36f, h, 1.4f), crowd);
                Box(root.transform, "Tier_E" + i, new Vector3(16.6f + i * 1.4f, h / 2, 0), new Vector3(1.4f, h, 24f), crowdEnd);
            }
            // light rigs (visual only: unlit emissive-looking lamps, no real lights)
            for (int x = -12; x <= 12; x += 4) for (int s = -1; s <= 1; s += 2)
                    Box(root.transform, "Lamp", new Vector3(x, 12f, s * 7f), new Vector3(0.9f, 0.2f, 0.9f), lamp);
            Box(root.transform, "RigBar_N", new Vector3(0, 12.2f, 7f), new Vector3(28f, 0.12f, 0.12f), board);
            Box(root.transform, "RigBar_S", new Vector3(0, 12.2f, -7f), new Vector3(28f, 0.12f, 0.12f), board);
            return root;
        }

        public static void ConfigureCamera(Camera cam)
        {
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.04f, 0.12f);
            cam.farClipPlane = 120f; cam.allowMSAA = true; cam.allowHDR = false;
        }
    }
}
