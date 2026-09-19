using UnityEngine;

namespace Popayork.Enemies
{
    public class AgentHealth : MonoBehaviour
    {
        [SerializeField] private AgentData data;
        [SerializeField] private AgentBrain brain;
        private float health;
        private bool dead;

        public float Health
        {
            get { return health; }
        }

        public float MaxHealth
        {
            get { return data != null ? data.maxHealth : 100.0f; }
        }

        public bool IsDead
        {
            get { return dead; }
        }

        public void Setup(AgentData agentData, AgentBrain owner)
        {
            data = agentData;
            brain = owner;
            dead = false;
            health = MaxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0.0f)
            {
                return;
            }
            health = Mathf.Max(0.0f, health - amount);
            if (brain != null)
            {
                brain.OnDamaged();
            }
            if (health <= 0.0f)
            {
                dead = true;
                if (brain != null)
                {
                    brain.OnKilled();
                }
            }
        }

        public void Revive()
        {
            dead = false;
            health = MaxHealth;
        }
    }
}
