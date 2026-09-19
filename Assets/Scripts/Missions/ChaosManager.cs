using UnityEngine;
using Popayork.Enemies;
using Popayork.Player;
using Popayork.Weapons;

namespace Popayork.Missions
{
    // Caos ambiental: explosiones periódicas con sacudida y daño en área.
    // Los fuegos/humos son ParticleSystems en loop creados por el builder.
    public class ChaosManager : MonoBehaviour
    {
        [SerializeField] private float explosionInterval = 12f;
        [SerializeField] private float explosionRadius = 8f;
        [SerializeField] private float explosionDamage = 25f;
        [SerializeField] private float shakeRadius = 30f;
        [SerializeField] private Vector3[] blastPoints = new Vector3[0];

        private float timer;
        private int cursor;
        private WeaponAudio boom;
        private PlayerController player;
        private AgentPool pool;

        public void Bind(float interval, float radius, float damage, float shake, Vector3[] points)
        {
            explosionInterval = interval;
            explosionRadius = radius;
            explosionDamage = damage;
            shakeRadius = shake;
            blastPoints = points;
            timer = interval * 0.5f;
        }

        private void Awake()
        {
            boom = GetComponent<WeaponAudio>();
            if (boom == null)
            {
                boom = gameObject.AddComponent<WeaponAudio>();
            }
            boom.Configure(0.35f);
        }

        private void Update()
        {
            if (blastPoints == null || blastPoints.Length == 0)
            {
                return;
            }
            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }
            timer = explosionInterval;
            Explode(blastPoints[cursor % blastPoints.Length]);
            cursor++;
        }

        private void Explode(Vector3 point)
        {
            if (ImpactPool.Instance != null)
            {
                ImpactPool.Instance.SpawnImpact(point, Vector3.up);
                ImpactPool.Instance.SpawnImpact(point + Vector3.up, Vector3.up);
            }
            if (boom != null)
            {
                boom.PlayShot();
            }
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }
            if (pool == null)
            {
                pool = AgentPool.Instance;
            }
            if (player != null && (player.transform.position - point).sqrMagnitude <= shakeRadius * shakeRadius)
            {
                player.AddRecoil(2.5f, 0.6f);
            }
            if (player != null)
            {
                var health = player.GetComponent<PlayerHealth>();
                if (health != null && (player.transform.position - point).sqrMagnitude <= explosionRadius * explosionRadius)
                {
                    Vector3 dir = (player.transform.position - point).normalized;
                    health.TakeDamage(explosionDamage, dir);
                }
            }
            if (pool != null)
            {
                int count = pool.ActiveCount;
                for (int i = 0; i < count; i++)
                {
                    AgentBrain brain = pool.GetActive(i);
                    if (brain == null || brain.Health.IsDead)
                    {
                        continue;
                    }
                    if ((brain.transform.position - point).sqrMagnitude <= explosionRadius * explosionRadius)
                    {
                        brain.Health.TakeDamage(explosionDamage);
                    }
                }
            }
        }
    }
}
