using UnityEngine;
using Popayork.Core;

namespace Popayork.World
{
    public class TargetDummy : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100.0f;
        [SerializeField] private float respawnSeconds = 2.0f;

        private float health;
        private bool down;
        private float stateTimer;
        private float flashTimer;
        private Material bodyMaterial;
        private Color baseColor = new Color(0.85f, 0.25f, 0.2f);
        private Color flashColor = Color.white;
        private Quaternion upRotation;
        private Vector3 basePosition;

        public float Health
        {
            get { return health; }
        }

        public bool IsDown
        {
            get { return down; }
        }

        private void Awake()
        {
            health = maxHealth;
            upRotation = transform.rotation;
            basePosition = transform.position;
            var renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                bodyMaterial = renderer.material;
                bodyMaterial.color = baseColor;
            }
        }

        private void Update()
        {
            if (flashTimer > 0.0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0.0f && bodyMaterial != null && !down)
                {
                    bodyMaterial.color = baseColor;
                }
            }
            if (!down)
            {
                return;
            }
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0.0f)
            {
                StandUp();
            }
        }

        // Sin asignaciones: aritmética + cambio de color + eventos.
        public void TakeHit(float damage, Vector3 direction)
        {
            if (down)
            {
                return;
            }
            health -= damage;
            if (bodyMaterial != null)
            {
                bodyMaterial.color = flashColor;
                flashTimer = 0.08f;
            }
            bool killed = health <= 0.0f;
            GameEvents.RaiseTargetHit(killed);
            if (killed)
            {
                KnockDown(direction);
            }
        }

        private void KnockDown(Vector3 direction)
        {
            down = true;
            stateTimer = respawnSeconds;
            Vector3 flat = direction;
            flat.y = 0.0f;
            if (flat.sqrMagnitude < 0.001f)
            {
                flat = transform.forward;
            }
            Quaternion fall = Quaternion.LookRotation(flat.normalized) * Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            transform.rotation = fall;
        }

        private void StandUp()
        {
            down = false;
            health = maxHealth;
            transform.rotation = upRotation;
            transform.position = basePosition;
            if (bodyMaterial != null)
            {
                bodyMaterial.color = baseColor;
            }
        }
    }
}
