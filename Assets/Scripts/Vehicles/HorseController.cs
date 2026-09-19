using UnityEngine;
using Popayork.Player;

namespace Popayork.Vehicles
{
    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour
    {
        public static readonly KeyCode MountKey = KeyCode.E;

        [SerializeField] private HorseData data;

        private CharacterController controller;
        private GameObject rider;
        private PlayerController riderMovement;
        private Vector3[] routePoints = new Vector3[0];
        private float verticalSpeed;
        private float bobPhase;
        private float promptCooldown;
        private bool mounted;
        private bool galloping;
        private GameObject cachedPlayer;

        public bool IsMounted
        {
            get { return mounted; }
        }

        public bool IsGalloping
        {
            get { return galloping; }
        }

        public Vector3 WaitPosition
        {
            get { return transform.position; }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Setup(HorseData horseData, Vector3[] route)
        {
            data = horseData;
            routePoints = route != null ? route : new Vector3[0];
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            if (data == null)
            {
                return;
            }
            if (mounted)
            {
                float moveZ = Input.GetAxisRaw("Vertical");
                bool gallop = Input.GetKey(KeyCode.LeftShift) && moveZ > 0.1f;
                float steer = Input.GetAxisRaw("Horizontal");
                if (Input.GetKeyDown(MountKey))
                {
                    Dismount();
                    return;
                }
                Simulate(Time.deltaTime, moveZ, steer, gallop);
            }
            else
            {
                promptCooldown = Mathf.Max(0f, promptCooldown - Time.deltaTime);
                GameObject player = FindRiderCandidate();
                if (player != null && Input.GetKeyDown(MountKey))
                {
                    Mount(player);
                }
            }
        }

        private GameObject FindRiderCandidate()
        {
            if (cachedPlayer == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    cachedPlayer = player.gameObject;
                }
            }
            if (cachedPlayer == null || data == null)
            {
                return null;
            }
            Vector3 diff = cachedPlayer.transform.position - transform.position;
            diff.y = 0f;
            if (diff.sqrMagnitude <= data.mountRange * data.mountRange)
            {
                return cachedPlayer;
            }
            return null;
        }

        public bool IsRiderNear()
        {
            return !mounted && FindRiderCandidate() != null;
        }

        public void Mount(GameObject player)
        {
            if (mounted || player == null || data == null)
            {
                return;
            }
            rider = player;
            riderMovement = rider.GetComponent<PlayerController>();
            if (riderMovement != null)
            {
                riderMovement.enabled = false;
            }
            mounted = true;
            galloping = false;
            SnapRider();
        }

        public void Dismount()
        {
            if (!mounted)
            {
                return;
            }
            mounted = false;
            galloping = false;
            if (rider != null)
            {
                Vector3 side = transform.right * 1.6f;
                side.y = 0f;
                rider.transform.position = transform.position + side + Vector3.up * 0.2f;
                if (riderMovement != null)
                {
                    riderMovement.enabled = true;
                }
                rider = null;
                riderMovement = null;
            }
        }

        // Paso de caballo sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt, float moveZ, float steer, bool gallop)
        {
            if (data == null)
            {
                return;
            }
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }
            float yaw = transform.eulerAngles.y + steer * data.turnSpeed * dt;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            galloping = gallop && moveZ > 0.1f;
            float speed = galloping ? data.gallopSpeed : data.trotSpeed;
            Vector3 motion = transform.forward * moveZ * speed;
            if (controller.isGrounded)
            {
                verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed -= data.gravity * dt;
            }
            motion.y = verticalSpeed;
            controller.Move(motion * dt);
            ClampToCorridor();
            if (moveZ > 0.1f)
            {
                bobPhase += dt * (galloping ? 11f : 7f);
            }
            if (mounted)
            {
                SnapRider();
            }
        }

        private void SnapRider()
        {
            if (rider == null || data == null)
            {
                return;
            }
            Vector3 saddle = transform.position + Vector3.up * data.saddleHeight + transform.forward * data.saddleForward;
            saddle.y += Mathf.Sin(bobPhase) * 0.05f;
            rider.transform.position = saddle;
            rider.transform.rotation = transform.rotation;
        }

        private void ClampToCorridor()
        {
            if (data == null || routePoints == null || routePoints.Length < 2)
            {
                return;
            }
            Vector3 p = transform.position;
            float bestDistSq = float.MaxValue;
            Vector3 best = p;
            for (int i = 0; i < routePoints.Length - 1; i++)
            {
                Vector3 closest = ClosestOnSegmentXZ(p, routePoints[i], routePoints[i + 1]);
                Vector3 flat = p - closest;
                flat.y = 0f;
                float d = flat.sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = closest;
                }
            }
            float half = data.corridorHalfWidth;
            if (bestDistSq > half * half)
            {
                Vector3 dir = p - best;
                dir.y = 0f;
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    Vector3 clamped = best + dir / len * half;
                    transform.position = new Vector3(clamped.x, p.y, clamped.z);
                }
            }
        }

        private static Vector3 ClosestOnSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            Vector3 flat = p;
            flat.y = 0f;
            Vector3 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom < 0.000001f)
            {
                return a;
            }
            float t = Mathf.Clamp01(Vector3.Dot(flat - a, ab) / denom);
            return a + ab * t;
        }
    }
}
