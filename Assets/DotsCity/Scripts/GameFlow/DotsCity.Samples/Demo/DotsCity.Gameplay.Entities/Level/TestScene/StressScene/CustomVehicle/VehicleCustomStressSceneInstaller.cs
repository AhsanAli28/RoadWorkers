using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Services;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleCustomStressSceneInstaller : MonoBehaviour
    {
        [SerializeField]
        private VehicleCustomStressUI vehicleCustomStressUI;

        [SerializeField]
        private EntityWorldService entityWorldService;

        [SerializeField]
        private SceneService sceneService;

        private void Start()
        {
            DefaultWorldUtils.CreateAndAddSystemManaged<VehicleLineTracker, StructuralSystemGroup>().Initialize(vehicleCustomStressUI);
            DefaultWorldUtils.CreateAndAddSystemManaged<VehicleStressSpawner, StructuralSystemGroup>().Initialize(vehicleCustomStressUI);
            sceneService.Construct(entityWorldService);
        }
    }
}
