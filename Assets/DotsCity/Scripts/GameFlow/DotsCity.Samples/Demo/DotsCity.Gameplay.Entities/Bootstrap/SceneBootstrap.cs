using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.CameraService;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.DotsCity.Simulation.Bootstrap;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Bootstrap
{
    public class SceneBootstrap : SimulationBootstrap
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        private IPlayerSpawnerService playerSpawnerService;
        private CameraController cameraController;

        [InjectWrapper]
        public void Construct(IPlayerSpawnerService playerSpawnerService, CameraController cameraController = null)
        {
            this.playerSpawnerService = playerSpawnerService;
            this.cameraController = cameraController;
        }

        protected override void RegisterPlayerSpawn()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var worldUnmanaged = World.DefaultGameObjectInjectionWorld.Unmanaged;
            var entityManager = world.EntityManager;

            if (citySettingsInitializer.GetSettings<GeneralSettingData>().BuiltInSolution)
            {
                commands.Add(new PlayerSpawnCommand(entityManager, worldUnmanaged, playerSpawnerService, this));

                // If the user wants to replace the camera with his own solution.
                if (cameraController != null)
                {
                    commands.Add(new WarpCameraCommand(cameraController, this));
                }
            }
        }
    }
}