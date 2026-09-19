using UnityEngine;
using Popayork.Missions;
using Popayork.Player;

namespace Popayork.Enemies
{
    [RequireComponent(typeof(AgentHealth))]
    [RequireComponent(typeof(AgentMovement))]
    public class AgentBrain : MonoBehaviour
    {
        [SerializeField] private AgentData data;
        [SerializeField] private AgentPool homePool;
        [SerializeField] private NpcPhrases phrases;
        [SerializeField] private Vector3[] coverPoints;
        [SerializeField] private Vector3 retreatPoint;
        [SerializeField] private int slotIndex;
        private AgentHealth health;
        private AgentMovement movement;

        private AgentState state;
        private Transform targetTransform;
        [SerializeField] private PlayerHealth playerTarget;
        private AgentHealth agentTarget;
        [SerializeField] private Vector3 objectivePoint;
        private int coverIndex = -1;

        private float attackTimer;
        private float repathTimer;
        private float scanTimer;
        private float chatterTimer;
        private float stateTimer;
        private float damagedTimer;
        private float bobPhase;
        private Transform view;
        private float viewBaseY;
        private Quaternion upRotation;
        private Quaternion fallRotation;
        private bool fallRotationSet;
        private bool active;
        private bool mute;

        public AgentState State
        {
            get { return state; }
        }

        public bool IsActive
        {
            get { return active; }
        }

        public Faction Faction
        {
            get { return data != null ? data.faction : Faction.Police; }
        }

        public bool IsHostile
        {
            get { return data != null && data.hostile; }
        }

        public int Slot
        {
            get { return slotIndex; }
        }

        public string VariantName
        {
            get { return data != null ? data.displayName : string.Empty; }
        }

        public AgentHealth Health
        {
            get { return health; }
        }

        private void Awake()
        {
            health = GetComponent<AgentHealth>();
            movement = GetComponent<AgentMovement>();
        }

        public void Setup(AgentData agentData, AgentPool pool, int slot, NpcPhrases phraseBook, Vector3[] covers, Vector3 retreat)
        {
            if (health == null)
            {
                health = GetComponent<AgentHealth>();
            }
            if (movement == null)
            {
                movement = GetComponent<AgentMovement>();
            }
            data = agentData;
            homePool = pool;
            slotIndex = slot;
            phrases = phraseBook;
            coverPoints = covers;
            retreatPoint = retreat;
            health.Setup(data, this);
            movement.Setup(data.moveSpeed, data.attackRange * 0.8f);
            upRotation = transform.rotation;
            fallRotationSet = false;
            view = transform.Find("View");
            if (view != null)
            {
                viewBaseY = view.localPosition.y;
            }
            playerTarget = FindAnyObjectByType<PlayerHealth>();
            attackTimer = 0.0f;
            repathTimer = 0.0f;
            scanTimer = Random.Range(0.0f, 0.5f);
            chatterTimer = Random.Range(2.0f, 5.0f);
            damagedTimer = 0.0f;
            objectivePoint = transform.position;
        }

        public void AttachPool(AgentPool pool)
        {
            homePool = pool;
            if (playerTarget == null)
            {
                playerTarget = FindAnyObjectByType<PlayerHealth>();
            }
        }

        // Activa al agente solo si hay punto NavMesh válido. Pre-coloca el
        // transform antes del SetActive para evitar el error "Failed to create
        // agent because it is not close enough to the NavMesh". Si no hay
        // punto válido, no aparece y deja un warning con el nombre del punto.
        public bool Activate(Vector3 position, Vector3 objective, string spotName = "?")
        {
            if (health == null)
            {
                health = GetComponent<AgentHealth>();
            }
            if (movement == null)
            {
                movement = GetComponent<AgentMovement>();
            }
            if (movement != null && !movement.Place(position))
            {
                Debug.LogWarning("Popayork: sin punto NavMesh válido en '" + spotName + "'; agente " + Faction + " no activado.");
                return false;
            }
            objectivePoint = objective;
            state = AgentState.Advance;
            active = true;
            attackTimer = 0.3f;
            repathTimer = 0.0f;
            coverIndex = -1;
            upRotation = transform.rotation;
            fallRotationSet = false;
            view = transform.Find("View");
            if (view != null)
            {
                viewBaseY = view.localPosition.y;
            }
            gameObject.SetActive(true);
            movement.Place(position);
            movement.RecalculatePath(objectivePoint);
            Say(PhraseKind.Spawn);
            return true;
        }

