using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class TopDownPCCarMotionInput : PcCarMotionInputBase
    {
        private readonly Camera mainCamera;

        public TopDownPCCarMotionInput(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
        }

        public override Vector3 FireInput
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