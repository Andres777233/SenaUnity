using UnityEngine;
using UnityEngine.UI;
using Popayork.Core;

namespace Popayork.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text weaponText;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private Image hitmarker;
        [SerializeField] private Image[] damageArrows = new Image[4];
        [SerializeField] private Text respawnText;

        private Camera viewCamera;
        private float hitmarkerTimer;
        private float damageTimer;
        private int damageArrowIndex;
        private int lastAmmoShown = -1;
        private int lastReserveShown = -1;

        public void Bind(Image health, Text ammo, Text weapon, GameObject cross, Image marker, Image[] arrows, Text respawn)
        {
            healthFill = health;
            ammoText = ammo;
            weaponText = weapon;
            crosshair = cross;
            hitmarker = marker;
            damageArrows = arrows;
            respawnText = respawn;
        }

        private void Awake()
        {
            viewCamera = GetComponentInParent<Camera>();
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            GameEvents.HealthChanged += OnHealthChanged;
            GameEvents.AmmoChanged += OnAmmoChanged;
            GameEvents.TargetHit += OnTargetHit;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.WeaponSwitched += OnWeaponSwitched;
        }

        private void OnDisable()
        {
            GameEvents.HealthChanged -= OnHealthChanged;
            GameEvents.AmmoChanged -= OnAmmoChanged;
            GameEvents.TargetHit -= OnTargetHit;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.WeaponSwitched -= OnWeaponSwitched;
        }

        private void Update()
        {
            if (hitmarkerTimer > 0.0f)
            {
                hitmarkerTimer -= Time.deltaTime;
                if (hitmarkerTimer <= 0.0f && hitmarker != null)
                {
                    hitmarker.enabled = false;
                }
            }
            if (damageTimer > 0.0f)
            {
                damageTimer -= Time.deltaTime;
                if (damageTimer <= 0.0f)
                {
                    HideArrows();
                }
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (healthFill != null && max > 0.0f)
            {
                healthFill.fillAmount = Mathf.Clamp01(current / max);
            }
            if (current > 0.0f && respawnText != null)
            {
                respawnText.enabled = false;
            }
        }

        private void OnAmmoChanged(int magazine, int reserve)
        {
            if (magazine == lastAmmoShown && reserve == lastReserveShown)
            {
                return;
            }
            lastAmmoShown = magazine;
            lastReserveShown = reserve;
            if (ammoText != null)
            {
                ammoText.text = magazine.ToString() + " / " + reserve.ToString();
            }
        }

        private void OnWeaponSwitched(string weaponName)
        {
            if (weaponText != null)
            {
                weaponText.text = weaponName;
            }
        }

        private void OnTargetHit(bool killed)
        {
            if (hitmarker != null)
            {
                hitmarker.enabled = true;
                hitmarker.color = killed ? Color.red : Color.white;
            }
            hitmarkerTimer = 0.18f;
        }

        private void OnPlayerDamaged(Vector3 worldDirection)
        {
            if (viewCamera == null || damageArrows == null || damageArrows.Length < 4)
            {
                return;
            }
            Vector3 local = viewCamera.transform.InverseTransformDirection(worldDirection);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            if (angle < 0.0f)
            {
                angle += 360.0f;
            }
            damageArrowIndex = Mathf.Clamp(Mathf.RoundToInt(angle / 90.0f) % 4, 0, 3);
            ShowArrow(damageArrowIndex);
            damageTimer = 0.9f;
        }

        private void OnPlayerDied()
        {
            if (respawnText != null)
            {
                respawnText.enabled = true;
                respawnText.text = "Reapareciendo...";
            }
        }

        private void ShowArrow(int index)
        {
            for (int i = 0; i < damageArrows.Length; i++)
            {
                if (damageArrows[i] != null)
                {
                    damageArrows[i].enabled = i == index;
                }
            }
        }

        private void HideArrows()
        {
            for (int i = 0; i < damageArrows.Length; i++)
            {
                if (damageArrows[i] != null)
                {
                    damageArrows[i].enabled = false;
                }
            }
        }
    }
}
