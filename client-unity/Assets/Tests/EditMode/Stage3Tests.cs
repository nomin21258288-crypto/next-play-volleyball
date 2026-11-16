// Stage 3 tests for the pure-C# core (branding config, character roster, loading flow).
// Compiled by BOTH Unity's EditMode runner and plain `dotnet test` (tools/SimTests). No UnityEngine types used here.
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NextPlay.Core;
using NUnit.Framework;

namespace NextPlay.Tests
{
    public static class ConfigPaths
    {
        // Walks up from the test directory / CWD until <rel> or client-unity/<rel> exists.
        public static string Find(string rel)
        {
            string[] starts = { TestContext.CurrentContext.TestDirectory, Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (string s in starts)
                for (DirectoryInfo d = new DirectoryInfo(s); d != null; d = d.Parent)
                {
                    string a = Path.Combine(d.FullName, rel); if (File.Exists(a)) return a;
                    string b = Path.Combine(d.FullName, "client-unity", rel); if (File.Exists(b)) return b;
                }
            return null;
        }
        public static string Read(string rel)
        {
            string p = Find(rel); if (p == null) throw new FileNotFoundException(rel + " not found");
            return File.ReadAllText(p);
        }
        public static BrandingConfig Branding() { return BrandingConfig.Parse(Read("Assets/Resources/Config/branding.json")); }
        public static CharacterRoster Roster() { return CharacterRoster.Parse(Read("Assets/Resources/Config/characters.json")); }
    }

    public class BrandingConfigTests
    {
        [Test] public void ShippedConfigIsValid() { Assert.That(ConfigPaths.Branding().Validate(), Is.Empty); }

        [Test] public void AllSuppliedNameOptionsArePreservedForApproval()
        {
            BrandingConfig c = ConfigPaths.Branding();
            foreach (string id in new[] { "nexora_play_volleyball_game", "next_play_volleyball_game", "nexora_play_platform" })
                Assert.That(c.nameOptions.Exists(n => n.id == id), id);
            Assert.That(c.taglineOptions.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(c.appIconOptions.Count, Is.EqualTo(2));
        }

        [Test] public void SelectionResolvesToRealOptions()
        {
            BrandingConfig c = ConfigPaths.Branding();
            Assert.That(c.Name.display, Is.Not.Empty); Assert.That(c.Logo.available, Is.True);
            Assert.That(c.Tagline.text, Is.Not.Empty); Assert.That(c.AppIcon.available, Is.True);
        }

        [Test] public void FinalLogoSlotIsReservedButNeverSelectableUntilSupplied()
        {
            BrandingConfig c = ConfigPaths.Branding();
            Assert.That(c.logoOptions.Exists(l => l.id == "final_vector_logo" && !l.available));
            Assert.That(c.Select("logo", "final_vector_logo"), Is.False);
            Assert.That(c.Logo.id, Is.EqualTo("text_lockup"));
        }

        [Test] public void SelectRejectsUnknownKindsAndIds()
        {
            BrandingConfig c = ConfigPaths.Branding(); string before = c.selected.name;
            Assert.That(c.Select("name", "nope"), Is.False); Assert.That(c.Select("colour", "x"), Is.False);
            Assert.That(c.selected.name, Is.EqualTo(before));
            Assert.That(c.Select("name", "next_play_volleyball_game"), Is.True);
            Assert.That(c.Name.display, Is.EqualTo("NEXT PLAY VOLLEYBALL GAME"));
        }

        [Test] public void OverrideAppliesKnownIdsAndIgnoresUnknown()
        {
            BrandingConfig c = ConfigPaths.Branding();
            int n = c.ApplyOverride("{\"selected\":{\"name\":\"nexora_play_platform\",\"tagline\":\"does_not_exist\",\"appIcon\":\"three_faces\"}}");
            Assert.That(n, Is.EqualTo(2));
            Assert.That(c.selected.name, Is.EqualTo("nexora_play_platform")); Assert.That(c.selected.appIcon, Is.EqualTo("three_faces"));
            Assert.That(c.selected.tagline, Is.EqualTo("rise_set_smash"));
        }

        [Test] public void GarbageOverrideNeverBreaksStartup()
        {
            BrandingConfig c = ConfigPaths.Branding(); string before = c.ToJson();
            Assert.That(c.ApplyOverride(null), Is.EqualTo(0)); Assert.That(c.ApplyOverride("   "), Is.EqualTo(0));
            Assert.That(c.ApplyOverride("{not json"), Is.EqualTo(0)); Assert.That(c.ApplyOverride("{\"selected\":5}"), Is.EqualTo(0));
            Assert.That(c.ToJson(), Is.EqualTo(before));
        }

        [Test] public void SelectionOnlyJsonRoundTrips()
        {
            BrandingConfig a = ConfigPaths.Branding(); a.Select("name", "next_play_volleyball_game"); a.Select("tagline", "the_volleyball_arena");
            BrandingConfig b = ConfigPaths.Branding(); b.ApplyOverride(a.SelectionOnlyJson());
            Assert.That(b.selected.name, Is.EqualTo("next_play_volleyball_game")); Assert.That(b.selected.tagline, Is.EqualTo("the_volleyball_arena"));
        }

        [Test] public void ValidateCatchesBrokenSelection()
        {
            BrandingConfig c = ConfigPaths.Branding(); c.selected.name = "ghost";
            Assert.That(c.Validate(), Has.Some.Matches<string>(x => x.Contains("selected.name")));
        }

        [Test] public void IconFilesNamedInConfigExist()
        {
            BrandingConfig c = ConfigPaths.Branding();
            foreach (IconOption i in c.appIconOptions)
                Assert.That(ConfigPaths.Find(i.file), Is.Not.Null, i.file);
        }

        [Test] public void ConfigCopyInResourcesMatchesTopLevelConfig()
        {
            string top = ConfigPaths.Find("config/branding.json");
            if (top == null) Assert.Ignore("top-level config/ not reachable from this test host");
            Assert.That(JToken.DeepEquals(JObject.Parse(File.ReadAllText(top)), JObject.Parse(ConfigPaths.Read("Assets/Resources/Config/branding.json"))), Is.True);
        }
    }

