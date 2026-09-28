using Spirit604.Gameplay.UI;
using UnityEngine;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public class MobileMotionInput : IMotionInput
    {
        private readonly InputManager inputManager;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
        private DefaultInputActions inputActions;
#endif

        public MobileMotionInput(InputManager inputManager)
        {
            this.inputManager = inputManager;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
            inputActions = new DefaultInputActions();
            inputActions.Enable();
#endif
        }

        public Vector3 MovementInput
        {
            get
            {
#if UNITY_EDITOR
#if ENABLE_LEGACY_INPUT_MANAGER
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                if (h != 0 || v != 0)
                    return new Vector3(h, 0, v);
#else
                var move = inputActions.Player.Move.ReadValue<Vector2>();

                if (move.x != 0 || move.y != 0)
                    return new Vector3(move.x, 0, move.y);
#endif
#endif

                return inputManager.GetMove(0);
            }
        }

        public Vector3 FireInput => inputManager.GetMove(1);
    }
}