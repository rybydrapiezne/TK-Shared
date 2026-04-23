using System;
using EnemySystem;
using OptionsSystem;
using PlayerShootingSystem;
using TK_Shared.ObjectInteractions3D;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TK_Shared._3DPlayerMovement
{
    [RequireComponent(typeof(CharacterController), typeof(HeadBobbing))]
    public class PlayerActionsController : MonoBehaviour
    {
        public static float Speed { get; private set; }
        [HideInInspector] public Transform pickedUpObject = null;
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public Vector2 lookInput;
        [HideInInspector] public bool sprintInput;
        [HideInInspector] public bool crouchInput;
        public static Action<Transform> OnPickedUp;

        [Header("References")] [SerializeField]
        CharacterController characterController;

        [SerializeField] CinemachineCamera playerCamera;
        [SerializeField] HeadBobbing headBobbing;
        [SerializeField] Transform HoldPivot;

        [Header("Movement Parameters")] [SerializeField]
        float acceleration = 15f;

        [Tooltip("The speed of character")] [SerializeField]
        float walkSpeed = 4f;

        [Tooltip("The speed of character sprinting")] [SerializeField]
        float sprintSpeed = 6f;

        [Tooltip("The speed of character when crouched")] [SerializeField]
        float crouchedSpeed = 2f;

        [Tooltip("Base frequency of head bobbing movement")] [SerializeField, Range(0, 30)]
        float baseHeadBobbingFreq = 10f;

        [Tooltip("Base amplitude of head bobbing movement")] [SerializeField, Range(0, 0.01f)]
        float baseHeadBobbingAmpl = 0.002f;

        [Header("Audio settings")] [SerializeField]
        AudioSource walkingAudioSource;

        [Space(10)] [SerializeField] float audioEmitRadius = 10f;
        [SerializeField] float crouchVolume = 0.1f;
        [SerializeField] float walkVolume = 1f;
        [SerializeField] float sprintVolume = 1.5f;

        [SerializeField] float jumpVolume = 2.5f;
        [SerializeField] float groundHitVolume = 3f;

        [Header("Lean Parameters")] [SerializeField]
        float leanAngle = 15f;

        [SerializeField] float leanHorizontalOffset = 0.3f;
        [SerializeField] float leanSpeed = 8f;

        [Header("Slide Settings")] [Tooltip("How fast the player slides off the enemy.")] [SerializeField]
        float slideSpeed = 8f;

        [SerializeField] string enemyLayerName = "Enemy";
        int enemyLayerIndex;
        Vector3 slideVelocity;

        float MaxMoveSpeed
        {
            get
            {
                if (isCrouching)
                {
                    return crouchedSpeed;
                }

                if (sprintInput)
                {
                    return sprintSpeed;
                }

                return walkSpeed;
            }
        }

        [Space(10)] [Tooltip("The height of player when crouched")] [SerializeField]
        float crouchHeight = 1.5f;

        float normalHeight = 2f; // Updated to characterController.height on awake

        [Tooltip("The layers to check for to avoid uncrouching into ceiling")] [SerializeField]
        LayerMask uncrouchCeilingLayer;

        [Space(10)] [Tooltip("The height character will jump")] [SerializeField]
        float jumpHeight = 2f;


        bool IsSprinting => sprintInput && _currentSpeed > 0.1f;
        bool isCrouching = false;

        [Header("Camera Parameters")] [Tooltip("Sensitivity of mouse applied to camera movement")] [SerializeField]
        Vector2 lookSensitivity = new Vector2(0.1f, 0.1f);

        [Tooltip("Normal camera FOV when not sprinting")] [SerializeField]
        float cameraNormalFOV = 60f;

        [Tooltip("Max camera FOV while sprinting")] [SerializeField]
        float cameraSprintFOV = 90f;

        [Tooltip("Smoothing of the transition between normal FOV and sprinting FOV")] [SerializeField]
        float cameraFOVSmoothing = 1f;

        [Tooltip("Max angle of camera up and down movement")] [SerializeField]
        float pitchLimit = 85f;

        float _currentPitch = 0f;
        float originalCameraPosY;
        [SerializeField] Transform CameraTarget;
        [HideInInspector] public float eyeLevel => CameraTarget.transform.localPosition.y;

        [Header("Interaction Parameters")] [SerializeField]
        float pickupRange = 2f;

        [SerializeField] LayerMask pickupLayer;
        [SerializeField] float maxHealth = 100;


        [Header("Physics Parameters")] [Tooltip("Scale of the gravity applied to the character")] [SerializeField]
        float gravityScale = 3f;

        Transform _cameraTransform;
        GrabbableObject _grabbedObject;
        bool _previousCrouchInputState = false;
        float _verticalVelocity = 0f;
        float health;
        float _currentSpeed;
        Vector3 _currentVelocity;
        Vector3 _centerOrigin;
        bool IsGrounded => characterController.isGrounded;

        bool IsCeilingAboveHead => Physics.CheckSphere(transform.position + _centerOrigin + new Vector3(0, 0.5f, 0),
            characterController.radius, uncrouchCeilingLayer);

        float CurrentPitch
        {
            get => _currentPitch;
            set => _currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
        }

        float _targetLeanAngle = 0f;
        float _currentLeanAngle = 0f;
        int _lastLeanDirection = 0;

        void Awake()
        {
            normalHeight = characterController.height;
            originalCameraPosY = CameraTarget.localPosition.y;
            _centerOrigin = characterController.center;
            _cameraTransform = playerCamera.transform;
            health = maxHealth;
            enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            MoveUpdate();
            LookUpdate();
            CameraUpdate();
            AudioUpdate();
        }

        public void TryLean(int direction)
        {
            // TODO: implement hold option, reading Options.IsLeanHold
            if (direction == _lastLeanDirection)
                direction = 0;
            _lastLeanDirection = direction;
            _targetLeanAngle = direction * leanAngle;
        }

        public void TryJump()
        {
            if (!IsGrounded)
                return;

            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y * gravityScale);
        }

        public void PickUp()
        {
            if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, pickupRange,
                    pickupLayer))
            {
                if (hit.transform.TryGetComponent(out GrabbableObject grabbable))
                {
                    pickedUpObject = grabbable.transform;
                    _grabbedObject = grabbable;
                    grabbable.Grab(HoldPivot);
                    OnPickedUp?.Invoke(pickedUpObject);
                }
            }
        }

        void CameraUpdate()
        {
            var targetFOV = cameraNormalFOV;

            float targetCameraY = isCrouching ? originalCameraPosY - normalHeight + crouchHeight : originalCameraPosY;

            Vector3 newCameraPos = CameraTarget.localPosition;
            newCameraPos.y = Mathf.Lerp(CameraTarget.localPosition.y, targetCameraY, 6 * Time.deltaTime);
            CameraTarget.localPosition = newCameraPos;

            if (Options.IsCrouchHold)
            {
                if (crouchInput)
                {
                    isCrouching = true;
                }
                else if (!IsCeilingAboveHead)
                {
                    isCrouching = false;
                }
            }
            else
            {
                if (crouchInput && !_previousCrouchInputState)
                {
                    isCrouching = !isCrouching;

                    if (!isCrouching && IsCeilingAboveHead)
                    {
                        isCrouching = true;
                    }
                }
            }

            if (isCrouching)
            {
                characterController.height = crouchHeight;
                characterController.center = _centerOrigin + new Vector3(0, -(normalHeight - crouchHeight) / 2f, 0);
            }
            else if (!IsCeilingAboveHead)
            {
                characterController.height = normalHeight;
                characterController.center = _centerOrigin;
            }

            _previousCrouchInputState = crouchInput;
            if (IsSprinting && !isCrouching)
            {
                var speedRatio = _currentSpeed / sprintSpeed;
                headBobbing.frequency = baseHeadBobbingFreq * 2;
                headBobbing.amplitude = baseHeadBobbingAmpl * 2;
                targetFOV = Mathf.Lerp(cameraNormalFOV, cameraSprintFOV, speedRatio);
            }
            else if (!IsSprinting)
            {
                headBobbing.frequency = baseHeadBobbingFreq;
                headBobbing.amplitude = baseHeadBobbingAmpl;
            }

            playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV,
                cameraFOVSmoothing * Time.deltaTime);
        }

        void LookUpdate()
        {
            Vector2 input = new Vector2(lookInput.x * lookSensitivity.x, lookInput.y * lookSensitivity.y);
            CurrentPitch -= input.y;

            playerCamera.transform.localRotation = Quaternion.Euler(_currentPitch, 0, 0);

            _currentLeanAngle = Mathf.Lerp(_currentLeanAngle, _targetLeanAngle, leanSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, transform.localEulerAngles.y, -_currentLeanAngle);

            transform.Rotate(Vector3.up * input.x);
        }

        void MoveUpdate()
        {
            Vector3 motion = transform.forward * moveInput.y + transform.right * moveInput.x;
            motion.y = 0f;
            motion.Normalize();
            _currentVelocity = motion.sqrMagnitude >= 0.01f
                ? Vector3.MoveTowards(_currentVelocity, motion *
                                                        MaxMoveSpeed, acceleration * Time.deltaTime)
                : Vector3.MoveTowards(_currentVelocity,
                    Vector3.zero, acceleration * Time.deltaTime);

            if (IsGrounded && _verticalVelocity <= 0.01f)
            {
                _verticalVelocity = -3f;
            }
            else
            {
                _verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
            }

            Vector3 fullVelocity =
                new Vector3(_currentVelocity.x, _verticalVelocity, _currentVelocity.z) + slideVelocity;

            characterController.Move(fullVelocity * Time.deltaTime);
            slideVelocity = Vector3.Lerp(slideVelocity, Vector3.zero, Time.deltaTime * 10f);
            _currentSpeed = _currentVelocity.magnitude;
            Speed = _currentSpeed;

            UpdateWalkingAudioPitch(0.8f + _currentSpeed / sprintSpeed / 2f);
            if (_currentSpeed > 0.01f && IsGrounded)
            {
                SetWalkingAudioPlaying(true);
            }
            else
            {
                SetWalkingAudioPlaying(false);
            }
        }

        void AudioUpdate()
        {
            float movementVolumeMod = GetMovementVolume();
            if (movementVolumeMod < 0.01f) return;
            Collider[] colliders = Physics.OverlapSphere(transform.position, audioEmitRadius);
            foreach (Collider col in colliders)
            {
                if (col.gameObject.layer == enemyLayerIndex && col.gameObject.GetComponent<AICore>() is { } aic)
                {
                    aic.HearingUpdate(movementVolumeMod * Time.deltaTime,
                        Vector3.Distance(transform.position, col.gameObject.transform.position), audioEmitRadius * movementVolumeMod);
                }
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.layer == enemyLayerIndex)
            {
                // Notify enemy
                if (hit.gameObject.GetComponent<EnemyHitbox>()?.GetEnemyHealth() is { } enemyHealth)
                {
                    enemyHealth.Damage(0);
                }

                // If y > 0.5, the surface is pointing mostly upwards, meaning we are on top of it.
                if (hit.normal.y > 0.5f)
                {
                    // Calculate a push direction based on the slope of the enemy's head.
                    // We ignore the Y axis so we strictly push the player horizontally.
                    Vector3 pushDirection = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;

                    // Edge Case: If the player lands perfectly dead-center on top, the horizontal normal is zero.
                    // We assign a random nudge so they don't get stuck balancing permanently.
                    if (pushDirection == Vector3.zero)
                    {
                        pushDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                    }

                    slideVelocity = pushDirection * slideSpeed;
                }
            }
        }

        void UpdateWalkingAudioPitch(float value)
        {
            walkingAudioSource.pitch = Mathf.Lerp(walkingAudioSource.pitch, value, Time.deltaTime);
        }

        void SetWalkingAudioPlaying(bool playing)
        {
            if (playing)
            {
                if (walkingAudioSource.isPlaying) return;
                walkingAudioSource.time = Random.Range(0, walkingAudioSource.clip.length);
                walkingAudioSource.Play();
            }
            else
            {
                if (!walkingAudioSource.isPlaying) return;
                walkingAudioSource.Stop();
            }
        }

        public void OnGamePaused(bool paused)
        {
            if (paused)
            {
                walkingAudioSource.Pause();
                headBobbing.enabled = false;
            }
            else
            {
                walkingAudioSource.UnPause();
                headBobbing.enabled = true;
            }
        }

        float GetMovementVolume()
        {
            if (isCrouching)
            {
                return crouchVolume * _currentSpeed / crouchedSpeed;
            }

            if (sprintInput)
            {
                return sprintVolume * _currentSpeed / sprintSpeed;
            }

            return walkVolume * _currentSpeed / walkSpeed;
        }
    }
}