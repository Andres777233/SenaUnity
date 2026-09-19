using UnityEngine;
using Popayork.Core;

namespace Popayork.Weapons
{
    public class ImpactPool : MonoBehaviour
    {
        public static ImpactPool Instance { get; private set; }

        [SerializeField] private int size = 24;

        private ParticleSystem[] items;
        private int cursor;

        public int Size
        {
            get { return size; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Build();
        }

        public void Build()
        {
            if (items != null)
            {
                return;
            }
            items = new ParticleSystem[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("Impact_" + i);
                go.transform.SetParent(transform, false);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.35f;
                main.startLifetime = 0.35f;
                main.startSpeed = 6.0f;
                main.startSize = 0.12f;
                main.maxParticles = 24;
                var emission = ps.emission;
                emission.rateOverTime = 0.0f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 18) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.05f;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.material = MaterialFactory.New();
                renderer.material.color = new Color(1.0f, 0.7f, 0.25f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.15f));
                go.SetActive(false);
                items[i] = ps;
            }
            cursor = 0;
        }

        // Sin asignaciones: round-robin sobre arreglo preasignado.
        public void SpawnImpact(Vector3 position, Vector3 normal)
        {
            if (items == null || items.Length == 0)
            {
                return;
            }
            ParticleSystem ps = items[cursor];
            cursor = (cursor + 1) % items.Length;
            ps.transform.position = position + normal * 0.05f;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        public void SpawnMuzzle(Vector3 position, Vector3 direction)
        {
            SpawnImpact(position, direction);
        }
    }
}
