// Branding configuration (game name, logo, tagline, app icon). Pure C#: no UnityEngine, so it is tested with plain `dotnet test`.
// Source of truth: config/branding.json (copied to StreamingAssets). Every option supplied by the user is preserved for approval.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextPlay.Core
{
    public sealed class NameOption { public string id, display, launcherLabel, source, status; public string[] lockupLines; }
    public sealed class LogoOption { public string id, type, resource, source, status; public bool available = true; }
    public sealed class TaglineOption { public string id, text, source, status; }
    public sealed class IconOption { public string id, file, source, status; public bool available = true; }
    public sealed class AndroidBranding { public string packageId; public bool packageIdApproved; public string note; }
    public sealed class LoadingSettings { public double minHoldSeconds = 0.8; public string layout = "auto"; public bool showCloseButton = true; public string note; }
    public sealed class BrandSelection { public string name, logo, tagline, appIcon; }

    public sealed class BrandingConfig
    {
        public int version;
        public string approvalStatus;
        public BrandSelection selected = new BrandSelection();
        public List<NameOption> nameOptions = new List<NameOption>();
        public List<LogoOption> logoOptions = new List<LogoOption>();
        public List<TaglineOption> taglineOptions = new List<TaglineOption>();
        public List<IconOption> appIconOptions = new List<IconOption>();
        public AndroidBranding android = new AndroidBranding();
        public LoadingSettings loadingScreen = new LoadingSettings();
        public Dictionary<string, string> colors = new Dictionary<string, string>();

        public static BrandingConfig Parse(string json)
        {
            BrandingConfig c = JsonConvert.DeserializeObject<BrandingConfig>(json);
            if (c == null) throw new FormatException("branding json is empty");
            return c;
        }

        public string ToJson() { return JsonConvert.SerializeObject(this, Formatting.Indented); }

        // ---- resolved selections: the selected id, or the first usable option if the id is missing/unavailable ----
        public NameOption Name { get { NameOption o = nameOptions.Find(x => x.id == selected.name); return o != null ? o : nameOptions[0]; } }
        public LogoOption Logo { get { LogoOption o = logoOptions.Find(x => x.id == selected.logo && x.available); return o != null ? o : logoOptions.Find(x => x.available); } }
        public TaglineOption Tagline { get { TaglineOption o = taglineOptions.Find(x => x.id == selected.tagline); return o != null ? o : taglineOptions[0]; } }
        public IconOption AppIcon { get { IconOption o = appIconOptions.Find(x => x.id == selected.appIcon && x.available); return o != null ? o : appIconOptions.Find(x => x.available); } }

        // kind: "name" | "logo" | "tagline" | "appIcon". Returns false (and changes nothing) for an unknown or unavailable id.
        public bool Select(string kind, string id)
        {
            switch (kind)
            {
                case "name": if (nameOptions.Exists(x => x.id == id)) { selected.name = id; return true; } return false;
                case "logo": if (logoOptions.Exists(x => x.id == id && x.available)) { selected.logo = id; return true; } return false;
                case "tagline": if (taglineOptions.Exists(x => x.id == id)) { selected.tagline = id; return true; } return false;
                case "appIcon": if (appIconOptions.Exists(x => x.id == id && x.available)) { selected.appIcon = id; return true; } return false;
                default: return false;
            }
        }

        // A user override file only carries {"selected": {...}}. Unknown ids are ignored, so a stale override can never break startup.
        public int ApplyOverride(string overrideJson)
        {
            if (string.IsNullOrWhiteSpace(overrideJson)) return 0;
            JObject o;
            try { o = JObject.Parse(overrideJson); } catch (JsonException) { return 0; }
            JToken sel = o["selected"]; if (sel == null || sel.Type != JTokenType.Object) return 0;
            int applied = 0;
            foreach (string kind in new[] { "name", "logo", "tagline", "appIcon" })
            {
                JToken v = sel[kind];
                if (v != null && v.Type == JTokenType.String && Select(kind, (string)v)) applied++;
            }
            return applied;
        }

        public string SelectionOnlyJson()
        {
            JObject o = new JObject();
            o["selected"] = JObject.FromObject(selected);
            return o.ToString(Formatting.Indented);
        }

        // Returns a list of problems; empty = valid.
        public List<string> Validate()
        {
            List<string> e = new List<string>();
            if (nameOptions.Count == 0) e.Add("no nameOptions");
            if (logoOptions.Count == 0 || !logoOptions.Exists(x => x.available)) e.Add("no available logoOptions");
            if (taglineOptions.Count == 0) e.Add("no taglineOptions");
            if (appIconOptions.Count == 0 || !appIconOptions.Exists(x => x.available)) e.Add("no available appIconOptions");
            if (selected == null) { e.Add("no selected block"); return e; }
            if (!nameOptions.Exists(x => x.id == selected.name)) e.Add("selected.name '" + selected.name + "' is not an option");
            if (!logoOptions.Exists(x => x.id == selected.logo && x.available)) e.Add("selected.logo '" + selected.logo + "' is not an available option");
            if (!taglineOptions.Exists(x => x.id == selected.tagline)) e.Add("selected.tagline '" + selected.tagline + "' is not an option");
            if (!appIconOptions.Exists(x => x.id == selected.appIcon && x.available)) e.Add("selected.appIcon '" + selected.appIcon + "' is not an available option");
            foreach (NameOption n in nameOptions)
            {
                if (string.IsNullOrEmpty(n.display) || string.IsNullOrEmpty(n.launcherLabel)) e.Add("name option '" + n.id + "' lacks display/launcherLabel");
                if (n.lockupLines == null || n.lockupLines.Length == 0) e.Add("name option '" + n.id + "' lacks lockupLines");
            }
            if (android == null || string.IsNullOrEmpty(android.packageId)) e.Add("android.packageId missing");
            return e;
        }
    }
}
