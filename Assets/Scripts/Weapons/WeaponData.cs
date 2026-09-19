using UnityEngine;

namespace Popayork.Weapons
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Popayork/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identidad")]
        public string displayName = "Arma";
        public string modelRootName = "MSR";

        [Header("Disparo")]
        public float shotsPerSecond = 8.0f;
        public float damage = 25.0f;
        public float projectileSpeed = 90.0f;
        public bool automatic = true;

        [Header("Munición")]
        public int magazineSize = 30;
        public int startingReserve = 90;
        public float reloadSeconds = 1.6f;

        [Header("Game feel")]
        public float recoilKickDegrees = 1.2f;
        public float shakeAmount = 0.25f;
        public float targetLengthMeters = 0.7f;

        [Header("Sonido placeholder")]
        public float shotPitch = 1.0f;
    }
}