    public class CharacterRosterTests
    {
        [Test] public void ShippedRosterIsValid() { Assert.That(ConfigPaths.Roster().Validate(), Is.Empty); }

        [Test] public void ExactlySixCharactersEachWithOwnPhotoAndTexture()
        {
            CharacterRoster r = ConfigPaths.Roster();
            Assert.That(r.characters.Count, Is.EqualTo(6));
            Assert.That(new HashSet<string>(r.characters.ConvertAll(c => c.faceTexture)).Count, Is.EqualTo(6));
            Assert.That(new HashSet<string>(r.characters.ConvertAll(c => c.source)).Count, Is.EqualTo(6));
        }

        [Test] public void FaceTexturesExistOnDisk()
        {
            foreach (CharacterDef c in ConfigPaths.Roster().characters)
                Assert.That(ConfigPaths.Find("Assets/Resources/Characters/" + c.faceTexture + ".png"), Is.Not.Null, c.faceTexture);
        }

        [Test] public void NoJerseyNumbersAndRoleMappingStillUnapproved()
        {
            CharacterRoster r = ConfigPaths.Roster();
            Assert.That(r.jerseyNumbersApproved, Is.False); Assert.That(r.roleMappingApproved, Is.False);
        }

        [Test] public void ValidateRejectsFiveCharacters()
        {
            CharacterRoster r = ConfigPaths.Roster(); r.characters.RemoveAt(5);
            Assert.That(r.Validate(), Has.Some.Matches<string>(x => x.Contains("exactly 6")));
        }

        [Test] public void ValidateRejectsSharedFaceTexture()
        {
            CharacterRoster r = ConfigPaths.Roster(); r.characters[1].faceTexture = r.characters[0].faceTexture;
            Assert.That(r.Validate(), Has.Some.Matches<string>(x => x.Contains("duplicate face texture")));
        }

        [Test] public void ValidateRejectsSwappedSourcePhotos()
        {
            CharacterRoster r = ConfigPaths.Roster(); string t = r.characters[0].source; r.characters[0].source = r.characters[1].source; r.characters[1].source = t;
            Assert.That(r.Validate(), Has.Some.Matches<string>(x => x.Contains("must use source")));
        }

        [Test] public void HexColourCheck()
        {
            Assert.That(CharacterRoster.IsHexColor("#A1b2C3"), Is.True); Assert.That(CharacterRoster.IsHexColor("A1B2C3"), Is.False); Assert.That(CharacterRoster.IsHexColor("#12345G"), Is.False);
        }
    }

