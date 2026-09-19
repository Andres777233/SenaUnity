using UnityEngine;

namespace Popayork.Player
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Popayork/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.0f;
        public float jumpHeight = 1.2f;
        public float gravity = 22.0f;
        public float groundStickForce = 2.0f;

        [Header("Cámara")]
        public float lookSpeed = 2.2f;
        public float minPitch = -85.0f;
        public float maxPitch = 85.0f;
        public float normalFov = 75.0f;
        public float sprintFov = 85.0f;
        public float fovBlendSpeed = 8.0f;

        [Header("Retroceso y sacudida")]
        public float recoilRecoverSpeed = 10.0f;
        public float shakeDecaySpeed = 3.0f;

        [Header("Vida y reaparición")]
        public float maxHealth = 100.0f;
        public float respawnDelay = 1.5f;
    }
}
