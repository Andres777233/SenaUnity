using System;
using System.Collections.Generic;

namespace Popayork.Core
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public List<string> unlockedMissions = new List<string>();
        public List<string> completedMissions = new List<string>();
        public float masterVolume = 0.8f;
        public float mouseSensitivity = 1.0f;

        public static SaveData NewDefault()
        {
            var data = new SaveData();
            data.unlockedMissions.Add(GameConfig.Mission1Scene);
            return data;
        }

        public bool IsUnlocked(string missionScene)
        {
            return unlockedMissions != null && unlockedMissions.Contains(missionScene);
        }
    }
}
