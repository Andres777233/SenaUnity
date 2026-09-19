using UnityEngine;
using UnityEngine.UI;
using Popayork.Core;

namespace Popayork.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private GameObject optionsPanel;
        private bool isPaused;
        private MissionUI missionUI;

        public void Bind(GameObject pausePanel, Slider volume, Slider sensitivity, GameObject options)
        {
            panel = pausePanel;
            volumeSlider = volume;
            sensitivitySlider = sensitivity;
            optionsPanel = options;
        }

        private void Start()
        {
            SetPaused(false);
            RefreshFromSave();
            missionUI = FindAnyObjectByType<MissionUI>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(GameConfig.PauseKey))
            {
                if (missionUI != null && (missionUI.IntroVisible || missionUI.ResultVisible))
                {
                    return;
                }
                SetPaused(!isPaused);
            }
        }

        public void OnContinuePressed()
        {
            SetPaused(false);
        }

        public void OnOptionsPressed()
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(!optionsPanel.activeSelf);
            }
        }

        public void OnMenuPressed()
        {
            SetPaused(false);
            SceneLoader.Load(GameConfig.MainMenuScene);
        }

        public void OnVolumeChanged(float v)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetVolume(v);
            }
        }

        public void OnSensitivityChanged(float s)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSensitivity(s);
            }
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (panel != null)
            {
                panel.SetActive(paused);
            }
            if (!paused && optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        private void RefreshFromSave()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentSave == null)
            {
                return;
            }
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(GameManager.Instance.MasterVolume);
            }
            if (sensitivitySlider != null)
            {
                sensitivitySlider.SetValueWithoutNotify(GameManager.Instance.MouseSensitivity);
            }
        }
    }
}
