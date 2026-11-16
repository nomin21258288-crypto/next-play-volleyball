// UNTESTED in Unity. Lobby overlay: Play (practice match vs bots), Branding options. Everything visible works or is marked "not yet".
// Profile / Wall / Emotes / Rewards are NOT implemented in this stage and are deliberately not shown as fake buttons.
using NextPlay.Core;
using NextPlay.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace NextPlay.App
{
    public sealed class LobbyScreen
    {
        public Canvas Canvas; public CanvasGroup Group;
        Text title, playLabel, tagline; Button play; Image playImage;
        // Create Room: default is the original ranked 2v2 practice match. Only a custom room has a selectable target score (7..25).
        public RoomConfig Room = RoomConfig.Create("H0", "2v2", "ranked");
        GameObject roomPanel; Text roomSummary;
        GameObject panel; BrandingConfig brand; System.Action onChanged;

        public static LobbyScreen Create(BrandingConfig brand, System.Action onPlay, System.Action onBrandingChanged)
        {
            LobbyScreen s = new LobbyScreen { brand = brand, onChanged = onBrandingChanged };
            s.Canvas = UiKit.MakeCanvas("LobbyCanvas", 90); s.Group = s.Canvas.GetComponent<CanvasGroup>(); s.Group.alpha = 0f;
            UiKit.EnsureEventSystem();
            RectTransform safe = UiKit.SafeRoot(s.Canvas);
            Color gold = UiKit.Hex("#FFC81E", Color.yellow);

            s.title = UiKit.MakeText(safe, "LobbyTitle", brand.Name.display, 52, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(1300, 70));
            s.tagline = UiKit.MakeText(safe, "LobbyTagline", brand.Tagline.text, 34, gold, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -84), new Vector2(1300, 50));

            s.play = UiKit.MakeButton(safe, "PlayButton", "LOADING 1%", 54, new Color(0.35f, 0.35f, 0.4f, 0.95f), Color.white, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(520, 130), onPlay);
            s.playImage = s.play.GetComponent<Image>(); s.playLabel = s.play.GetComponentInChildren<Text>(); s.play.interactable = false;

            s.roomSummary = UiKit.MakeText(safe, "RoomSummary", s.RoomText(), 30, Color.white, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 170), new Vector2(900, 44));
            UiKit.MakeButton(safe, "RoomButton", "CREATE ROOM", 34, new Color(0.55f, 0.35f, 0.05f, 0.95f), Color.white, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 225), new Vector2(520, 90), s.ToggleRoomPanel);
            UiKit.MakeButton(safe, "BrandingButton", "BRANDING OPTIONS", 34, new Color(0.04f, 0.10f, 0.35f, 0.92f), Color.white, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(420, 90), s.TogglePanel);
            UiKit.MakeText(safe, "ProposedNote", "Role labels under the players are proposed, pending your approval.", 26, new Color(1, 1, 1, 0.8f), TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 132), new Vector2(1100, 40), false);
            return s;
        }

        // Called every frame by GameBootstrap.
        public void Refresh(bool loaded, float progress01)
        {
            if (play == null) return;
            play.interactable = loaded;
            playImage.color = loaded ? new Color(0.1f, 0.65f, 0.25f, 0.97f) : new Color(0.35f, 0.35f, 0.4f, 0.95f);
            playLabel.text = loaded ? "PLAY MATCH" : "LOADING " + Mathf.Clamp(Mathf.FloorToInt(progress01 * 100f), 1, 99) + "%";
        }

        public void RefreshTexts() { title.text = brand.Name.display; tagline.text = brand.Tagline.text; }
        public void Destroy() { if (Canvas != null) Object.Destroy(Canvas.gameObject); }

        string RoomText() { return Room.Mode == "custom" ? Room.Format + " custom - first to " + Room.Rules.SetTargets[0] : Room.Format + " ranked - official scoring"; }

        void ToggleRoomPanel()
        {
            if (roomPanel != null) { Object.Destroy(roomPanel); roomPanel = null; return; }
            roomPanel = BuildRoomPanel();
        }

        // Rebuilds the room with the chosen settings. Ranked never carries a custom target; custom keeps the last chosen one.
        void SetRoom(string format, string mode, int target)
        {
            Room = mode == "custom" ? RoomConfig.Create("H0", format, "custom", target) : RoomConfig.Create("H0", format, "ranked");
            roomSummary.text = RoomText();
            Object.Destroy(roomPanel); roomPanel = BuildRoomPanel();
        }

        GameObject BuildRoomPanel()
        {
            RectTransform root = UiKit.MakeRect(Canvas.transform, "RoomPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            root.offsetMin = root.offsetMax = Vector2.zero;
            Image bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(0.01f, 0.03f, 0.12f, 0.95f);
            RectTransform safe = UiKit.MakeRect(root, "Safe", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Rect sa = Screen.safeArea; safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height); safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            Vector2 tl = new Vector2(0, 1);
            Color on = new Color(0.1f, 0.65f, 0.25f, 0.97f), off = new Color(0.15f, 0.2f, 0.4f, 0.95f);
            bool custom = Room.Mode == "custom";
            int target = custom ? Room.Rules.SetTargets[0] : 15;
            UiKit.MakeText(safe, "H", "CREATE ROOM", 46, UiKit.Hex("#FFC81E", Color.yellow), TextAnchor.UpperLeft, tl, tl, tl, new Vector2(24, -16), new Vector2(900, 64));
            UiKit.MakeText(safe, "FormatLbl", "Format", 30, Color.white, TextAnchor.UpperLeft, tl, tl, tl, new Vector2(24, -96), new Vector2(300, 40));
            for (int i = 0; i < RoomConfig.Formats.Length; i++)
            {
                string f = RoomConfig.Formats[i];
                UiKit.MakeButton(safe, "Fmt_" + f, f, 34, f == Room.Format ? on : off, Color.white, tl, tl, tl, new Vector2(24 + i * 190, -140), new Vector2(170, 80), () => SetRoom(f, Room.Mode, target));
            }
            UiKit.MakeText(safe, "ModeLbl", "Match type", 30, Color.white, TextAnchor.UpperLeft, tl, tl, tl, new Vector2(24, -236), new Vector2(300, 40));
            UiKit.MakeButton(safe, "Mode_custom", "CUSTOM", 32, custom ? on : off, Color.white, tl, tl, tl, new Vector2(24, -280), new Vector2(260, 80), () => SetRoom(Room.Format, "custom", target));
            UiKit.MakeButton(safe, "Mode_ranked", "RANKED", 32, !custom ? on : off, Color.white, tl, tl, tl, new Vector2(304, -280), new Vector2(260, 80), () => SetRoom(Room.Format, "ranked", target));
            if (custom)
            {
                UiKit.MakeText(safe, "ScoreLbl", "Play to (any whole number 7-25): " + target, 30, Color.white, TextAnchor.UpperLeft, tl, tl, tl, new Vector2(24, -376), new Vector2(1200, 40));
                int col = 0, row = 0;
                foreach (int n in RoomConfig.ScoreOptions())
                {
                    int pick = n;
                    UiKit.MakeButton(safe, "Score_" + n, n.ToString(), 32, n == target ? on : off, Color.white, tl, tl, tl, new Vector2(24 + col * 112, -424 - row * 90), new Vector2(100, 76), () => SetRoom(Room.Format, "custom", pick));
                    if (++col == 7) { col = 0; row++; }
                }
            }
            else UiKit.MakeText(safe, "RankedNote", "Ranked matches use the fixed official scoring (best of 3: sets to 25, deciding set to 15, win by 2). Only custom rooms can choose the score.", 28, Color.white, TextAnchor.UpperLeft, tl, tl, tl, new Vector2(24, -380), new Vector2(1500, 90), false);
            UiKit.MakeButton(safe, "RoomDone", "DONE", 40, new Color(0.85f, 0.12f, 0.15f, 0.95f), Color.white, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(260, 80), ToggleRoomPanel);
            return root.gameObject;
        }

        void TogglePanel()
        {
            if (panel != null) { Object.Destroy(panel); panel = null; return; }
            panel = BuildPanel();
        }

        GameObject BuildPanel()
        {
            RectTransform root = UiKit.MakeRect(Canvas.transform, "BrandingPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            root.offsetMin = root.offsetMax = Vector2.zero;
            Image bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(0.01f, 0.03f, 0.12f, 0.94f);
            RectTransform safe = UiKit.MakeRect(root, "Safe", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Rect sa = Screen.safeArea; safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height); safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height); safe.offsetMin = safe.offsetMax = Vector2.zero;
            UiKit.MakeText(safe, "H", "BRANDING OPTIONS  (pending approval)", 46, UiKit.Hex("#FFC81E", Color.yellow), TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(1500, 64));
            UiKit.MakeText(safe, "Note", "Name, logo and tagline switch instantly and are remembered on this phone. App icon, launcher name and package id are baked into the APK: edit config/branding.json and rebuild.", 26, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -80), new Vector2(1500, 40), false);

            float y = -150f;
            y = Row(safe, y, "GAME NAME", "name");
            y = Row(safe, y, "LOGO", "logo");
            y = Row(safe, y, "TAGLINE", "tagline");
            y = Row(safe, y, "APP ICON (next build)", "appIcon");

            UiKit.MakeButton(safe, "Reset", "RESET TO FILE DEFAULTS", 30, new Color(0.45f, 0.2f, 0.2f, 0.95f), Color.white, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(460, 80), () =>
            {
                BrandingStore.ClearOverride(); BrandingConfig fresh = BrandingStore.LoadBranding();
                brand.selected = fresh.selected; RefreshTexts(); if (onChanged != null) onChanged(); Object.Destroy(panel); panel = BuildPanel();
            });
            UiKit.MakeButton(safe, "Close", "CLOSE", 40, new Color(0.85f, 0.12f, 0.15f, 0.95f), Color.white, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(260, 80), TogglePanel);
            return root.gameObject;
        }

        float Row(RectTransform parent, float y, string heading, string kind)
        {
            UiKit.MakeText(parent, "RowH_" + kind, heading, 32, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, y), new Vector2(900, 40), false);
            y -= 46f;
            string[] ids, labels; bool[] avail; string selected;
            switch (kind)
            {
                case "name": ids = brand.nameOptions.ConvertAll(o => o.id).ToArray(); labels = brand.nameOptions.ConvertAll(o => o.display).ToArray(); avail = brand.nameOptions.ConvertAll(o => true).ToArray(); selected = brand.selected.name; break;
                case "logo": ids = brand.logoOptions.ConvertAll(o => o.id).ToArray(); labels = brand.logoOptions.ConvertAll(o => o.available ? o.id : o.id + " (not supplied)").ToArray(); avail = brand.logoOptions.ConvertAll(o => o.available).ToArray(); selected = brand.selected.logo; break;
                case "tagline": ids = brand.taglineOptions.ConvertAll(o => o.id).ToArray(); labels = brand.taglineOptions.ConvertAll(o => o.text).ToArray(); avail = brand.taglineOptions.ConvertAll(o => true).ToArray(); selected = brand.selected.tagline; break;
                default: ids = brand.appIconOptions.ConvertAll(o => o.id).ToArray(); labels = brand.appIconOptions.ConvertAll(o => o.id).ToArray(); avail = brand.appIconOptions.ConvertAll(o => o.available).ToArray(); selected = brand.selected.appIcon; break;
            }
            float x = 24f;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i]; bool isSel = id == selected; float w = Mathf.Clamp(labels[i].Length * 19f + 50f, 220f, 760f);
                Color bgc = !avail[i] ? new Color(0.25f, 0.25f, 0.3f, 0.8f) : (isSel ? new Color(0.1f, 0.6f, 0.25f, 0.97f) : new Color(0.1f, 0.25f, 0.6f, 0.95f));
                UiKit.MakeButton(parent, "Opt_" + kind + "_" + id, (isSel ? "[x] " : "") + labels[i], 28, bgc, Color.white, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(w, 70), () =>
                {
                    if (!brand.Select(kind, id)) return;
                    BrandingStore.SaveSelection(brand); RefreshTexts(); if (onChanged != null) onChanged();
                    Object.Destroy(panel); panel = BuildPanel();
                });
                x += w + 14f;
            }
            return y - 100f;
        }
    }
}
