using UnityEngine;
using UnityEngine.UI;

namespace Popayork.UI
{
    public class MissionUI : MonoBehaviour
    {
        [SerializeField] private GameObject introPanel;
        [SerializeField] private Text introTitle;
        [SerializeField] private Text introBody;
        [SerializeField] private GameObject objectiveBar;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultText;

        private float lastProgress = -1f;

        public bool ObjectiveVisible
        {
            get { return objectiveBar != null && objectiveBar.activeSelf; }
        }

        public bool ResultVisible
        {
            get { return resultPanel != null && resultPanel.activeSelf; }
        }

        public string ResultContent
        {
            get { return resultText != null ? resultText.text : string.Empty; }
        }

        public void Bind(GameObject intro, Text title, Text body, GameObject objBar, Text objText, Image fill, GameObject result, Text resultLabel)
        {
            introPanel = intro;
            introTitle = title;
            introBody = body;
            objectiveBar = objBar;
            objectiveText = objText;
            progressFill = fill;
            resultPanel = result;
            resultText = resultLabel;
        }

        public void ShowIntro()
        {
            Time.timeScale = 0f;
            if (introPanel != null)
            {
                introPanel.SetActive(true);
            }
            if (objectiveBar != null)
            {
                objectiveBar.SetActive(false);
            }
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        public void ShowObjective(string objective)
        {
            Time.timeScale = 1f;
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }
            if (objectiveBar != null)
            {
                objectiveBar.SetActive(true);
            }
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
            lastProgress = -1f;
            SetProgress(0f);
        }

        public void SetProgress(float fraction)
        {
            float clamped = Mathf.Clamp01(fraction);
            if (Mathf.Abs(clamped - lastProgress) < 0.01f)
            {
                return;
            }
            lastProgress = clamped;
            if (progressFill != null)
            {
                progressFill.fillAmount = clamped;
            }
        }

        public void ShowResult(bool victory, string message)
        {
            Time.timeScale = 0f;
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }
            if (resultText != null)
            {
                resultText.text = message;
            }
        }
    }
}
