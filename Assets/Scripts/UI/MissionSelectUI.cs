using UnityEngine;
using UnityEngine.UI;
using Popayork.Core;

namespace Popayork.UI
{
    public class MissionSelectUI : MonoBehaviour
    {
        [SerializeField] private Button mission1Button;
        [SerializeField] private Button mission2Button;
        [SerializeField] private Button mission3Button;
        [SerializeField] private Text statusText;

        public void Bind(Button m1, Button m2, Button m3, Text status)
        {
            mission1Button = m1;
            mission2Button = m2;
            mission3Button = m3;
            statusText = status;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            GameManager.SaveLoaded += OnSaveLoaded;
        }

        private void OnDisable()
        {
            GameManager.SaveLoaded -= OnSaveLoaded;
        }

        private void OnSaveLoaded(SaveData data)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentSave == null)
            {
                return;
            }
            SetInteractable(mission1Button, GameManager.Instance.IsMissionUnlocked(GameConfig.Mission1Scene));
            SetInteractable(mission2Button, GameManager.Instance.IsMissionUnlocked(GameConfig.Mission2Scene));
            SetInteractable(mission3Button, GameManager.Instance.IsMissionUnlocked(GameConfig.Mission3Scene));
            if (statusText != null)
            {
                int n = 0;
                if (GameManager.Instance.IsMissionUnlocked(GameConfig.Mission1Scene))
                {
                    n++;
                }
                if (GameManager.Instance.IsMissionUnlocked(GameConfig.Mission2Scene))
                {
                    n++;
                }
                if (GameManager.Instance.IsMissionUnlocked(GameConfig.Mission3Scene))
                {
                    n++;
                }
                statusText.text = "Progreso: " + n + "/3 misiones";
            }
        }

        public void OnMissionPressed(int index)
        {
            string scene = GameConfig.Mission1Scene;
            if (index == 2)
            {
                scene = GameConfig.Mission2Scene;
            }
            else if (index == 3)
            {
                scene = GameConfig.Mission3Scene;
            }
            if (GameManager.Instance == null || !GameManager.Instance.IsMissionUnlocked(scene))
            {
                return;
            }
            SceneLoader.Load(scene);
        }

        public void OnBackPressed()
        {
            SceneLoader.Load(GameConfig.MainMenuScene);
        }

        private static void SetInteractable(Button b, bool enabled)
        {
            if (b != null)
            {
                b.interactable = enabled;
            }
        }
    }
}
