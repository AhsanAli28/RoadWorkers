using Spirit604.Gameplay.UI;
using UnityEngine;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public class TopDownMobileCarMotionInput : ICarMotionInput
    {
        private const float MAX_ROTATE_ANGLE = 90f;
        private const float MAX_JOYSTICK_FORWARD_ANGLE = 100f;

        private readonly InputManager inputManager;
        private readonly Camera mainCamera;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
        private DefaultInputActions inputActions;
#endif

        public TopDownMobileCarMotionInput(InputManager inputManager, Camera camera)
        {
            this.inputManager = inputManager;
            this.mainCamera = camera;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
            inputActions = new DefaultInputActions();
            inputActions.Enable();
#endif
        }

        public Vector2 GetMovementInput(Vector3 forward)
        {
#if UNITY_EDITOR
#if ENABLE_LEGACY_INPUT_MANAGER
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            if (h != 0 || v != 0)
                return new Vector2(h, v);
#else
            var move = inputActions.Player.Move.ReadValue<Vector2>();

            if (move.x != 0 || move.y != 0)
                return move;
#endif
#endif

            var cameraRotation = Quaternion.Euler(0, mainCamera.transform.rotation.eulerAngles.y, 0);

            var moveVector = cameraRotation * inputManager.GetMove(0);

            float signedAngle = Vector3.SignedAngle(forward, moveVector, Vector3.up);

            int side = signedAngle > 0 ? 1 : -1;

            var InputDirection = Mathf.Abs(signedAngle) < MAX_JOYSTICK_FORWARD_ANGLE ? 1 : -1;

            if (InputDirection == -1)
            {
                signedAngle = Vector3.SignedAngle(-forward, moveVector, Vector3.up) * -1;
            }

            var vertical = moveVector.magnitude * InputDirection;

            signedAngle = Mathf.Clamp(signedAngle, -MAX_ROTATE_ANGLE, MAX_ROTATE_ANGLE);

            float rotateForce = 0;

            rotateForce = signedAngle / MAX_ROTATE_ANGLE;

            var horizontal = rotateForce;

            return new Vector2(horizontal, vertical);
        }

        public Vector3 FireInput => inputManager.GetMove(1);

        public bool Brake => false;
    }
}
