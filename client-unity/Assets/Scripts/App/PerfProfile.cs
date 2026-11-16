// UNTESTED on a device. Android performance defaults: 60 fps target, no vsync double-cap, cheap shadows on low-RAM phones.
using UnityEngine;

namespace NextPlay.App
{
    public static class PerfProfile
    {
        public static bool LowEnd { get { int mem = SystemInfo.systemMemorySize; return mem > 0 && mem < 3500; } }

        public static void Apply()
        {
            bool low = LowEnd;
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.shadows = low ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = low ? ShadowResolution.Low : ShadowResolution.Medium;
            QualitySettings.shadowDistance = low ? 28f : 45f;
            QualitySettings.pixelLightCount = low ? 1 : 2;
            QualitySettings.antiAliasing = low ? 0 : 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.softParticles = false;
            Debug.Log("NextPlay perf profile: " + (low ? "LOW-END" : "STANDARD") + " (RAM " + SystemInfo.systemMemorySize + " MB, " + SystemInfo.graphicsDeviceName + ")");
        }
    }
}
