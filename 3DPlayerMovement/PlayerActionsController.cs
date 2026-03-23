using System;
using TK_Shared.ObjectInteractions3D;
using Unity.Cinemachine;
using UnityEngine;

namespace TK_Shared._3DPlayerMovement
{
    [RequireComponent(typeof(CharacterController),typeof(HeadBobbing))]
    public class PlayerActionsController : MonoBehaviour
    {        
        public static float Speed { get; private set; }
        [HideInInspector]
        public Transform pickedUpObject=null;
        [HideInInspector]
        public Vector2 moveInput;
        [HideInInspector]
        public Vector2 lookInput;
        [HideInInspector]
        public bool sprintInput;
        [HideInInspector]
        public bool crouchInput;
        public static Action<Transform> OnPickedUp;
        [Header("References")]
        [SerializeField] CharacterController characterController;
        [SerializeField] CinemachineCamera playerCamera;
        [SerializeField] HeadBobbing headBobbing;
        [SerializeField] Transform HoldPivot;
    
        [Header("Movement Parameters")] 
        [SerializeField] float acceleration = 15f;
        [Tooltip("The speed of character")]
        [SerializeField] float walkSpeed = 4f;
        [Tooltip("The speed of character sprinting")]
        [SerializeField] float sprintSpeed = 6f;
        [Tooltip("The speed of character when crouched")]
        [SerializeField] float crouchedSpeed = 2f;
        [Tooltip("Base frequency of head bobbing movement")]
        [SerializeField, Range(0, 30)] float baseHeadBobbingFreq = 10f;
        [Tooltip("Base amplitude of head bobbing movement")]
        [SerializeField, Range(0, 0.01f)] float baseHeadBobbingAmpl = 0.002f;
        [Tooltip("Crouch toggle")]
        [SerializeField] bool crouchToggle;
        [Header("Lean Parameters")]
        [SerializeField] float leanAngle = 15f;
        [SerializeField] float leanHorizontalOffset = 0.3f;
        [SerializeField] float leanSpeed = 8f;
        float MaxMoveSpeed
        {
            get
            {
                if (isCrouching)
                {
                    return crouchedSpeed;
                }
                else if (sprintInput)
                {
                    return sprintSpeed;
                }
                else
                {
                    return walkSpeed;
                }
            }
        }
        [Space(10)] 
        [Tooltip("The height of player when crouched")]
        [SerializeField] float crouchHeight = 1.5f;
        float normalHeight = 2f; // Updated to characterController.height on awake
        [Tooltip("The layers to check for to avoid uncrouching into ceiling")]
        [SerializeField] LayerMask uncrouchCeilingLayer;
        [Space(10)] 
        [Tooltip("The height character will jump")]
        [SerializeField] float jumpHeight = 2f;
        

        bool IsSprinting => sprintInput && _currentSpeed > 0.1f;
        bool isCrouching = false;

        [Header("Camera Parameters")]
        [Tooltip("Sensitivity of mouse applied to camera movement")]
        [SerializeField] Vector2 lookSensitivity = new Vector2 (0.1f,0.1f);
    
        [Tooltip("Normal camera FOV when not sprinting")]
        [SerializeField] float cameraNormalFOV = 60f;
    
        [Tooltip("Max camera FOV while sprinting")]
        [SerializeField] float cameraSprintFOV = 90f;
    
        [Tooltip("Smoothing of the transition between normal FOV and sprinting FOV")]
        [SerializeField] float cameraFOVSmoothing = 1f;
    
        [Tooltip("Max angle of camera up and down movement")]
        [SerializeField] float pitchLimit = 85f;
    
        float _currentPitch = 0f;
        float originalCameraPosY;
        [SerializeField] Transform CameraTarget;
        [HideInInspector] public float eyeLevel => CameraTarget.transform.localPosition.y;
        [Header("Interaction Parameters")] 
        [SerializeField] float pickupRange = 2f;
        [SerializeField] LayerMask pickupLayer;
        [SerializeField] float maxHealth = 100;
        

        [Header("Physics Parameters")] 
        [Tooltip("Scale of the gravity applied to the character")]
        [SerializeField] float gravityScale = 3f;

        Transform _cameraTransform;
        GrabbableObject _grabbedObject;
        bool _previousCrouchInputState = false;
        float _verticalVelocity = 0f;
        float health;
        float _currentSpeed;
        Vector3 _currentVelocity;
        Vector3 _centerOrigin;
        bool IsGrounded => characterController.isGrounded;
        bool IsCeilingAboveHead => Physics.CheckSphere(transform.position + _centerOrigin + new Vector3(0, 0.5f, 0), characterController.radius, uncrouchCeilingLayer);
        float CurrentPitch { get => _currentPitch; set => _currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);}
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
        }
        void Update()
        {
            MoveUpdate();
            LookUpdate();
            CameraUpdate();
        }
        public void TryLean(int direction)
        {
            if (direction == _lastLeanDirection)
                direction = 0;
            _lastLeanDirection = direction;
            _targetLeanAngle = direction * leanAngle;
        }
        public void TryJump()
        {
            if(!IsGrounded)
                return;
        
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y* gravityScale);
        }
        public void PickUp()
        {
            if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit,pickupRange,pickupLayer))
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

            if (!crouchToggle)
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
                targetFOV = Mathf.Lerp(cameraNormalFOV,cameraSprintFOV,speedRatio);
            }
            else if (!IsSprinting)
            {
                headBobbing.frequency = baseHeadBobbingFreq;
                headBobbing.amplitude = baseHeadBobbingAmpl;
            }
            playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV,cameraFOVSmoothing * Time.deltaTime);
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
            Vector3 motion = transform. forward * moveInput.y + transform.right * moveInput.x;
            motion.y = 0f;
            motion.Normalize();
            _currentVelocity = motion.sqrMagnitude >= 0.01f ? Vector3.MoveTowards(_currentVelocity, motion * 
                MaxMoveSpeed, acceleration*Time.deltaTime) : Vector3.MoveTowards(_currentVelocity, 
                Vector3.zero,acceleration*Time.deltaTime);

            if (IsGrounded && _verticalVelocity <= 0.01f)
            {
                _verticalVelocity = -3f;
            }
            else
            {
                _verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;

            }
            Vector3 fullVelocity = new(_currentVelocity.x, _verticalVelocity, _currentVelocity.z);

            characterController.Move(fullVelocity * Time.deltaTime);
            _currentSpeed = _currentVelocity.magnitude;
            Speed = _currentSpeed;
        }
        

        public void Damage(float damage)
        {
            Debug.Log("GOT HIT PLAYER");

            health = Mathf.Clamp(health - damage, 0,maxHealth);

            if (health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
        }
    }
}
