using UnityEngine;

namespace Popayork.Core
{
    // Aplica 60 FPS y la calidad guardada al arrancar, sin tocar escenas.
    public static class PerfBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            ApplyQuality(save.quality);
        }

        public static void ApplyQuality(int quality)
        {
            if (quality == 1)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.pixelLightCount = 1;
                Application.targetFrameRate = 30;
            }
            else
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.pixelLightCount = 2;
                Application.targetFrameRate = 60;
            }
        }
    }
}
