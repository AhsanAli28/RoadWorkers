using UnityEngine;
using Spirit604.DotsCity.Simulation.Initialization;

#if !DOTS_SIMULATION
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Debug;
using Spirit604.DotsCity.TestScene.UI;
using Spirit604.Gameplay.Services;
#endif

namespace Spirit604.DotsCity.TestScene
{
    public class CustomVehicleTestManager : MonoBehaviour
    {
#if !DOTS_SIMULATION
        [SerializeField] private CustomVehicleTestView customVehicleTestView;
        [SerializeField] private SpeedometerViewAdapter speedometerViewAdapter;
        [SerializeField] private VehicleControl vehicleControl;
        [SerializeField] private VehicleCustomDebugger vehicleCustomDebugger;
        [SerializeField] private EntityWorldService entityWorldService;
        [SerializeField] private SceneService sceneService;

        private void Awake()
        {
            customVehicleTestView.OnExitClicked += CustomVehicleTestView_OnExitClicked;
            sceneService.Construct(entityWorldService);
            sceneService.OnSceneUnloaded += SceneService_OnSceneUnloaded;
            TrafficInitializer.InitCustomVehicleSystems();
        }

        private void SceneService_OnSceneUnloaded()
        {
            speedometerViewAdapter.enabled = false;
            vehicleControl.enabled = false;
            vehicleCustomDebugger.enabled = false;
        }

        private void CustomVehicleTestView_OnExitClicked()
        {
            sceneService.LoadScene(0);
        }
#endif
    }
}