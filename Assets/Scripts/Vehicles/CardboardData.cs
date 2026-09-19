using UnityEngine;

namespace Popayork.Vehicles
{
    [CreateAssetMenu(fileName = "CardboardData", menuName = "Popayork/CardboardData")]
    public class CardboardData : ScriptableObject
    {
        [Header("Cartón: descenso al río")]
        public float gravity = 22f;
        public float maxSpeed = 28f;
        public float drag = 0.12f;
        public float brakeDrag = 1.6f;
        public float turnRate = 90f;
        public float corridorHalfWidth = 9f;
        public float baseFov = 75f;
        public float maxFovBoost = 15f;
        public float seatHeight = 0.7f;
    }
}
