using UnityEngine;
using UnityEngine.InputSystem;

namespace TK_Shared._3DPlayerMovement
{
    [RequireComponent(typeof(PlayerActionsController))]
    public class PlayerInputController: MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerActionsController playerActionsController;
        [SerializeField] PauseMenu pauseMenu;

        void OnMove(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            playerActionsController.moveInput = value.Get<Vector2>();
        }

        void OnLook(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            playerActionsController.lookInput = value.Get<Vector2>();
        }

        void OnSprint(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            playerActionsController.sprintInput = value.isPressed;
        }

        void OnJump(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            if(value.isPressed)
                playerActionsController.TryJump();
        }

        void OnCrouch(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            playerActionsController.crouchInput = value.isPressed;
        }
        void OnInteract(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            if (value.isPressed)
                playerActionsController.PickUp();
        }
        void OnLeanRight(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            if (value.isPressed)
                playerActionsController.TryLean(1);

        }
        void OnLeanLeft(InputValue value)
        {
            if (Time.timeScale == 0f) return;
            if (value.isPressed)
                playerActionsController.TryLean(-1);
        }
        void OnEscape(InputValue value)
        {
            if (value.isPressed)
                pauseMenu.TogglePauseMenu();
        }
        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
