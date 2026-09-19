using System;
using UnityEngine;

namespace Popayork.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        public static GameManager Instance
        {
            get { return instance; }
        }

        public static event Action<SaveData> SaveLoaded;

        [SerializeField] private SaveData currentSave;

        public SaveData CurrentSave
        {
            get { return currentSave; }
        }

        public float MasterVolume
        {
            get { return currentSave != null ? currentSave.masterVolume : GameConfig.DefaultVolume; }
        }

        public float MouseSensitivity
        {
            get { return currentSave != null ? currentSave.mouseSensitivity : GameConfig.DefaultSensitivity; }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            currentSave = SaveSystem.Load();
            ApplyAudio();
            if (SaveLoaded != null)
            {
                SaveLoaded(currentSave);
            }
        }

        public void SetVolume(float v)
        {
            if (currentSave == null)
            {
                return;
            }
            currentSave.masterVolume = Mathf.Clamp01(v);
            ApplyAudio();
            SaveSystem.Save(currentSave);
        }

        public void SetSensitivity(float s)
        {
            if (currentSave == null)
            {
                return;
            }
            currentSave.mouseSensitivity = Mathf.Clamp(s, 0.1f, 5.0f);
            SaveSystem.Save(currentSave);
        }

        public bool IsMissionUnlocked(string missionScene)
        {
            return currentSave != null && currentSave.IsUnlocked(missionScene);
        }

        public void UnlockMission(string missionScene)
        {
            if (currentSave == null || currentSave.IsUnlocked(missionScene))
            {
                return;
            }
            currentSave.unlockedMissions.Add(missionScene);
            SaveSystem.Save(currentSave);
        }

        public void CompleteMission(string missionScene)
        {
            if (currentSave == null)
            {
                return;
            }
            if (currentSave.completedMissions == null)
            {
                return;
            }
            if (!currentSave.completedMissions.Contains(missionScene))
            {
                currentSave.completedMissions.Add(missionScene);
            }
            SaveSystem.Save(currentSave);
        }

        public void ReloadSave()
        {
            currentSave = SaveSystem.Load();
            ApplyAudio();
            if (SaveLoaded != null)
            {
                SaveLoaded(currentSave);
            }
        }

        private void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
        }
    }
}
