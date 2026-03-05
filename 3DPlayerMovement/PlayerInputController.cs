using UnityEngine;
using UnityEngine.InputSystem;

namespace TK_Shared._3DPlayerMovement
{
    [RequireComponent(typeof(PlayerActionsController))]
    public class PlayerInputController: MonoBehaviour
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
        void OnInteract(InputValue value)
        {
            if (value.isPressed)
                playerActionsController.PickUp();
        }

        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
