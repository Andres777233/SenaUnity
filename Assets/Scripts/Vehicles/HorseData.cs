using UnityEngine;

namespace Popayork.Vehicles
{
    [CreateAssetMenu(fileName = "HorseData", menuName = "Popayork/HorseData")]
    public class HorseData : ScriptableObject
    {
        [Header("Caballo (modelo real HorseMesh.json)")]
        public float trotSpeed = 6.0f;
        public float gallopSpeed = 11.0f;
        public float turnSpeed = 120.0f;
        public float gravity = 22.0f;
        public float mountRange = 3.5f;
        public float saddleHeight = 1.55f;
        public float saddleForward = 0.1f;
        public float targetLengthMeters = 2.2f;
        public float corridorHalfWidth = 12.0f;
    }
}