        public void SetMute(bool value)
        {
            mute = value;
        }

        public void Deactivate()
        {
            active = false;
            if (movement == null)
            {
                movement = GetComponent<AgentMovement>();
            }
            if (movement != null)
            {
                movement.Stop();
            }
            gameObject.SetActive(false);
        }

        public void OnDamaged()
        {
            damagedTimer = 3.0f;
        }

        public void OnKilled()
        {
            state = AgentState.Fallen;
            stateTimer = 0.0f;
            upRotation = transform.rotation;
            fallRotationSet = false;
            movement.Stop();
            Say(PhraseKind.Death);
        }

        // Un solo paso de IA sin asignaciones: lo usa el scheduler en juego y el Verify en edit.
        public void Simulate(float dt)
        {
            if (!active || data == null)
            {
                return;
            }
            attackTimer -= dt;
            repathTimer -= dt;
            scanTimer -= dt;
            chatterTimer -= dt;
            damagedTimer = Mathf.Max(0.0f, damagedTimer - dt);

            if (state == AgentState.Fallen)
            {
                SimulateFallen(dt);
                return;
            }

            UpdateTarget();
            Vector3 goal = objectivePoint;
            bool targetInRange = false;
            float targetDist = float.MaxValue;
            if (targetTransform != null)
            {
                Vector3 diff = targetTransform.position - transform.position;
                diff.y = 0.0f;
                targetDist = diff.magnitude;
                if (targetDist <= data.sightRange)
                {
                    goal = targetTransform.position;
                    targetInRange = targetDist <= data.attackRange;
                }
            }

            bool lowLife = health.Health <= data.maxHealth * data.retreatHealthFraction;
            if (lowLife && state != AgentState.Retreat)
            {
                state = AgentState.Retreat;
                movement.RecalculatePath(retreatPoint);
                Say(PhraseKind.Retreat);
            }

            switch (state)
            {
                case AgentState.Advance:
                    if (targetInRange)
                    {
                        state = AgentState.Attack;
                        movement.Stop();
                    }
                    else
                    {
                        if (damagedTimer > 0.0f && TryPickCover())
                        {
                            state = AgentState.SeekCover;
                            break;
                        }
                        MoveToward(goal, dt);
                    }
                    break;
                case AgentState.SeekCover:
                    if (targetInRange)
                    {
                        state = AgentState.Attack;
                        movement.Stop();
                    }
                    else if (coverIndex >= 0)
                    {
                        MoveToward(coverPoints[coverIndex], dt);
                        if (!movement.HasDestination)
                        {
                            state = AgentState.Advance;
                        }
                    }
                    else
                    {
                        state = AgentState.Advance;
                    }
                    break;
                case AgentState.Attack:
                    if (!targetInRange)
                    {
                        state = AgentState.Advance;
                        MoveToward(goal, dt);
                    }
                    else
                    {
                        FaceTarget(dt);
                        if (attackTimer <= 0.0f)
                        {
                            Strike();
                            attackTimer = data.attackCooldown;
                        }
                    }
                    break;
                case AgentState.Retreat:
                    MoveToward(retreatPoint, dt);
                    if (movement.RemainingDistance(retreatPoint) < 2.0f)
                    {
                        ReturnToPool();
                    }
                    break;
                default:
                    MoveToward(goal, dt);
                    break;
            }

            if (!mute && chatterTimer <= 0.0f)
            {
                chatterTimer = Random.Range(4.0f, 9.0f);
                Say(state == AgentState.Attack ? PhraseKind.Attack : PhraseKind.Idle);
            }
        }

        private void MoveToward(Vector3 goal, float dt)
        {
            if (repathTimer <= 0.0f)
            {
                repathTimer = data.repathInterval;
                movement.RecalculatePath(goal);
            }
            movement.Step(dt, goal);
            if (view != null)
            {
                bobPhase += dt * 9.0f;
                Vector3 p = view.localPosition;
                p.y = viewBaseY + Mathf.Sin(bobPhase) * 0.03f;
                view.localPosition = p;
            }
        }

