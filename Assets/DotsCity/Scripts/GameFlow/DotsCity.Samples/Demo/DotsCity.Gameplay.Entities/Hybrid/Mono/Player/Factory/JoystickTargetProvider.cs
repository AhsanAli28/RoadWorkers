using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Player
{
    public class JoystickTargetProvider : IShootTargetProvider
    {
        private readonly IMotionInput input;
        private readonly Camera camera;
        private readonly CrossHair crossHair;

        public JoystickTargetProvider(IMotionInput input, Camera camera, CrossHair crossHair)
        {
            this.input = input;
            this.camera = camera;
            this.crossHair = crossHair;
        }

        public bool GetShootDirection(Vector3 sourcePosition, out Vector3 shootDirection)
        {
            shootDirection = default;

            if (input.FireInput != default)
            {
                shootDirection = (Quaternion.Euler(0, camera.transform.rotation.eulerAngles.y, 0) * input.FireInput);
                return true;
            }

            return false;
        }

        public bool HasTarget => input.FireInput != Vector3.zero;

        public Vector3 GetTarget() => crossHair.transform.position;
    }
}
