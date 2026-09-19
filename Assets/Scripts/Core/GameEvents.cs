using System;
using UnityEngine;

namespace Popayork.Core
{
    public static class GameEvents
    {
        public static event Action<float, float> HealthChanged;
        public static event Action<int, int> AmmoChanged;
        public static event Action<bool> TargetHit;
        public static event Action<Vector3> PlayerDamaged;
        public static event Action PlayerDied;
        public static event Action<string> WeaponSwitched;

        public static void RaiseHealthChanged(float current, float max)
        {
            var h = HealthChanged;
            if (h != null)
            {
                h(current, max);
            }
        }

        public static void RaiseAmmoChanged(int magazine, int reserve)
        {
            var h = AmmoChanged;
            if (h != null)
            {
                h(magazine, reserve);
            }
        }

        public static void RaiseTargetHit(bool killed)
        {
            var h = TargetHit;
            if (h != null)
            {
                h(killed);
            }
        }

        public static void RaisePlayerDamaged(Vector3 worldDirection)
        {
            var h = PlayerDamaged;
            if (h != null)
            {
                h(worldDirection);
            }
        }

        public static void RaisePlayerDied()
        {
            var h = PlayerDied;
            if (h != null)
            {
                h();
            }
        }

        public static void RaiseWeaponSwitched(string weaponName)
        {
            var h = WeaponSwitched;
            if (h != null)
            {
                h(weaponName);
            }
        }
    }
}
