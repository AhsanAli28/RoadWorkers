using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class TopDownPCMotionInput : PcMotionInput
    {
        public TopDownPCMotionInput(Camera camera) : base(camera)
        {
        }

        public override Vector3 MovementInput
        {
            get
            {
                var cameraRotation = Quaternion.Euler(0, mainCamera.transform.rotation.eulerAngles.y, 0);

                return cameraRotation * base.MovementInput;
            }
        }
    }
}