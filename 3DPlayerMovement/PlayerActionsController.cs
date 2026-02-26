using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerActionsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CharacterController characterController;
    [SerializeField] CinemachineCamera playerCamera;

    [Header("Movement Parameters")] 
    [SerializeField] float acceleration = 15f;
    [Tooltip("The speed of character")]
    [SerializeField] float walkSpeed = 4f;
    [Tooltip("The speed of character sprinting")]
    [SerializeField] float sprintSpeed = 6f;
    float MaxMoveSpeed => sprintInput? sprintSpeed:walkSpeed;
    [Space(10)] 
    [Tooltip("The height character will jump")]
    [SerializeField] float jumpHeight = 2f;


    bool IsSprinting => sprintInput && _currentSpeed > 0.1f;

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
   

    [Header("Input")] 
    [HideInInspector]
    public Vector2 moveInput;
    [HideInInspector]
    public Vector2 lookInput;
    [HideInInspector]
    public bool sprintInput;

    [Header("Physics Parameters")] 
    [Tooltip("Scale of the gravity applied to the character")]
    [SerializeField] float gravityScale = 3f;

    float _verticalVelocity = 0f;
    float _currentSpeed;
    Vector3 _currentVelocity;
    bool IsGrounded => characterController.isGrounded;
    float CurrentPitch { get => _currentPitch; set => _currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);}

    void Update()
    {
        MoveUpdate();
        LookUpdate();
        CameraUpdate();
    }

    public void TryJump()
    {
        if(!IsGrounded)
            return;
        
        _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y* gravityScale);
    }
    void CameraUpdate()
    {
        var targetFOV = cameraNormalFOV;
        if (IsSprinting)
        {
            var speedRatio = _currentSpeed / sprintSpeed;
            targetFOV = Mathf.Lerp(cameraNormalFOV,cameraSprintFOV,speedRatio);
        }
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV,cameraFOVSmoothing * Time.deltaTime);
    }

    void LookUpdate()
    {
        Vector2 input = new Vector2(lookInput.x * lookSensitivity.x, lookInput.y * lookSensitivity.y);
        CurrentPitch -= input.y;
        
        playerCamera.transform.localRotation = Quaternion.Euler(_currentPitch, 0, 0);
        
        transform.Rotate(Vector3.up*input.x);
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
    }
}
