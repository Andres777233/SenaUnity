using UnityEngine;
using Popayork.Core;

namespace Popayork.Weapons
{
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private int size = 32;
        [SerializeField] private float projectileLife = 3.0f;

        private Projectile[] items;
        private int[] freeStack;
        private int freeCount;
        private Material sharedMaterial;

        public int Size
        {
            get { return size; }
        }

        public int FreeCount
        {
            get { return freeCount; }
        }

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            if (items != null)
            {
                return;
            }
            sharedMaterial = MaterialFactory.New();
            sharedMaterial.color = new Color(1.0f, 0.8f, 0.2f);
            sharedMaterial.EnableKeyword("_EMISSION");
            sharedMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.6f, 0.1f));
            items = new Projectile[size];
            freeStack = new int[size];
            for (int i = 0; i < size; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Projectile_" + i;
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.09f;
                var mesh = go.GetComponent<MeshRenderer>();
                mesh.sharedMaterial = sharedMaterial;
                var proj = go.AddComponent<Projectile>();
                proj.Configure(this);
                go.SetActive(false);
                items[i] = proj;
                freeStack[i] = i;
            }
            freeCount = size;
        }

        // Sin asignaciones en disparo: índice de pila preasignada.
        public bool TryLaunch(Vector3 origin, Vector3 direction, float speed, float damage)
        {
            if (freeCount <= 0)
            {
                return false;
            }
            freeCount--;
            int index = freeStack[freeCount];
            items[index].Launch(origin, direction, speed, damage, projectileLife);
            return true;
        }

        public void Return(Projectile proj)
        {
            for (int i = 0; i < size; i++)
            {
                if (items[i] == proj)
                {
                    freeStack[freeCount] = i;
                    freeCount++;
                    return;
                }
            }
        }

        // Devuelve todo al pool (reinicio de misión). Sin asignaciones.
        public void ReclaimAll()
        {
            freeCount = 0;
            for (int i = 0; i < size; i++)
            {
                items[i].gameObject.SetActive(false);
                freeStack[freeCount] = i;
                freeCount++;
            }
        }
    }
}
