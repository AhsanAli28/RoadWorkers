using Spirit604.DotsCity.Gameplay.Factory;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerTargetProvider : MonoBehaviour
    {
        [SerializeField] private CrossHairCreator crossHairCreator;

        private CrossHair crossHair;

        public IShootTargetProvider Create(IMotionInput input, Camera camera, ShootDirectionSource shootDirectionSource)
        {
            crossHair = crossHairCreator.Create();

            switch (shootDirectionSource)
            {
                case ShootDirectionSource.Joystick:
                    return new JoystickTargetProvider(input, camera, crossHair);
                case ShootDirectionSource.CrossHair:
                    return new JoystickTargetProvider(input, camera, crossHair);
                case ShootDirectionSource.Mouse:
                    return new PlayerShootMouseTargetProvider(camera);
            }

            return null;
        }
    }
}
