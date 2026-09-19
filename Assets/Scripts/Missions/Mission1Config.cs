using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    [CreateAssetMenu(fileName = "Mission1Config", menuName = "Popayork/Mission1Config")]
    public class Mission1Config : ScriptableObject
    {
        [Header("Misión 1: Empieza el caos")]
        public float missionTime = 240.0f;
        public float captureRadius = 4.0f;
        public float warningRadius = 14.0f;
        public WaveData[] waves = new WaveData[0];
        public WaveData alliesWave;

        public int TotalPolice()
        {
            int n = 0;
            if (waves != null)
            {
                for (int w = 0; w < waves.Length; w++)
                {
                    if (waves[w] == null || waves[w].entries == null)
                    {
                        continue;
                    }
                    for (int e = 0; e < waves[w].entries.Length; e++)
                    {
                        if (waves[w].entries[e].faction == Faction.Police)
                        {
                            n += Mathf.Max(0, waves[w].entries[e].count);
                        }
                    }
                }
            }
            return n;
        }
    }
}
