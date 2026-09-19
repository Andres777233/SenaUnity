using UnityEngine;

namespace Popayork.Core
{
    public static class GameConfig
    {
        public const string MainMenuScene = "MainMenu";
        public const string CampaignScene = "Campaign";
        public const string Mission1Scene = "Mision1";
        public const string Mission2Scene = "Mision2";
        public const string Mission3Scene = "Mision3";
        public const string TestArenaScene = "TestArena";

        public const float DefaultVolume = 0.8f;
        public const float DefaultSensitivity = 1.0f;

        // Tecla de pausa: P (no usa Escape para no chocar con liberar cursor del Editor).
        public const KeyCode PauseKey = KeyCode.P;

        public const string SaveFileName = "popayork_save.json";

        public static readonly string[] AllScenes =
        {
            MainMenuScene,
            CampaignScene,
            Mission1Scene,
            Mission2Scene,
            Mission3Scene
        };

        public static readonly string[] MissionScenes =
        {
            Mission1Scene,
            Mission2Scene,
            Mission3Scene
        };
    }
}
