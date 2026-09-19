using UnityEngine;
using Popayork.Core;

namespace Popayork.Player
{
    public class PlayerWeapons : MonoBehaviour
    {
        [SerializeField] private Weapons.Weapon[] weapons = new Weapons.Weapon[2];
        [SerializeField] private Transform fireOrigin;

        private int activeIndex;
        private PlayerController controller;

        public Weapons.Weapon ActiveWeapon
        {
            get
            {
                if (weapons == null || weapons.Length == 0)
                {
                    return null;
                }
                return weapons[activeIndex];
            }
        }

        public void Bind(Weapons.Weapon[] owned, Transform origin)
        {
            weapons = owned;
            fireOrigin = origin;
            activeIndex = 0;
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            var projectilePool = FindAnyObjectByType<Weapons.ProjectilePool>();
            var impactPool = FindAnyObjectByType<Weapons.ImpactPool>();
            if (projectilePool != null)
            {
                projectilePool.Build();
            }
            if (impactPool != null)
            {
                impactPool.Build();
            }
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i] != null && weapons[i].Data != null)
                    {
                        weapons[i].Setup(weapons[i].Data, fireOrigin, projectilePool, impactPool, controller);
                    }
                }
            }
        }

        private void Start()
        {
            Select(0);
        }

        private void Update()
        {
            if (weapons == null || weapons.Length == 0)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Select(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Length > 1)
            {
                Select(1);
            }
            Weapons.Weapon active = ActiveWeapon;
            if (active == null || active.Data == null)
            {
                return;
            }
            bool wantFire = active.Data.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (wantFire && fireOrigin != null)
            {
                active.TryFire(fireOrigin.position, fireOrigin.forward);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                active.StartReload();
            }
        }

        public void Select(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            {
                return;
            }
            activeIndex = index;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[i].gameObject.SetActive(i == activeIndex);
                }
            }
            Weapons.Weapon active = weapons[activeIndex];
            GameEvents.RaiseAmmoChanged(active.Magazine, active.Reserve);
            GameEvents.RaiseWeaponSwitched(active.Data != null ? active.Data.displayName : string.Empty);
        }

        public void RefillAll()
        {
            if (weapons == null)
            {
                return;
            }
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[i].RefillAll();
                }
            }
            Select(activeIndex);
        }
    }
}
