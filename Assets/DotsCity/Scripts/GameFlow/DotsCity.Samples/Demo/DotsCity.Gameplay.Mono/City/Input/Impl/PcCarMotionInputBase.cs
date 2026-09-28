using Spirit604.Extensions;
using UnityEngine;

#if !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public abstract class PcCarMotionInputBase : ICarMotionInput
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        private const string HorizontalAxisName = "Horizontal";
        private const string VerticalAxisName = "Vertical";
#else

        private DefaultInputActions inputActions;

        protected PcCarMotionInputBase()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
        }
#endif

        public virtual Vector2 GetMovementInput(Vector3 forward)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float vertical = Input.GetAxis(VerticalAxisName);
            float horizontal = Input.GetAxis(HorizontalAxisName);

            return new Vector2(horizontal, vertical);
#else
            var move = inputActions.Player.Move.ReadValue<Vector2>();
            return move;
#endif
        }

        public abstract Vector3 FireInput { get; }

        public bool Brake => InputExtension.GetKey(KeyCode.Space);
    }
}