        private void FaceTarget(float dt)
        {
            if (targetTransform == null)
            {
                return;
            }
            Vector3 flat = targetTransform.position - transform.position;
            flat.y = 0.0f;
            if (flat.sqrMagnitude < 0.0001f)
            {
                return;
            }
            Quaternion want = Quaternion.LookRotation(flat.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Mathf.Min(1.0f, 8.0f * dt));
        }

        private void Strike()
        {
            if (playerTarget != null && targetTransform == playerTarget.transform)
            {
                Vector3 dir = (transform.position - playerTarget.transform.position).normalized;
                playerTarget.TakeDamage(data.attackDamage, dir);
            }
            else if (agentTarget != null)
            {
                agentTarget.TakeDamage(data.attackDamage);
            }
            Say(PhraseKind.Attack);
        }

        private void UpdateTarget()
        {
            if (scanTimer > 0.0f)
            {
                return;
            }
            scanTimer = 0.5f;
            if (IsHostile)
            {
                if (playerTarget != null && !IsPlayerDead())
                {
                    targetTransform = playerTarget.transform;
                    agentTarget = null;
                }
                else
                {
                    targetTransform = FindAllyTarget();
                }
                return;
            }
            targetTransform = FindEnemyTarget();
        }

        private bool IsPlayerDead()
        {
            return playerTarget.IsDead;
        }

        private Transform FindAllyTarget()
        {
            if (homePool == null)
            {
                return null;
            }
            agentTarget = null;
            Transform best = null;
            float bestDist = float.MaxValue;
            int count = homePool.ActiveCount;
            for (int i = 0; i < count; i++)
            {
                AgentBrain other = homePool.GetActive(i);
                if (other == null || other == this || other.IsHostile || other.Health.IsDead)
                {
                    continue;
                }
                float d = (other.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = other.transform;
                    agentTarget = other.Health;
                }
            }
            return best;
        }

        private Transform FindEnemyTarget()
        {
            if (homePool == null)
            {
                return null;
            }
            agentTarget = null;
            Transform best = null;
            float bestDist = data.sightRange * data.sightRange;
            int count = homePool.ActiveCount;
            for (int i = 0; i < count; i++)
            {
                AgentBrain other = homePool.GetActive(i);
                if (other == null || other == this || !other.IsHostile || other.Health.IsDead)
                {
                    continue;
                }
                float d = (other.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = other.transform;
                    agentTarget = other.Health;
                }
            }
            return best;
        }

        private bool TryPickCover()
        {
            if (coverPoints == null || coverPoints.Length == 0)
            {
                return false;
            }
            int best = -1;
            float bestDist = 12.0f * 12.0f;
            for (int i = 0; i < coverPoints.Length; i++)
            {
                float d = (coverPoints[i] - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            if (best < 0)
            {
                return false;
            }
            coverIndex = best;
            movement.RecalculatePath(coverPoints[best]);
            return true;
        }

        private void SimulateFallen(float dt)
        {
            stateTimer += dt;
            if (!fallRotationSet)
            {
                fallRotationSet = true;
                fallRotation = upRotation * Quaternion.Euler(0.0f, 0.0f, 88.0f);
            }
            float t = Mathf.Min(1.0f, stateTimer / 0.45f);
            transform.rotation = Quaternion.Slerp(upRotation, fallRotation, t);
            if (stateTimer >= 2.0f)
            {
                transform.rotation = upRotation;
                fallRotationSet = false;
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            active = false;
            if (homePool != null)
            {
                homePool.Return(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void Say(PhraseKind kind)
        {
            if (mute || phrases == null)
            {
                return;
            }
            var ui = UI.SubtitleSystem.Instance;
            if (ui == null)
            {
                return;
            }
            string line = phrases.Pick(data.faction, kind);
            if (!string.IsNullOrEmpty(line))
            {
                ui.ShowLine(data.displayName + ": " + line, 2.5f);
            }
        }
    }
}
