using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    public class WaveManager : MonoBehaviour
    {
        public const int MaxActiveAgents = 30;

        [SerializeField] private AgentPool pool;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform objective;

        private WaveData activeWave;
        private int entryIndex;
        private int entryRemaining;
        private int spawnCursor;
        private float spawnTimer;
        private bool running;

        public bool IsRunning
        {
            get { return running; }
        }

        public void Bind(AgentPool agentPool, Transform[] spawns, Transform objectivePoint)
        {
            pool = agentPool;
            spawnPoints = spawns;
            objective = objectivePoint;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // Un paso de oleada sin asignaciones: lo usa Update y el Verify.
        public void Tick(float dt)
        {
            if (!running || activeWave == null)
            {
                return;
            }
            spawnTimer -= dt;
            if (spawnTimer > 0.0f)
            {
                return;
            }
            spawnTimer = Mathf.Max(0.05f, activeWave.spawnInterval);
            SpawnNext();
            if (entryIndex >= EntryCount())
            {
                running = false;
            }
        }

        // Oleada completa inmediata para tests (respeta el tope de 30 activos).
        public int SpawnWaveNow(WaveData wave)
        {
            if (wave == null || pool == null)
            {
                return 0;
            }
            int spawned = 0;
            int n = wave.entries != null ? wave.entries.Length : 0;
            for (int e = 0; e < n; e++)
            {
                for (int i = 0; i < wave.entries[e].count; i++)
                {
                    if (pool.ActiveCount >= MaxActiveAgents)
                    {
                        return spawned;
                    }
                    if (SpawnOne(wave.entries[e].faction) != null)
                    {
                        spawned++;
                    }
                }
            }
            return spawned;
        }

        public void StartWave(WaveData wave)
        {
            activeWave = wave;
            entryIndex = 0;
            entryRemaining = EntryCount() > 0 ? Mathf.Max(0, wave.entries[0].count) : 0;
            spawnTimer = 0.0f;
            running = wave != null && wave.TotalCount > 0;
        }

        public void StopWave()
        {
            running = false;
            activeWave = null;
        }

        private int EntryCount()
        {
            return activeWave != null && activeWave.entries != null ? activeWave.entries.Length : 0;
        }

        private void SpawnNext()
        {
            if (pool.ActiveCount >= MaxActiveAgents)
            {
                return;
            }
            while (entryIndex < EntryCount() && entryRemaining <= 0)
            {
                entryIndex++;
                if (entryIndex < EntryCount())
                {
                    entryRemaining = Mathf.Max(0, activeWave.entries[entryIndex].count);
                }
            }
            if (entryIndex >= EntryCount())
            {
                return;
            }
            if (SpawnOne(activeWave.entries[entryIndex].faction) != null)
            {
                entryRemaining--;
            }
        }

        private AgentBrain SpawnOne(Faction faction)
        {
            if (pool == null || spawnPoints == null || spawnPoints.Length == 0 || objective == null)
            {
                return null;
            }
            Transform spot = spawnPoints[spawnCursor % spawnPoints.Length];
            spawnCursor++;
            return pool.Spawn(faction, spot.position, objective.position);
        }
    }
}
