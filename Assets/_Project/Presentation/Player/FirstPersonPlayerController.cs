using HorseParking.Application.Interaction;
using HorseParking.Core.Interaction;
using HorseParking.Presentation.Composition;
using UnityEngine;

namespace HorseParking.Presentation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintMultiplier = 1.7f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float interactionDistance = 3f;

        private CharacterController characterController = null!;
        [SerializeField] private Camera playerCamera = null!;
        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        private InteractWithTargetUseCase interactUseCase = null!;
        private float verticalVelocity;
        private float cameraPitch;

        public void Configure(Camera cameraComponent, GameCompositionRoot compositionRoot)
        {
            playerCamera = cameraComponent;
            this.compositionRoot = compositionRoot;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            if (playerCamera == null || compositionRoot == null)
            {
                Debug.LogError("First-person player is missing its camera or composition root.", this);
                enabled = false;
                return;
            }

            // The composition root completes Awake before Start. This preserves DI and
            // prevents the runtime-only reference from being lost when Unity reloads a scene.
            interactUseCase = compositionRoot.InteractWithTargetUseCase;
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                return;
            }

            HandleLook();
            HandleMovement();
            HandleInteraction();
            HandleCursorUnlock();
        }

        private void HandleLook()
        {
            var yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            var pitch = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, yaw);
            cameraPitch = Mathf.Clamp(cameraPitch - pitch, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var move = (transform.right * input.x) + (transform.forward * input.y);
            var isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
            move = Vector3.ClampMagnitude(move, 1f) * currentSpeed;

            if (characterController.isGrounded)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
                else if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }

        private void HandleInteraction()
        {
            if (!Input.GetButtonDown("Fire1") || interactUseCase == null)
            {
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactionDistance))
            {
                return;
            }

            var target = hit.collider.GetComponentInParent(typeof(IInteractionTarget)) as IInteractionTarget;
            if (target == null)
            {
                return;
            }

            var result = interactUseCase.Execute(target);
            Debug.Log(result.MessageKey.Value);
        }

        private static void HandleCursorUnlock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
