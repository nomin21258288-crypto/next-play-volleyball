// UNTESTED in Unity. Shared materials. Built-in render pipeline shaders (URP names are tried first if a project ever adds URP).
// Shader.Find only works in a player build for shaders that are included: see RequiredShaders (the editor tool adds them to
// "Always Included Shaders"), otherwise Android builds render pink/invisible.
using System.Collections.Generic;
using UnityEngine;

namespace NextPlay.App
{
    public static class ShaderLib
    {
        public static readonly string[] RequiredShaders =
        {
            "Standard", "Unlit/Color", "Unlit/Texture", "Legacy Shaders/Transparent/Diffuse", "Legacy Shaders/Diffuse", "Sprites/Default", "UI/Default"
        };

        static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();

        static Shader Find(params string[] names)
        {
            foreach (string n in names) { Shader s = Shader.Find(n); if (s != null) return s; }
            Debug.LogError("NextPlay: none of these shaders exist in the build: " + string.Join(", ", names));
            return Shader.Find("Sprites/Default");
        }

        static long Key(Color c, int kind) { Color32 k = c; uint packed = ((uint)k.r << 24) | ((uint)k.g << 16) | ((uint)k.b << 8) | k.a; return ((long)kind << 32) | packed; }

        // Shared lit material per colour (fewer materials = fewer draw-call state changes on phones).
        public static Material Lit(Color c, float smoothness = 0.2f)
        {
            long key = Key(c, 1); Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Find("Universal Render Pipeline/Lit", "Standard")) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(c), color = c };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            cache[key] = m; return m;
        }

        public static Material Unlit(Color c)
        {
            long key = Key(c, 2); Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Find("Unlit/Color", "Sprites/Default")) { name = "Unlit_" + ColorUtility.ToHtmlStringRGB(c), color = c };
            cache[key] = m; return m;
        }

        public static Material Textured(Texture tex, Color tint, float tilingX = 1f, float tilingY = 1f)
        {
            Material m = new Material(Find("Standard", "Unlit/Texture")) { name = "Tex_" + (tex != null ? tex.name : "none"), color = tint, mainTexture = tex };
            m.mainTextureScale = new Vector2(tilingX, tilingY);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
            return m;
        }

        // Face photo on the head cap: lit, with alpha so the soft mask edge blends into the hair-coloured head.
        public static Material FaceDecal(Texture tex)
        {
            Material m = new Material(Find("Legacy Shaders/Transparent/Diffuse", "Sprites/Default")) { name = "Face_" + (tex != null ? tex.name : "none"), mainTexture = tex, color = Color.white };
            m.renderQueue = 3000; return m;
        }
    }
}