    public class LoadingFlowTests
    {
        static LoadingFlow Make() { LoadingFlow f = new LoadingFlow(); f.AddStep("a", 1); f.AddStep("b", 3); return f; }

        [Test] public void ProgressIsWeighted() { LoadingFlow f = Make(); f.Complete("a"); Assert.That(f.Progress, Is.EqualTo(0.25).Within(1e-9)); f.Report("b", 0.5); Assert.That(f.Progress, Is.EqualTo(0.625).Within(1e-9)); }
        [Test] public void NotCompleteUntilEveryStepIsDone() { LoadingFlow f = Make(); f.Complete("a"); Assert.That(f.AllComplete, Is.False); f.Complete("b"); Assert.That(f.AllComplete, Is.True); }
        [Test] public void EmptyFlowIsNeverComplete() { Assert.That(new LoadingFlow().AllComplete, Is.False); }
        [Test] public void StepProgressNeverGoesBackwards() { LoadingFlow f = Make(); f.Report("b", 0.8); f.Report("b", 0.2); Assert.That(f.Progress, Is.EqualTo(0.6).Within(1e-9)); }
        [Test] public void BadFractionsAreClamped() { LoadingFlow f = Make(); f.Report("a", double.NaN); f.Report("b", 7.0); Assert.That(f.Progress, Is.EqualTo(0.75).Within(1e-9)); }
        [Test] public void UnknownOrDuplicateStepThrows()
        {
            LoadingFlow f = Make();
            Assert.Throws<InvalidOperationException>(() => f.Report("zzz", 1)); Assert.Throws<InvalidOperationException>(() => f.AddStep("a", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => f.AddStep("c", 0));
        }
        [Test] public void PercentIs1To99UntilReallyComplete()
        {
            Assert.That(LoadingFlow.PercentFor(0.0, false), Is.EqualTo(1)); Assert.That(LoadingFlow.PercentFor(0.5, false), Is.EqualTo(50));
            Assert.That(LoadingFlow.PercentFor(1.0, false), Is.EqualTo(99)); Assert.That(LoadingFlow.PercentFor(1.0, true), Is.EqualTo(100));
            Assert.That(LoadingFlow.PercentFor(0.7, true), Is.EqualTo(70));
        }
        [Test] public void SmoothedBarNeverRunsAheadOfRealProgressOrBackwards()
        {
            double shown = 0; double[] targets = { 0.1, 0.1, 0.4, 0.4, 0.9, 1.0, 1.0, 1.0 };
            foreach (double t in targets)
                for (int i = 0; i < 200; i++)
                {
                    double next = LoadingFlow.SmoothDisplay(shown, t, 1.0 / 60);
                    Assert.That(next, Is.LessThanOrEqualTo(t + 1e-12)); Assert.That(next, Is.GreaterThanOrEqualTo(shown)); shown = next;
                }
            Assert.That(shown, Is.EqualTo(1.0).Within(1e-9));
        }
        [Test] public void AutoAdvancesToLobbyOnlyAfterCompleteAndMinHold()
        {
            LoadingFlow f = Make(); Assert.That(f.Tick(5.0, 0.8), Is.False); Assert.That(f.View, Is.EqualTo(LoadingView.Intro));
            f.Complete("a"); f.Complete("b"); Assert.That(f.Tick(0.1, 100.0), Is.False);
            Assert.That(f.Tick(0.0, 0.8), Is.True); Assert.That(f.View, Is.EqualTo(LoadingView.Lobby)); Assert.That(f.Tick(1.0, 0.8), Is.False);
        }
        [Test] public void CloseButtonWorksAtAnyTimeAndLobbyPlayStaysDisabledUntilLoaded()
        {
            LoadingFlow f = Make(); f.Report("a", 0.3); f.RequestClose();
            Assert.That(f.Tick(0.016, 0.8), Is.True); Assert.That(f.View, Is.EqualTo(LoadingView.Lobby));
            Assert.That(f.LobbyPlayEnabled, Is.False);
            f.Complete("a"); f.Complete("b"); Assert.That(f.LobbyPlayEnabled, Is.True);
        }
        [Test] public void LobbyPlayDisabledBeforeLobby() { LoadingFlow f = Make(); f.Complete("a"); f.Complete("b"); Assert.That(f.LobbyPlayEnabled, Is.False); }
    }
}
