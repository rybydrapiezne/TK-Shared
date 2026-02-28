using UnityEngine;

namespace TK_Shared._3DPlayerMovement
{
    [RequireComponent(typeof(CharacterController))]
    public class HeadBobbing : MonoBehaviour
    {
        [SerializeField] bool enable = true;
        [HideInInspector] public float amplitude = 0.002f;
        [HideInInspector] public float frequency = 10.0f;

        [SerializeField] Transform cameraT;
        [SerializeField] Transform cameraHolder;

        const float ToggleSpeed = 3.0f;
        Vector3 _startPosition;
        CharacterController _controller;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _startPosition = cameraT.localPosition;
        }

        Vector3 MoveMotion()
        {
            Vector3 pos=Vector3.zero;
            pos.y += Mathf.Sin(Time.time*frequency) * amplitude;
            pos.x += Mathf.Cos(Time.time*frequency/2) * amplitude*2;
            return pos;
        }

        void CheckMovement()
        {
            float speed = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
            if (speed < ToggleSpeed) return;
            if (!_controller.isGrounded) return;
            PlayMotion(MoveMotion());
        }

        void ResetPosition()
        {
            if (cameraT.localPosition == _startPosition) return;
            cameraT.localPosition = Vector3.Lerp(cameraT.localPosition, _startPosition, 1*Time.deltaTime);

        }

        void Update()
        {
            if(!enable) return;
            CheckMovement();
            ResetPosition();
            cameraT.LookAt(FocusTarget());
        }

        Vector3 FocusTarget()
        {
            Vector3 pos = new Vector3(transform.position.x, transform.position.y+cameraHolder.localPosition.y, transform.position.z);
            pos += cameraHolder.forward * 15f;
            return pos;
        }
        void PlayMotion(Vector3 motion){
            cameraT.localPosition += motion; 
        }
    }
}
