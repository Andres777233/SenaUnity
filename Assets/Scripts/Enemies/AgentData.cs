using UnityEngine;

namespace Popayork.Enemies
{
    public enum Faction
    {
        Police = 0,
        Sena = 1,
        University = 2
    }

    public enum AgentState
    {
        Idle = 0,
        Advance = 1,
        SeekCover = 2,
        Attack = 3,
        Retreat = 4,
        Fallen = 5
    }

    [CreateAssetMenu(fileName = "AgentData", menuName = "Popayork/AgentData")]
    public class AgentData : ScriptableObject
    {
        [Header("Identidad")]
        public string displayName = "Agente";
        public Faction faction = Faction.Police;
        public bool hostile = true;
        public string modelFbxPath = "Assets/Models3D/Policias/source/Posed People by JJ - Police vol1 with HQ.fbx";
        public string modelRootName = "Police idle1_gameasset";

        [Header("Cuerpo")]
        public float maxHealth = 100.0f;
        public float moveSpeed = 3.5f;
        public float targetHeightMeters = 1.75f;
        public Color ringColor = Color.red;

        [Header("Combate")]
        public float attackRange = 2.4f;
        public float attackDamage = 10.0f;
        public float attackCooldown = 1.2f;
        public float retreatHealthFraction = 0.25f;
        public float repathInterval = 1.0f;
        public float sightRange = 25.0f;
    }
}
