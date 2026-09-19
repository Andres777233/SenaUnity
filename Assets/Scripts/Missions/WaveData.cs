using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    [System.Serializable]
    public struct WaveEntry
    {
        public Faction faction;
        public int count;
    }

    [CreateAssetMenu(fileName = "WaveData", menuName = "Popayork/WaveData")]
    public class WaveData : ScriptableObject
    {
        public string waveName = "Oleada 1";
        public WaveEntry[] entries = new WaveEntry[1];
        public float spawnInterval = 0.8f;
        public int TotalCount
        {
            get
            {
                int n = 0;
                if (entries != null)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        n += Mathf.Max(0, entries[i].count);
                    }
                }
                return n;
            }
        }
    }
}
