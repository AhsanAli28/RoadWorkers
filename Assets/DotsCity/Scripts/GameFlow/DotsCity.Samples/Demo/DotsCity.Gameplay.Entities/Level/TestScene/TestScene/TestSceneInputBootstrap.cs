using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class TestSceneInputBootstrap : MonoBehaviour
    {
        [SerializeField] private InputManager inputManager;
        [SerializeField] private VehicleOrbitalCamera vehicleOrbitalCamera;
        [SerializeField] private bool forceMobile;

        private ICarMotionInput input;
        private OrbitalPcInput pcInput;

        private bool IsMobile => Application.isMobilePlatform || forceMobile;

        private void Awake()
        {
            if (IsMobile)
            {
                input = new TopDownOrbitalMobileCarMotionInput(inputManager);
            }
            else
            {
                pcInput = new OrbitalPcInput();
                input = pcInput;
            }

            inputManager.SwitchEnabledState(IsMobile);
            DefaultWorldUtils.CreateAndAddSystemManaged<PlayerVehicleInputSystem, MainThreadInitGroup>().Initialize(input);
            vehicleOrbitalCamera.Initialize(input);
        }

        private void Update()
        {
            pcInput?.Tick();
        }
    }
}
