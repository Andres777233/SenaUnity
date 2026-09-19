using UnityEngine;
using UnityEngine.AI;

namespace Popayork.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : MonoBehaviour
    {
        private const int MaxCorners = 16;

        private NavMeshAgent agent;
        private NavMeshPath path;
        private Vector3[] cornerCache = new Vector3[MaxCorners];
        private int cornerCount;
        private int cornerIndex;
        private float moveSpeed = 3.5f;
        private bool hasDestination;

        public bool HasDestination
        {
            get { return hasDestination; }
        }

        private void Awake()
        {
            EnsureParts();
        }

        private void EnsureParts()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
            if (path == null)
            {
                path = new NavMeshPath();
            }
        }

        public void Setup(float speed, float stoppingDistance)
        {
            EnsureParts();
            moveSpeed = Mathf.Max(0.1f, speed);
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.angularSpeed = 360.0f;
                agent.acceleration = 12.0f;
                agent.stoppingDistance = stoppingDistance;
                agent.autoBraking = true;
            }
        }

        public void Place(Vector3 position)
        {
            EnsureParts();
            hasDestination = false;
            cornerCount = 0;
            cornerIndex = 0;
            if (Application.isPlaying && agent != null && agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }

        // Recalcula ruta (llamada ocasional y escalonada, no por cuadro).
        public void RecalculatePath(Vector3 destination)
        {
            EnsureParts();
            hasDestination = true;
            cornerCount = 0;
            cornerIndex = 0;
            if (Application.isPlaying && agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
                return;
            }
            if (path == null)
            {
                return;
            }
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
            {
                cornerCount = Mathf.Min(path.GetCornersNonAlloc(cornerCache), MaxCorners);
                cornerIndex = 0;
            }
        }

        // Avance sin asignaciones: en play delega al agente; en edit recorre esquinas cacheadas.
        public void Step(float dt, Vector3 destination)
        {
            if (!hasDestination)
            {
                return;
            }
            if (Application.isPlaying)
            {
                return;
            }
            if (cornerCount <= 0)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * dt);
                if ((transform.position - destination).sqrMagnitude < 0.04f)
                {
                    hasDestination = false;
                }
                return;
            }
            Vector3 target = cornerIndex < cornerCount ? cornerCache[cornerIndex] : destination;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * dt);
            Vector3 flat = target - transform.position;
            flat.y = 0.0f;
            if (flat.sqrMagnitude > 0.0004f)
            {
                transform.rotation = Quaternion.LookRotation(flat.normalized);
            }
            if ((transform.position - target).sqrMagnitude < 0.04f)
            {
                cornerIndex++;
                if (cornerIndex >= cornerCount)
                {
                    hasDestination = false;
                }
            }
        }

        public void Stop()
        {
            hasDestination = false;
            if (Application.isPlaying && agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }

        public float RemainingDistance(Vector3 destination)
        {
            Vector3 diff = destination - transform.position;
            diff.y = 0.0f;
            return diff.magnitude;
        }
    }
}
