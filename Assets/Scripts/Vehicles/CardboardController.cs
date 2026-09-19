using UnityEngine;
using Popayork.Player;
using Popayork.World;

namespace Popayork.Vehicles
{
    [RequireComponent(typeof(CharacterController))]
    public class CardboardController : MonoBehaviour
    {
        [SerializeField] private CardboardData data;
        [SerializeField] private Vector3[] routePoints = new Vector3[0];

        private CharacterController controller;
        private GameObject rider;
        private PlayerController riderMovement;
        private Camera riderCamera;
        private float riderBaseFov;
        private Vector3 velocity = Vector3.zero;
        private Vector3[] route = new Vector3[0];
        private GameObject windLeft;
        private GameObject windRight;
        private float hitCooldown;
        private float camPhase;
        private bool riding;
        private TrackObstacle[] obstacles = new TrackObstacle[0];
        private bool riderResolved;

        public bool IsRiding
        {
            get { return riding; }
        }

        public float Speed
        {
            get
            {
                Vector3 flat = velocity;
                flat.y = 0f;
                return flat.magnitude;
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Setup(CardboardData cardboardData, Vector3[] routePoints, GameObject player, GameObject windL, GameObject windR)
        {
            data = cardboardData;
            route = routePoints != null ? routePoints : new Vector3[0];
            rider = player;
            if (rider != null)
            {
                riderMovement = rider.GetComponent<PlayerController>();
                riderCamera = rider.GetComponentInChildren<Camera>();
                if (riderCamera != null)
                {
                    riderBaseFov = riderCamera.fieldOfView;
                }
                if (riderMovement != null)
                {
                    riderMovement.enabled = false;
                }
            }
            windLeft = windL;
            windRight = windR;
            obstacles = FindObjectsByType<TrackObstacle>(FindObjectsInactive.Include);
            riding = rider != null;
            velocity = Vector3.zero;
            SnapRider();
        }

        // Paso de cartón sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt, float steer, bool brake)
        {
            EnsureRider();
            if (data == null || !riding)
            {
                return;
            }
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }
            hitCooldown = Mathf.Max(0f, hitCooldown - dt);
            RaycastHit ground;
            bool grounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, out ground, 3f);
            if (grounded)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down * data.gravity, ground.normal);
                velocity += downhill * dt;
                float yawKick = steer * data.turnRate * (0.4f + 0.6f * Mathf.Clamp01(Speed / data.maxSpeed)) * dt;
                if (yawKick != 0f)
                {
                    Quaternion turn = Quaternion.Euler(0f, yawKick, 0f);
                    Vector3 flat = velocity;
                    flat.y = 0f;
                    flat = turn * flat;
                    velocity = new Vector3(flat.x, velocity.y, flat.z);
                    transform.rotation = turn * transform.rotation;
                }
                float drag = 1f - data.drag * dt - (brake ? data.brakeDrag * dt : 0f);
                if (drag < 0f)
                {
                    drag = 0f;
                }
                velocity *= drag;
            }
            else
            {
                velocity.y -= data.gravity * dt;
            }
            Vector3 flatSpeed = velocity;
            flatSpeed.y = 0f;
            if (flatSpeed.magnitude > data.maxSpeed)
            {
                Vector3 capped = flatSpeed.normalized * data.maxSpeed;
                velocity = new Vector3(capped.x, velocity.y, capped.z);
            }
            CheckObstacles();
            controller.Move(velocity * dt);
            ClampToCorridor();
            SnapRider();
            UpdateFeel(dt);
        }

        private void EnsureRider()
        {
            if (riderResolved)
            {
                return;
            }
            if (route == null || route.Length == 0)
            {
                route = routePoints;
            }
            if (obstacles == null || obstacles.Length == 0)
            {
                obstacles = FindObjectsByType<TrackObstacle>(FindObjectsInactive.Include);
            }
            if (rider == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    rider = player.gameObject;
                    riderMovement = player;
                    riderCamera = rider.GetComponentInChildren<Camera>();
                    if (riderCamera != null)
                    {
                        riderBaseFov = riderCamera.fieldOfView;
                    }
                    if (riderMovement != null)
                    {
                        riderMovement.enabled = false;
                    }
                }
            }
            if (windLeft == null)
            {
                windLeft = GameObject.Find("VientoL");
            }
            if (windRight == null)
            {
                windRight = GameObject.Find("VientoR");
            }
            if (rider != null)
            {
                riderResolved = true;
                riding = true;
                velocity = Vector3.zero;
                SnapRider();
            }
        }

        private void CheckObstacles()
        {
            if (obstacles == null || hitCooldown > 0f)
            {
                return;
            }
            for (int i = 0; i < obstacles.Length; i++)
            {
                TrackObstacle ob = obstacles[i];
                if (ob == null || !ob.gameObject.activeSelf)
                {
                    continue;
                }
                Vector3 diff = ob.transform.position - transform.position;
                diff.y = 0f;
                if (diff.sqrMagnitude > ob.radius * ob.radius)
                {
                    continue;
                }
                if (ob.isRamp)
                {
                    velocity.y = Mathf.Max(velocity.y, ob.rampBoost + Speed * 0.12f);
                    hitCooldown = 0.5f;
                    return;
                }
                velocity *= Mathf.Clamp01(ob.speedKeep);
                transform.rotation *= Quaternion.Euler(0f, ob.spinDegrees, 0f);
                hitCooldown = 1f;
                if (rider != null)
                {
                    var health = rider.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        Vector3 dir = transform.position - ob.transform.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.001f)
                        {
                            dir = transform.forward;
                        }
                        health.TakeDamage(ob.damage, dir.normalized);
                    }
                }
                return;
            }
        }

        private void ClampToCorridor()
        {
            if (data == null || route == null || route.Length < 2)
            {
                return;
            }
            Vector3 p = transform.position;
            float bestDistSq = float.MaxValue;
            Vector3 best = p;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector3 a = route[i];
                a.y = 0f;
                Vector3 b = route[i + 1];
                b.y = 0f;
                Vector3 flat = p;
                flat.y = 0f;
                Vector3 ab = b - a;
                float denom = ab.sqrMagnitude;
                if (denom < 0.000001f)
                {
                    continue;
                }
                float t = Mathf.Clamp01(Vector3.Dot(flat - a, ab) / denom);
                Vector3 c = a + ab * t;
                float d = (flat - c).sqrMagnitude;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = c;
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
                    Vector3 vflat = velocity;
                    vflat.y = 0f;
                    Vector3 inward = best - p;
                    inward.y = 0f;
                    if (Vector3.Dot(vflat, inward) < 0f)
                    {
                        velocity = new Vector3(-vflat.x * 0.3f, velocity.y, -vflat.z * 0.3f);
                    }
                }
            }
        }

        private void SnapRider()
        {
            if (rider == null || data == null)
            {
                return;
            }
            rider.transform.position = transform.position + Vector3.up * data.seatHeight;
            rider.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        private void UpdateFeel(float dt)
        {
            float factor = Mathf.Clamp01(Speed / data.maxSpeed);
            if (riderCamera != null)
            {
                riderCamera.fieldOfView = riderBaseFov + data.maxFovBoost * factor;
                camPhase += dt * (6f + 24f * factor);
                Vector3 euler = riderCamera.transform.localEulerAngles;
                euler.z = Mathf.Sin(camPhase) * 1.2f * factor;
                euler.x += Mathf.Sin(camPhase * 0.63f) * 0.4f * factor;
                riderCamera.transform.localEulerAngles = euler;
            }
            bool windy = factor > 0.45f;
            if (windLeft != null)
            {
                windLeft.SetActive(windy);
            }
            if (windRight != null)
            {
                windRight.SetActive(windy);
            }
        }

        private void Update()
        {
            float steer = Input.GetAxisRaw("Horizontal");
            bool brake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            Simulate(Time.deltaTime, steer, brake);
        }
    }
}
