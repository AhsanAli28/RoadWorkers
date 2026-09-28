using Spirit604.Extensions;
using Spirit604.Gameplay.UI;
using UnityEngine;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public class MobileCarMotionInput : ICarMotionInput
    {
        private readonly InputManager inputManager;

#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
        private DefaultInputActions inputActions;
#endif

        public MobileCarMotionInput(InputManager inputManager)
        {
            this.inputManager = inputManager;

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

            return inputManager.GetMove(0).ToVector2_2DSpace();
        }

        public Vector3 FireInput => inputManager.GetMove(1);

        public bool Brake => inputManager.Brake;
    }
}