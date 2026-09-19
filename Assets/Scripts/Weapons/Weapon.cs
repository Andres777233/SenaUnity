using System.Collections;
using UnityEngine;
using Popayork.Core;
using Popayork.Player;

namespace Popayork.Weapons
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponData data;
        [SerializeField] private Transform muzzle;

        private ProjectilePool pool;
        private ImpactPool impacts;
        private WeaponAudio audio;
        private PlayerController recoilTarget;
        private GameObject viewModel;
        private int magazine;
        private int reserve;
        private float cooldown;
        private bool reloading;

        public WeaponData Data
        {
            get { return data; }
        }

        public int Magazine
        {
            get { return magazine; }
        }

        public int Reserve
        {
            get { return reserve; }
        }

        public bool IsReloading
        {
            get { return reloading; }
        }

        public void Setup(WeaponData weaponData, Transform muzzlePoint, ProjectilePool projectilePool, ImpactPool impactPool, PlayerController owner)
        {
            data = weaponData;
            muzzle = muzzlePoint;
            pool = projectilePool;
            impacts = impactPool;
            recoilTarget = owner;
            audio = GetComponent<WeaponAudio>();
            if (audio == null)
            {
                audio = gameObject.AddComponent<WeaponAudio>();
            }
            audio.Configure(data.shotPitch);
            magazine = data.magazineSize;
            reserve = data.startingReserve;
            cooldown = 0.0f;
            reloading = false;
        }

        public void AttachViewModel(GameObject modelInstance)
        {
            if (viewModel != null)
            {
                Destroy(viewModel);
            }
            viewModel = modelInstance;
            if (viewModel != null)
            {
                viewModel.transform.SetParent(transform, false);
            }
        }

        private void Update()
        {
            cooldown = Mathf.Max(0.0f, cooldown - Time.deltaTime);
        }

        // Ruta de disparo sin asignaciones: aritmética + pool preasignado + eventos.
        public bool TryFire(Vector3 origin, Vector3 direction)
        {
            if (data == null || reloading || cooldown > 0.0f)
            {
                return false;
            }
            if (magazine <= 0)
            {
                if (audio != null)
                {
                    audio.PlayDry();
                }
                cooldown = 0.25f;
                return false;
            }
            if (pool == null || !pool.TryLaunch(origin, direction, data.projectileSpeed, data.damage))
            {
                return false;
            }
            magazine--;
            cooldown = 1.0f / Mathf.Max(0.01f, data.shotsPerSecond);
            if (recoilTarget != null)
            {
                recoilTarget.AddRecoil(data.recoilKickDegrees, data.shakeAmount);
            }
            if (impacts != null && muzzle != null)
            {
                impacts.SpawnMuzzle(muzzle.position, direction);
            }
            if (audio != null)
            {
                audio.PlayShot();
            }
            GameEvents.RaiseAmmoChanged(magazine, reserve);
            return true;
        }

        public void StartReload()
        {
            if (data == null || reloading || magazine >= data.magazineSize || reserve <= 0)
            {
                return;
            }
            reloading = true;
            if (audio != null)
            {
                audio.PlayReload();
            }
            StartCoroutine(ReloadRoutine());
        }

        // Recarga instantánea para reaparición y tests (sin corrutina).
        public void RefillAll()
        {
            if (data == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                StopAllCoroutines();
            }
            reloading = false;
            magazine = data.magazineSize;
            reserve = data.startingReserve;
            GameEvents.RaiseAmmoChanged(magazine, reserve);
        }

        private IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(data.reloadSeconds);
            int need = data.magazineSize - magazine;
            int take = Mathf.Min(need, reserve);
            magazine += take;
            reserve -= take;
            reloading = false;
            GameEvents.RaiseAmmoChanged(magazine, reserve);
        }
    }
}
