using System.Collections;
using UnityEngine;
using Popayork.Core;

namespace Popayork.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private float health;
        private bool dead;
        private Vector3 spawnPosition;
        private float spawnYaw;

        public float Health
        {
            get { return health; }
        }

        public float MaxHealth
        {
            get { return config != null ? config.maxHealth : 100.0f; }
        }

        public bool IsDead
        {
            get { return dead; }
        }

        private void Awake()
        {
            spawnPosition = transform.position;
            spawnYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            health = MaxHealth;
            GameEvents.RaiseHealthChanged(health, MaxHealth);
        }

        public void SetSpawn(Vector3 position, float yaw)
        {
            spawnPosition = position;
            spawnYaw = yaw;
        }

        public void TakeDamage(float amount, Vector3 fromDirection)
        {
            if (dead || amount <= 0.0f)
            {
                return;
            }
            health = Mathf.Max(0.0f, health - amount);
            GameEvents.RaiseHealthChanged(health, MaxHealth);
            GameEvents.RaisePlayerDamaged(fromDirection);
            if (health <= 0.0f)
            {
                dead = true;
                GameEvents.RaisePlayerDied();
                if (Application.isPlaying)
                {
                    StartCoroutine(RespawnRoutine());
                }
            }
        }

        public void Refill()
        {
            health = MaxHealth;
            GameEvents.RaiseHealthChanged(health, MaxHealth);
        }

        // Reaparición en menos de 3 s (config.respawnDelay = 1.5). Llamable directo para tests.
        public void RespawnNow()
        {
            if (Application.isPlaying)
            {
                StopAllCoroutines();
            }
            dead = false;
            health = MaxHealth;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.Euler(0f, spawnYaw, 0f);
            var weapons = GetComponent<PlayerWeapons>();
            if (weapons != null)
            {
                weapons.RefillAll();
            }
            GameEvents.RaiseHealthChanged(health, MaxHealth);
        }

        private IEnumerator RespawnRoutine()
        {
            float delay = config != null ? config.respawnDelay : 1.5f;
            yield return new WaitForSeconds(delay);
            RespawnNow();
        }
    }
}
