using UnityEngine;
using Popayork.Core;

namespace Popayork.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraPivot;

        private CharacterController controller;
        private float yaw;
        private float pitch;
        private float pitchKick;
        private float shakeTrauma;
        private float verticalSpeed;
        private bool sprinting;

        public bool Sprinting
        {
            get { return sprinting; }
        }

        public float Yaw
        {
            get { return yaw; }
        }

        private void Awake()
        {
            EnsureParts();
        }

        private void EnsureParts()
        {
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
            if (cameraPivot == null && playerCamera != null)
            {
                cameraPivot = playerCamera.transform;
            }
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }
            float sensitivity = GameManager.Instance != null ? GameManager.Instance.MouseSensitivity : GameConfig.DefaultSensitivity;
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            bool wantSprint = Input.GetKey(KeyCode.LeftShift) && moveZ > 0.1f;
            bool jump = Input.GetButtonDown("Jump");
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            Simulate(Time.deltaTime, moveX, moveZ, wantSprint, jump, mouseX, mouseY, sensitivity);
        }

        // Ruta sin asignaciones: solo aritmética y Move; la usa Update y el Verify.
        public void Simulate(float dt, float moveX, float moveZ, bool wantSprint, bool jump, float mouseX, float mouseY, float sensitivity)
        {
            EnsureParts();
            if (config == null || controller == null || playerCamera == null || cameraPivot == null)
            {
                return;
            }
            yaw += mouseX * sensitivity * config.lookSpeed;
            pitch -= mouseY * sensitivity * config.lookSpeed;
            pitch = Mathf.Clamp(pitch, config.minPitch, config.maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            pitchKick = Mathf.MoveTowards(pitchKick, 0f, config.recoilRecoverSpeed * dt);
            shakeTrauma = Mathf.MoveTowards(shakeTrauma, 0f, config.shakeDecaySpeed * dt);
            float shake = shakeTrauma * shakeTrauma;
            float shakeYaw = Mathf.Sin(Time.time * 91.0f) * 1.5f * shake;
            float shakePitch = Mathf.Cos(Time.time * 83.0f) * 1.5f * shake;
            cameraPivot.rotation = Quaternion.Euler(pitch + pitchKick + shakePitch, yaw + shakeYaw, 0f);

            sprinting = wantSprint;
            float targetFov = sprinting ? config.sprintFov : config.normalFov;
            playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, targetFov, config.fovBlendSpeed * 20.0f * dt);

            float speed = sprinting ? config.sprintSpeed : config.walkSpeed;
            Vector3 wish = transform.forward * moveZ + transform.right * moveX;
            if (wish.sqrMagnitude > 1.0f)
            {
                wish.Normalize();
            }

            if (controller.isGrounded)
            {
                verticalSpeed = -config.groundStickForce;
                if (jump)
                {
                    verticalSpeed = Mathf.Sqrt(2.0f * config.gravity * config.jumpHeight);
                }
            }
            else
            {
                verticalSpeed -= config.gravity * dt;
            }

            Vector3 motion = wish * speed + Vector3.up * verticalSpeed;
            controller.Move(motion * dt);
        }

        public void AddRecoil(float kickDegrees, float shakeAmount)
        {
            pitchKick += kickDegrees;
            shakeTrauma = Mathf.Clamp01(shakeTrauma + shakeAmount);
        }
    }
}
