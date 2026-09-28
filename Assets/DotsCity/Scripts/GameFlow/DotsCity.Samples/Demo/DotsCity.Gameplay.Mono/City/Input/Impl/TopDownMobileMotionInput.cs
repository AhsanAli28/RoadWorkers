using Spirit604.Gameplay.UI;
using UnityEngine;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public class TopDownMobileMotionInput : IMotionInput
    {
        private readonly InputManager inputManager;
        private readonly Camera mainCamera;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
        private DefaultInputActions inputActions;
#endif

        public TopDownMobileMotionInput(InputManager inputManager, Camera camera)
        {
            this.inputManager = inputManager;
            this.mainCamera = camera;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
            inputActions = new DefaultInputActions();
            inputActions.Enable();
#endif
        }

        public Vector3 MovementInput
        {
            get
            {
                var cameraRotation = Quaternion.Euler(0, mainCamera.transform.rotation.eulerAngles.y, 0);

#if UNITY_EDITOR
#if ENABLE_LEGACY_INPUT_MANAGER
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                if (h != 0 || v != 0)
                    return cameraRotation * new Vector3(h, 0, v);
#else
                var move = inputActions.Player.Move.ReadValue<Vector2>();

                if (move.x != 0 || move.y != 0)
                    return cameraRotation * new Vector3(move.x, 0, move.y);
#endif
#endif

                return cameraRotation * inputManager.GetMove(0);
            }
        }

        public Vector3 FireInput => inputManager.GetMove(1);
    }
}