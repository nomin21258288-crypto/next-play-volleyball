// UNTESTED in Unity. Procedural volleyball (blue / yellow / white panels). Diameter matches the sim ball (BallCfg.Radius).
using NextPlay.Sim;
using UnityEngine;

namespace NextPlay.App
{
    public static class BallFactory
    {
        static Texture2D tex;
        static Texture2D BallTexture()
        {
            if (tex != null) return tex;
            const int W = 256, H = 128; Texture2D t = new Texture2D(W, H, TextureFormat.RGB24, false); t.name = "VolleyballTex"; t.wrapMode = TextureWrapMode.Repeat;
            Color white = new Color(0.97f, 0.97f, 0.97f), blue = new Color(0.12f, 0.35f, 0.85f), yellow = new Color(1f, 0.8f, 0.1f), seam = new Color(0.06f, 0.12f, 0.35f);
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float phi = (x / (float)W) * Mathf.PI * 2f, theta = (y / (float)H - 0.5f) * Mathf.PI;
                    float s = Mathf.Sin(3f * phi + 2f * theta);
                    Color c;
                    if (Mathf.Abs(s) < 0.10f) c = seam;
                    else if (s > 0f) c = (Mathf.FloorToInt(phi / (Mathf.PI * 2f / 3f)) % 2 == 0) ? blue : yellow;
                    else c = white;
                    px[y * W + x] = c;
                }
            t.SetPixels(px); t.Apply(true, true); tex = t; return t;
        }

        // diameterScale 1 = real sim size; showcase uses a bigger one.
        public static GameObject Build(float diameterScale = 1f)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Ball";
            Collider c = b.GetComponent<Collider>(); if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
            b.transform.localScale = Vector3.one * ((float)(BallCfg.Radius * 2) * diameterScale);
            b.GetComponent<Renderer>().sharedMaterial = ShaderLib.Textured(BallTexture(), Color.white);
            return b;
        }
    }
}
