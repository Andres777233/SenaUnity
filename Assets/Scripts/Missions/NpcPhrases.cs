using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    public enum PhraseKind
    {
        Idle = 0,
        Spawn = 1,
        Attack = 2,
        Death = 3,
        Retreat = 4
    }

    [CreateAssetMenu(fileName = "NpcPhrases", menuName = "Popayork/NpcPhrases")]
    public class NpcPhrases : ScriptableObject
    {
        [Header("Policía")]
        public string[] policeSpawn = new string[] { "¡Quieto ahí, parce!", "¡Se acabó la recocha!" };
        [Header("SENA")]
        public string[] senaSpawn = new string[] { "¡Aguante, parceros!", "¡Por el SENA, oe!" };
        [Header("Universidad")]
        public string[] uniSpawn = new string[] { "¡La nacho presente!", "¡Resistencia, compas!" };

        [Header("Ataque")]
        public string[] policeAttack = new string[] { "¡Tome su comparendo!", "¡Pa' la tanqueta!" };
        public string[] senaAttack = new string[] { "¡Tome, pues!", "¡Uy, qué voltaje!" };
        public string[] uniAttack = new string[] { "¡Fuera, fuera!", "¡Ni un paso atrás!" };

        [Header("Caída")]
        public string[] policeDeath = new string[] { "¡Me dieron, me dieron!", "¡Pidan refuerzos!" };
        public string[] senaDeath = new string[] { "¡Ay, juepucha!", "¡Sigan ustedes, parce!" };
        public string[] uniDeath = new string[] { "¡Me tumbaron, compas!", "¡No me dejen!" };

        [Header("Retirada")]
        public string[] policeRetreat = new string[] { "¡Repliegue, repliegue!", "¡Atrás, atrás!" };
        public string[] senaRetreat = new string[] { "¡Corra, parce, corra!", "¡Nos vemos en el SENA!" };
        public string[] uniRetreat = new string[] { "¡Reagruparse, compas!", "¡Al Morro, al Morro!" };

        [Header("Reposo")]
        public string[] idle = new string[] { "¿Qué hubo, pues?", "Ojo, que esto se calienta." };

        // Sin asignaciones: solo indexa arreglos preexistentes.
        public string Pick(Faction faction, PhraseKind kind)
        {
            string[] list = idle;
            switch (kind)
            {
                case PhraseKind.Spawn:
                    list = faction == Faction.Police ? policeSpawn : (faction == Faction.Sena ? senaSpawn : uniSpawn);
                    break;
                case PhraseKind.Attack:
                    list = faction == Faction.Police ? policeAttack : (faction == Faction.Sena ? senaAttack : uniAttack);
                    break;
                case PhraseKind.Death:
                    list = faction == Faction.Police ? policeDeath : (faction == Faction.Sena ? senaDeath : uniDeath);
                    break;
                case PhraseKind.Retreat:
                    list = faction == Faction.Police ? policeRetreat : (faction == Faction.Sena ? senaRetreat : uniRetreat);
                    break;
                default:
                    list = idle;
                    break;
            }
            if (list == null || list.Length == 0)
            {
                return string.Empty;
            }
            return list[Random.Range(0, list.Length)];
        }
    }
}
