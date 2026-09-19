using UnityEngine;

namespace Popayork.World
{
    public class TrackObstacle : MonoBehaviour
    {
        [Header("Penalización al chocar (sin muerte instantánea)")]
        public float radius = 1.4f;
        public float speedKeep = 0.5f;
        public float damage = 10f;
        public float spinDegrees = 40f;
        public bool isRamp;
        public float rampBoost = 7f;
    }
}
