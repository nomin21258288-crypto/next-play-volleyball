// UNTESTED in Unity. The loading / intro overlay: logo lockup (from the SELECTED name), tagline, real progress bar 1-100%, Close (X).
// It is drawn over the live 3D scene (six characters, court, net, stadium) built by GameBootstrap.
using NextPlay.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NextPlay.App
{
    public sealed class IntroScreen
    {
        public Canvas Canvas; public CanvasGroup Group;
        RectTransform fill; Text percentText, statusText;

        public static IntroScreen Create(BrandingConfig brand, System.Action onClose)
        {
            IntroScreen s = new IntroScreen();
            s.Canvas = UiKit.MakeCanvas("IntroCanvas", 100); s.Group = s.Canvas.GetComponent<CanvasGroup>();
            UiKit.EnsureEventSystem();
            RectTransform safe = UiKit.SafeRoot(s.Canvas);
            Color gold = UiKit.Hex(brand.colors.ContainsKey("gold") ? brand.colors["gold"] : "#FFC81E", Color.yellow);

            // --- logo lockup: generated from the selected name's lines (NOT final artwork) ---
            string[] lines = brand.Name.lockupLines; float y = -10f; int big = 0;
            foreach (string line in lines)
            {
                bool small = line.ToUpperInvariant().Contains("PLAY ELEVEN");
                int size = small ? 54 : (big == 0 ? 112 : 142); Color col = small ? Color.white : (big == 0 ? Color.white : gold);
                float h = small ? 70f : size * 1.12f;
                if (small)
                {
                    UiKit.MakeImage(safe, "ElevenPill", new Color(0.04f, 0.08f, 0.25f, 0.9f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(520, 70));
                }
                UiKit.MakeText(safe, "Logo_" + line, line, size, col, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(1500, h));
                y -= h + 2f; if (!small) big++;
            }
            UiKit.MakeText(safe, "Tagline", brand.Tagline.text, 46, gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y - 6f), new Vector2(1500, 64));

            // --- close (X): always present when enabled, never blocked by loading ---
            if (brand.loadingScreen.showCloseButton)
                UiKit.MakeButton(safe, "CloseX", "X", 64, new Color(0.85f, 0.12f, 0.15f, 0.95f), Color.white, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(112, 112), onClose);

            // --- progress bar ---
            RectTransform bar = UiKit.MakeImage(safe, "BarBack", new Color(0.02f, 0.05f, 0.18f, 0.88f), new Vector2(0.05f, 0), new Vector2(0.95f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(0, 64)).rectTransform;
            Image f = UiKit.MakeImage(bar, "BarFill", gold, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            s.fill = f.rectTransform; s.fill.offsetMin = new Vector2(4, 4); s.fill.offsetMax = new Vector2(0, -4);
            s.percentText = UiKit.MakeText(bar, "Percent", "1%", 44, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            s.percentText.rectTransform.offsetMin = s.percentText.rectTransform.offsetMax = Vector2.zero;
            s.statusText = UiKit.MakeText(safe, "Status", "Loading...", 40, Color.white, TextAnchor.LowerLeft, new Vector2(0.05f, 0), new Vector2(0.95f, 0), new Vector2(0, 0), new Vector2(0, 100), new Vector2(0, 56));
            s.statusText.rectTransform.offsetMin = new Vector2(0, 100); s.statusText.rectTransform.offsetMax = new Vector2(0, 156);
            return s;
        }

        // shown: 0..1 bar position (never ahead of real progress). percent: 1..100 text.
        public void SetProgress(float shown, int percent, string status)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(shown), 1f); fill.offsetMax = new Vector2(-4, -4);
            percentText.text = percent + "%";
            if (status != null) statusText.text = status;
        }

        public void Destroy() { if (Canvas != null) Object.Destroy(Canvas.gameObject); }
    }
}
