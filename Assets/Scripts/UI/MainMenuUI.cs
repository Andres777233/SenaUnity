using UnityEngine;
using UnityEngine.UI;
using Popayork.Core;

namespace Popayork.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private GameObject optionsPanel;

        public void Bind(Slider volume, Slider sensitivity, GameObject options)
        {
            volumeSlider = volume;
            sensitivitySlider = sensitivity;
            optionsPanel = options;
        }

        private void Start()
        {
            RefreshFromSave();
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
            RefreshFromSave();
        }

        public void RefreshFromSave()
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

        public void OnCampaignPressed()
        {
            SceneLoader.Load(GameConfig.CampaignScene);
        }

        public void OnOptionsPressed()
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(!optionsPanel.activeSelf);
            }
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
    }
}
