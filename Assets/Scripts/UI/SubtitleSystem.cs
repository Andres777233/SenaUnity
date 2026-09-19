using UnityEngine;
using UnityEngine.UI;

namespace Popayork.UI
{
    public class SubtitleSystem : MonoBehaviour
    {
        public static SubtitleSystem Instance { get; private set; }

        [SerializeField] private Text subtitleText;
        [SerializeField] private float defaultSeconds = 2.5f;

        private float hideTimer;

        public string CurrentText
        {
            get { return subtitleText != null ? subtitleText.text : string.Empty; }
        }

        public void Bind(Text text)
        {
            subtitleText = text;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (subtitleText != null)
            {
                subtitleText.enabled = false;
            }
        }

        private void Update()
        {
            if (hideTimer > 0.0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0.0f && subtitleText != null)
                {
                    subtitleText.enabled = false;
                }
            }
        }

        public void ShowLine(string line, float seconds)
        {
            if (subtitleText == null || string.IsNullOrEmpty(line))
            {
                return;
            }
            subtitleText.text = line;
            subtitleText.enabled = true;
            hideTimer = seconds > 0.0f ? seconds : defaultSeconds;
        }
    }
}
