using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Popayork.Core
{
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;

        public static SceneLoader Instance
        {
            get { return instance; }
        }

        private Canvas loadingCanvas;
        private Text loadingText;
        private bool isLoading;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLoadingCanvas();
        }

        public static void Load(string sceneName)
        {
            if (Instance != null)
            {
                Instance.LoadInternal(sceneName);
                return;
            }
            SceneManager.LoadScene(sceneName);
        }

        public void LoadInternal(string sceneName)
        {
            if (isLoading)
            {
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
            Time.timeScale = 1f;
            SetLoadingVisible(true, "Cargando " + sceneName + "...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                SetProgress(op.progress);
                yield return null;
            }
            SetLoadingVisible(false, string.Empty);
            isLoading = false;
        }

        private void BuildLoadingCanvas()
        {
            var root = new GameObject("LoadingScreen");
            root.transform.SetParent(transform, false);
            loadingCanvas = root.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 999;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(root.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            loadingText = label.AddComponent<Text>();
            loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadingText.alignment = TextAnchor.MiddleCenter;
            loadingText.fontSize = 28;
            loadingText.color = Color.white;
            var labelRect = loadingText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.4f);
            labelRect.anchorMax = new Vector2(0.9f, 0.6f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            root.SetActive(false);
        }

        private void SetLoadingVisible(bool visible, string message)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(visible);
            }
            if (visible && loadingText != null)
            {
                loadingText.text = message;
            }
        }

        private void SetProgress(float progress)
        {
            if (loadingText != null && loadingCanvas.gameObject.activeSelf)
            {
                int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
                string baseMsg = loadingText.text;
                int idx = baseMsg.IndexOf(" (");
                if (idx >= 0)
                {
                    baseMsg = baseMsg.Substring(0, idx);
                }
                loadingText.text = baseMsg + " (" + pct + "%)";
            }
        }
    }
}
