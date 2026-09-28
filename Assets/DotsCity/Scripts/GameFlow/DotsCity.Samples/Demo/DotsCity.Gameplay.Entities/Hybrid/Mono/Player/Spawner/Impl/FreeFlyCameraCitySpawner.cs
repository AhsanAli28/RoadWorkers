using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Player;
using Spirit604.Gameplay.UI;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class FreeFlyCameraCitySpawner : PlayerSpawnerBase
    {
        private readonly Camera mainCamera;
        private readonly FreeFlyCameraFactory freeFlyCameraFactory;
        private readonly IMotionInput input;
        private readonly InputManager joystickManager;

        private EntityManager entityManager;
        private EntityArchetype cameraEntityArchetype;

        public FreeFlyCameraCitySpawner(FreeFlyCameraFactory freeFlyCameraFactory, IMotionInput input, Camera mainCamera = null, InputManager joystickManager = null)
        {
            this.mainCamera = mainCamera;
            this.freeFlyCameraFactory = freeFlyCameraFactory;
            this.input = input;
            this.joystickManager = joystickManager;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public override void Initialize()
        {
            if (joystickManager != null)
            {
                joystickManager.SetRelativeCamera(0, false);
                joystickManager.SetRelativeCamera(1, false);
            }

            cameraEntityArchetype = entityManager.CreateArchetype(
                    typeof(LocalToWorld),
                    typeof(LocalTransform),
                    typeof(CopyTransformFromGameObject),
                    typeof(PlayerTag));
        }

        public override GameObject Spawn(PlayerSpawnDataConfig playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (spawnPosition == Vector3.zero && mainCamera != null)
            {
                spawnPosition = mainCamera.transform.position;
            }

            var target = CreateFreeFlyObject(spawnPosition, spawnRotation);

            var hybridEntity = entityManager.CreateEntity(cameraEntityArchetype);

            entityManager.AddComponentObject(hybridEntity, target.transform);

            var playerInteractCarStateEntity = entityManager.CreateEntityQuery(typeof(PlayerInteractCarStateComponent)).GetSingletonEntity();

            entityManager.SetComponentData(playerInteractCarStateEntity, new PlayerInteractCarStateComponent()
            {
                PlayerInteractCarState = PlayerInteractCarState.OutOfCar
            });

            return target;
        }

        private GameObject CreateFreeFlyObject(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var target = freeFlyCameraFactory.Spawn(spawnPosition, spawnRotation);
            target.GetComponent<BasicFlight>().Initialize(input);

            return target;
        }
    }
}
