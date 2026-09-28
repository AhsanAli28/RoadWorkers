using Spirit604.Extensions;
using UnityEngine;

#if !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.Gameplay.InputService
{
    public class PcMotionInput : IMotionInput
    {
        protected readonly Camera mainCamera;

#if !ENABLE_LEGACY_INPUT_MANAGER
        private DefaultInputActions inputActions;
#endif

        public PcMotionInput(Camera camera)
        {
            this.mainCamera = camera;

#if !ENABLE_LEGACY_INPUT_MANAGER
            inputActions = new DefaultInputActions();
            inputActions.Enable();
#endif
        }

        public virtual Vector3 MovementInput
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                var h = Input.GetAxis("Horizontal");
                var v = Input.GetAxis("Vertical");

                return new Vector3(h, 0, v);
#else
                var move = inputActions.Player.Move.ReadValue<Vector2>();
                return new Vector3(move.x, 0, move.y);
#endif
            }
        }

        public virtual Vector3 FireInput
        {
            get
            {
                if (InputExtension.GetMouseButton(0))
                {
                    var ray = mainCamera.ScreenPointToRay(InputExtension.GetMousePosition());

                    Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance = 0;

                    Vector3 worldPosition = Vector3.zero;

                    if (hPlane.Raycast(ray, out distance))
                    {
                        worldPosition = ray.GetPoint(distance).Flat();
                    }

                    return worldPosition;
                }

                return default;
            }
        }
    }
}