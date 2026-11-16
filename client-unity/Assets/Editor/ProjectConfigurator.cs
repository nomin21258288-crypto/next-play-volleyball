// UNTESTED in Unity. Applies branding + Android settings from config/branding.json every time the project loads (also in CI batch mode),
// so the APK name, package id and icon always follow the config file. Nothing here changes gameplay.
using System.IO;
using NextPlay.App;
using NextPlay.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace NextPlay.EditorTools
{
    public static class ProjectConfigurator
    {
        const string BrandingPath = "Assets/Resources/Config/branding.json";
        const string BootScene = "Assets/Scenes/Boot.unity";

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            try { Apply(); } catch (System.Exception e) { Debug.LogError("NextPlay ProjectConfigurator failed: " + e); }
        }

        [MenuItem("NextPlay/Apply branding and Android settings")]
        public static void Apply()
        {
            if (!File.Exists(BrandingPath)) { Debug.LogWarning("NextPlay: " + BrandingPath + " not found, skipping."); return; }
            BrandingConfig b = BrandingConfig.Parse(File.ReadAllText(BrandingPath));
            foreach (string problem in b.Validate()) Debug.LogError("NextPlay branding config: " + problem);

            PlayerSettings.productName = b.Name.launcherLabel;               // launcher label under the icon
            PlayerSettings.companyName = "NextPlay";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, b.android.packageId);
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(b.AppIcon.file);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else Debug.LogWarning("NextPlay: icon not imported yet: " + b.AppIcon.file);

            EnsureAlwaysIncludedShaders();
            if (File.Exists(BootScene)) EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
            Debug.Log("NextPlay: applied branding '" + b.Name.display + "' icon '" + b.AppIcon.id + "' package '" + b.android.packageId + "'.");
        }

        // Shader.Find() at runtime only works for shaders that are in the build. Without this the Android build can render pink / invisible.
        static void EnsureAlwaysIncludedShaders()
        {
            GraphicsSettings gs = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) { Debug.LogWarning("NextPlay: GraphicsSettings.asset not loadable; shader list not updated."); return; }
            SerializedObject so = new SerializedObject(gs); SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            foreach (string name in ShaderLib.RequiredShaders)
            {
                Shader sh = Shader.Find(name); if (sh == null) continue;
                bool has = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
                if (!has) { arr.arraySize++; arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
            }
            so.ApplyModifiedProperties();
        }

        // Command-line / Unity Build Automation entry: -executeMethod NextPlay.EditorTools.ProjectConfigurator.BuildAndroidApk
        [MenuItem("NextPlay/Build Android APK")]
        public static void BuildAndroidApk()
        {
            Apply();
            EditorUserBuildSettings.buildAppBundle = false;
            BuildPlayerOptions o = new BuildPlayerOptions { scenes = new[] { BootScene }, locationPathName = "Builds/Android/NextPlay.apk", target = BuildTarget.Android, options = BuildOptions.None };
            BuildReport r = BuildPipeline.BuildPlayer(o);
            Debug.Log("NextPlay build result: " + r.summary.result + ", size " + r.summary.totalSize + " bytes");
            if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
