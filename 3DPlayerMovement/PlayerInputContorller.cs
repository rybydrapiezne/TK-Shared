using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerActionsController))]
public class PlayerInputContorller: MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerActionsController playerActionsController;

    void OnMove(InputValue value)
    {
        playerActionsController.moveInput = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        playerActionsController.lookInput = value.Get<Vector2>();
    }

    void OnSprint(InputValue value)
    {
        playerActionsController.sprintInput = value.isPressed;
    }

    void OnJump(InputValue value)
    {
        if(value.isPressed)
            playerActionsController.TryJump();
    }

    void OnCrouch(InputValue value)
    {
        playerActionsController.crouchInput = value.isPressed;
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
