using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Npc;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay
{
    public class ArcadeMonoCarConverter : MonoCarConverterBase
    {
        private const bool AddDriverByDefault = false || !EntityEnteringLogic;
        private const bool EntityEnteringLogic = true;

        private bool initialized;
        private CarIgnitionConfigReference carIgnitionConfigReference;
        private CarSharedDataConfigReference soundConfigRef;
        private GeneralCoreSettingsDataReference generalCoreSettingsDataReference;
        private SoundEventPlaybackSystem.Singleton soundEventQueue;

        private EntityQuery soundEventQuery;
        private PlayerCarPool playerCarPool;
        private PlayerCarSpawner playerCarSpawner;
        private IShootTargetProvider targetProvider;
        private INpcInteractCarService npcInteractCarService;

        public ArcadeMonoCarConverter(
            PlayerCarPool playerCarPool,
            PlayerCarSpawner playerCarSpawner,
            IShootTargetProvider targetProvider,
            INpcInteractCarService npcInteractCarService)
        {
            this.playerCarPool = playerCarPool;
            this.playerCarSpawner = playerCarSpawner;
            this.targetProvider = targetProvider;
            this.npcInteractCarService = npcInteractCarService;
        }

        public override bool EnterCar(GameObject enteredCar, CarType newType)
        {
            Initialize();

            var oldHasDriver = true;
            int runtimeIndex = -1;

            if (enteredCar.TryGetComponent<IHybridEntityRef>(out var entityRef))
            {
                oldHasDriver = EntityManager.HasComponent<HasDriverTag>(entityRef.RelatedEntity);

                if (EntityManager.HasComponent<CarModelComponent>(entityRef.RelatedEntity))
                {
                    var carModel = EntityManager.GetComponentData<CarModelComponent>(entityRef.RelatedEntity);
                    runtimeIndex = carModel.Value;
                }
            }

            var converted = base.EnterCar(enteredCar, newType);

#pragma warning disable CS0162
            if (!EntityEnteringLogic)
                return converted;
#pragma warning restore CS0162

            var newCarEntity = enteredCar.GetComponent<CarReferences>().RelatedEntity;

            if (newCarEntity == Entity.Null)
            {
                return converted;
            }

            if (runtimeIndex == -1)
            {
                if (!oldHasDriver)
                {
                    var commandBuffer2 = new EntityCommandBuffer(Allocator.Temp);
                    commandBuffer2.AddComponent<CarEngineStartedTag>(newCarEntity);
                    commandBuffer2.AddComponent<HasDriverTag>(newCarEntity);
                    commandBuffer2.Playback(EntityManager);
                    commandBuffer2.Dispose();
                }

                return converted;
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var hasIgnition = HasIgnition();

            var shouldIgnite = hasIgnition && !oldHasDriver;

            var entityManager = EntityManager;

            BlobAssetReference<CarSharedConfig> soundConfig = default;

            if (soundConfigRef.Config != null)
            {
                soundConfig = soundConfigRef.Config;
            }

            InteractCarUtils.EnterCar(
                ref commandBuffer,
                ref entityManager,
                ref soundConfig,
                ref soundEventQueue,
                newCarEntity,
                true,
                shouldIgnite,
                runtimeIndex,
                enteredCar.transform.position);

            if (!converted)
            {
                commandBuffer.AddComponent<PlayerTag>(newCarEntity);
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            return converted;
        }

        public override void ExitCar(GameObject exitCar, CarType newType)
        {
            Initialize();

            base.ExitCar(exitCar, newType);

#pragma warning disable CS0162
            if (!EntityEnteringLogic)
                return;
#pragma warning restore CS0162

            var runtimeIndex = -1;

            Entity playerCarEntity = Entity.Null;

            if (exitCar.TryGetComponent<IHybridEntityRef>(out var entityRef))
            {
                playerCarEntity = entityRef.RelatedEntity;
            }

            if (EntityManager.HasComponent<CarModelComponent>(playerCarEntity))
                runtimeIndex = EntityManager.GetComponentData<CarModelComponent>(playerCarEntity).Value;

            if (runtimeIndex == -1) return;

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            BlobAssetReference<CarSharedConfig> soundConfig = default;

            if (soundConfigRef.Config != null)
            {
                soundConfig = soundConfigRef.Config;
            }

            InteractCarUtils.ExitCar(
                ref commandBuffer,
                ref soundConfig,
                ref soundEventQueue,
                exitCar.transform.position,
                runtimeIndex);

            var hasIgnition = HasIgnition();

            if (hasIgnition)
            {
                InteractCarUtils.StopEngine(ref commandBuffer, playerCarEntity, true);
                commandBuffer.RemoveComponent<HasDriverTag>(playerCarEntity);
            }
            else
                commandBuffer.RemoveComponent<HasDriverTag>(playerCarEntity);

            commandBuffer.RemoveComponent<PlayerTag>(playerCarEntity);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        protected override void InitCustomComponents(GameObject enteredCar, int carModel, CarType newType)
        {
            if (newType == CarType.Player)
            {
                ConvertPlayerCar(enteredCar, carModel, AddDriverByDefault);
            }
        }

        private void ConvertPlayerCar(GameObject car, int carModel, bool addDriver = true)
        {
            var prefab = playerCarPool.GetCarGoPrefab(carModel);

            if (prefab.TryGetComponent<CarSlots>(out var sourceSlots))
            {
                var sourceSlotsParent = sourceSlots.DriverSlot.transform.parent;

                var slots = GameObject.Instantiate(sourceSlotsParent, car.transform);
                slots.name = sourceSlotsParent.name;
                slots.SetLocalPositionAndRotation(default, Quaternion.identity);

                var newSlots = car.gameObject.AddComponent<CarSlots>();
                newSlots.InitSlots();

                var playerNpcCarBehaviour = car.AddComponent<PlayerNpcCarBehaviour>();

                playerNpcCarBehaviour.Initialize(targetProvider);
                newSlots.Initialize(npcInteractCarService);
            }

            var playerActor = car.AddComponent<PlayerActor>();
            playerActor.CurrentActorType = PlayerActorType.Car;

            var vehicleEntity = playerCarSpawner.SpawnEntity(carModel, addDriver);

            EntityManager.AddComponentObject(vehicleEntity, car.transform);
            EntityManager.AddComponentObject(vehicleEntity, car.GetComponent<Rigidbody>());

            var carReferences = car.AddComponent<CarReferences>();
            carReferences.Initialize(vehicleEntity);

            var input = car.AddComponent<PlayerHybridInputControl>();
            input.SetEntityRef(carReferences);

            EntityManager.SetComponentData(vehicleEntity, LocalTransform.FromPositionRotation(car.transform.position, car.transform.rotation));

            if (car.TryGetComponent<IVehicleSettingsSync>(out var vehicleSync))
            {
                vehicleSync.Sync(prefab);
            }

            if (car.TryGetComponent<IFactionProvider>(out var factionProvider))
            {
                factionProvider.FactionType = FactionType.Player;
            }

            // This is a hacky way to invoke a method without referencing an assembly
            MonoBehaviour component = car.GetComponent("Spirit604.DotsCity.Simulation.Mono.ArcadeVehicleController") as MonoBehaviour;

            component.Invoke("InitInput", 0);
        }

        private bool HasIgnition()
        {
            var hasIgnition = false;

            if (carIgnitionConfigReference.Config != default)
            {
                hasIgnition = carIgnitionConfigReference.Config.Value.HasIgnition;
            }

            return hasIgnition;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            var ignitionQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CarIgnitionConfigReference>());

            if (ignitionQuery.CalculateEntityCount() == 1)
            {
                carIgnitionConfigReference = ignitionQuery.GetSingleton<CarIgnitionConfigReference>();
            }

            var sharedConfigQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CarSharedDataConfigReference>());

            if (sharedConfigQuery.CalculateEntityCount() == 1)
            {
                soundConfigRef = sharedConfigQuery.GetSingleton<CarSharedDataConfigReference>();
            }

            generalCoreSettingsDataReference = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GeneralCoreSettingsDataReference>()).GetSingleton<GeneralCoreSettingsDataReference>();

            var soundSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SoundEventPlaybackSystem>().SystemHandle;
            soundEventQueue = EntityManager.GetComponentData<SoundEventPlaybackSystem.Singleton>(soundSystem);
        }
    }
}