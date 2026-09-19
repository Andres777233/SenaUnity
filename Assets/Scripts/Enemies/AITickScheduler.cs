using UnityEngine;

namespace Popayork.Enemies
{
    // Actualización de IA escalonada: cada cuadro procesa una fracción de agentes
    // con dt compensado, sin asignaciones (arreglos preasignados, bucles for).
    public class AITickScheduler : MonoBehaviour
    {
        public static AITickScheduler Instance { get; private set; }

        [SerializeField] private int slices = 3;

        private AgentPool pool;
        private int cursor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Bind(AgentPool agentPool)
        {
            pool = agentPool;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (pool == null)
            {
                pool = AgentPool.Instance;
            }
            if (pool == null || pool.ActiveCount == 0)
            {
                return;
            }
            int total = pool.ActiveCount;
            int step = slices > 0 ? slices : 1;
            float scaledDt = dt * step;
            for (int i = 0; i < total; i++)
            {
                int index = (cursor + i) % total;
                if (index % step != cursor % step)
                {
                    continue;
                }
                if (index >= pool.ActiveCount)
                {
                    continue;
                }
                AgentBrain brain = pool.GetActive(index);
                if (brain != null)
                {
                    brain.Simulate(scaledDt);
                }
            }
            cursor = (cursor + 1) % step;
        }
    }
}
