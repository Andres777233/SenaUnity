using UnityEngine;
using UnityEngine.UI;
using Popayork.Player;

namespace Popayork.UI
{
    public class CompassUI : MonoBehaviour
    {
        [SerializeField] private RectTransform arrow;
        [SerializeField] private Text distanceText;
        [SerializeField] private Vector3 target;
        private string targetLabel = "Morro";

        private PlayerController player;
        private int lastMeters = -1;

        public Vector3 Target
        {
            get { return target; }
        }

        public void Bind(RectTransform arrowRect, Text distance, Vector3 morroTarget)
        {
            arrow = arrowRect;
            distanceText = distance;
            target = morroTarget;
            targetLabel = "Morro";
        }

        public void SetTarget(Vector3 newTarget, string label)
        {
            target = newTarget;
            targetLabel = string.IsNullOrEmpty(label) ? "Morro" : label;
            lastMeters = -1;
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }
            if (player == null)
            {
                return;
            }
            Vector3 diff = target - player.transform.position;
            float dist = diff.magnitude;
            if (distanceText != null)
            {
                int meters = Mathf.RoundToInt(dist);
                if (meters != lastMeters)
                {
                    lastMeters = meters;
                    distanceText.text = targetLabel + ": " + meters + "m";
                }
            }
            if (arrow != null)
            {
                Vector3 flat = diff;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    float worldAngle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                    float yaw = player.transform.eulerAngles.y;
                    arrow.rotation = Quaternion.Euler(0f, 0f, yaw - worldAngle);
                }
            }
        }
    }
}
