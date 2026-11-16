// UNTESTED in Unity. Loads config from Resources (readable on Android, unlike StreamingAssets) and a per-phone override file.
using System.IO;
using NextPlay.Core;
using UnityEngine;

namespace NextPlay.App
{
    public static class BrandingStore
    {
        public static string OverridePath { get { return Path.Combine(Application.persistentDataPath, "branding.override.json"); } }

        public static BrandingConfig LoadBranding()
        {
            TextAsset ta = Resources.Load<TextAsset>("Config/branding");
            if (ta == null) throw new FileNotFoundException("Resources/Config/branding.json is missing");
            BrandingConfig c = BrandingConfig.Parse(ta.text);
            try { if (File.Exists(OverridePath)) c.ApplyOverride(File.ReadAllText(OverridePath)); }
            catch (System.Exception e) { Debug.LogWarning("NextPlay: ignoring unreadable branding override: " + e.Message); }
            foreach (string problem in c.Validate()) Debug.LogError("NextPlay branding config: " + problem);
            return c;
        }

        public static CharacterRoster LoadRoster()
        {
            TextAsset ta = Resources.Load<TextAsset>("Config/characters");
            if (ta == null) throw new FileNotFoundException("Resources/Config/characters.json is missing");
            CharacterRoster r = CharacterRoster.Parse(ta.text);
            foreach (string problem in r.Validate()) Debug.LogError("NextPlay roster: " + problem);
            return r;
        }

        public static void SaveSelection(BrandingConfig c)
        {
            try { File.WriteAllText(OverridePath, c.SelectionOnlyJson()); }
            catch (System.Exception e) { Debug.LogWarning("NextPlay: could not save branding choice: " + e.Message); }
        }

        public static void ClearOverride() { try { if (File.Exists(OverridePath)) File.Delete(OverridePath); } catch (System.Exception) { } }
    }
}
