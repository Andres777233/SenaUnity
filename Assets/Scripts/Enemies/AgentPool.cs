using UnityEngine;

namespace Popayork.Enemies
{
    public class AgentPool : MonoBehaviour
    {
        public static AgentPool Instance { get; private set; }

        private const int MaxSlots = 40;

        private AgentBrain[] slots = new AgentBrain[MaxSlots];
        private AgentBrain[] activeList = new AgentBrain[MaxSlots];
        private int slotCount;
        private int activeCount;

        public int ActiveCount
        {
            get { return activeCount; }
        }

        public int SlotCount
        {
            get { return slotCount; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Discover();
        }

        // Registra los cerebros de la escena (el registro en memoria no se guarda
        // en el .unity, así que se redescubre al cargar). Solo asignaciones de init.
        public void Discover()
        {
            Clear();
            AgentBrain[] found = FindObjectsByType<AgentBrain>(FindObjectsInactive.Include);
            for (int i = 0; i < found.Length && slotCount < MaxSlots; i++)
            {
                found[i].AttachPool(this);
                Register(found[i]);
            }
        }

        public void Register(AgentBrain brain)
        {
            if (brain == null || slotCount >= MaxSlots)
            {
                return;
            }
            slots[slotCount] = brain;
            slotCount++;
            brain.Deactivate();
        }

        public AgentBrain GetActive(int index)
        {
            return activeList[index];
        }

        // Activa un slot libre de la facción pedida (y variante si se indica). Sin asignaciones.
        public AgentBrain Spawn(Faction faction, Vector3 position, Vector3 objective)
        {
            return SpawnFiltered(faction, string.Empty, position, objective);
        }

        public AgentBrain SpawnFiltered(Faction faction, string variant, Vector3 position, Vector3 objective)
        {
            for (int i = 0; i < slotCount; i++)
            {
                AgentBrain brain = slots[i];
                if (brain != null && !brain.IsActive && brain.Faction == faction
                    && (string.IsNullOrEmpty(variant) || brain.VariantName == variant))
                {
                    brain.Activate(position, objective);
                    activeList[activeCount] = brain;
                    activeCount++;
                    return brain;
                }
            }
            return null;
        }

        public void Return(AgentBrain brain)
        {
            if (brain == null)
            {
                return;
            }
            for (int i = 0; i < activeCount; i++)
            {
                if (activeList[i] == brain)
                {
                    activeCount--;
                    activeList[i] = activeList[activeCount];
                    activeList[activeCount] = null;
                    break;
                }
            }
            brain.Deactivate();
        }

        public void ReclaimAll()
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (slots[i] != null && slots[i].IsActive)
                {
                    slots[i].Deactivate();
                }
            }
            activeCount = 0;
        }

        // Limpia el registro (reconstrucción de escena o reinicio de misión).
        public void Clear()
        {
            activeCount = 0;
            slotCount = 0;
        }

        public int CountActive(Faction faction)
        {
            int n = 0;
            for (int i = 0; i < activeCount; i++)
            {
                if (activeList[i] != null && activeList[i].Faction == faction)
                {
                    n++;
                }
            }
            return n;
        }
    }
}
