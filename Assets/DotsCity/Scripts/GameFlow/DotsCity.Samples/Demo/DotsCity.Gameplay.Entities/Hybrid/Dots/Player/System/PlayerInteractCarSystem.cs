using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerInteractCarSystem : BeginSimulationSystemBase
    {
        private EntityQuery carGroup;
        private EntityQuery playerGroup;
        private EntityQuery playerNpcGroup;

        private Entity playerInteractCarStateEntity;
        private EntityQuery mobGroup;

        private IPlayerInteractCarService playerInteractCarService;
        private ICarConverter carConverter;

        protected override void OnCreate()
        {
            base.OnCreate();

            carGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalToWorld, BoundsComponent, CarModelComponent>()
                .Build(this);

            mobGroup = new EntityQueryBuilder(Allocator.Temp)
               .WithAll<PlayerMobNpcComponent>()
               .Build(this);

            playerGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag>()
                .Build(this);

            playerNpcGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, PlayerNpcComponent>()
                .Build(this);

            RequireForUpdate(playerGroup);

            Enabled = false;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            playerInteractCarStateEntity = SystemAPI.GetSingletonEntity<PlayerInteractCarStateComponent>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            var hasIgnition = false;

            if (SystemAPI.HasSingleton<CarIgnitionConfigReference>())
            {
                hasIgnition = SystemAPI.GetSingleton<CarIgnitionConfigReference>().Config.Value.HasIgnition;
            }

            BlobAssetReference<CarSharedConfig> soundConfig = default;

            if (SystemAPI.HasSingleton<CarSharedDataConfigReference>())
            {
                soundConfig = SystemAPI.GetSingleton<CarSharedDataConfigReference>().Config;
            }

            var generalCoreSettingsDataReference = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var soundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithChangeFilter<PlayerEnterCarStateComponent>()
            .ForEach((Entity playerStateEntity, in PlayerEnterCarStateComponent playerStateComponent) =>
            {
                if (playerStateComponent.EnterCarState == EnterCarState.Enter)
                {
                    var playerNpcComponent = playerNpcGroup.GetSingleton<PlayerNpcComponent>();
                    var playerNpcEntity = playerNpcGroup.GetSingletonEntity();
                    int entityId = playerNpcComponent.AvailableCarEntityIndex;

                    var entityArray = carGroup.ToEntityArray(Allocator.TempJob);

                    var entityManager = EntityManager;

                    if (generalCoreSettingsDataReference.Config.Value.DOTSSimulation)
                        PoolEntityUtils.DestroyEntity(ref entityManager, playerNpcEntity);

                    EntityManager.SetComponentData(playerStateEntity, new PlayerEnterCarStateComponent() { EnterCarState = EnterCarState.Default });

                    Entity newCarEntity = Entity.Null;

                    for (int i = 0; i < carGroup.CalculateEntityCount(); i++)
                    {
                        var sourceCarEntity = entityArray[i];

                        if (sourceCarEntity.Index != entityId)
                            continue;

                        var carModelComponent = EntityManager.GetComponentData<CarModelComponent>(sourceCarEntity);
                        var oldHasDriver = EntityManager.HasComponent<HasDriverTag>(sourceCarEntity);
                        var oldCarPosition = EntityManager.GetComponentData<LocalToWorld>(sourceCarEntity).Position;

                        var runtimeIndex = carModelComponent.Value;
                        bool shouldConvert = true;

                        if (shouldConvert)
                        {
                            newCarEntity = carConverter.Convert(ref commandBuffer, sourceCarEntity, CarType.Player);

                            if (newCarEntity != sourceCarEntity)
                            {
                                PoolEntityUtils.DestroyEntity(ref commandBuffer, sourceCarEntity);
                            }
                        }
                        else
                        {
                            newCarEntity = sourceCarEntity;
                        }

                        var shouldIgnite = hasIgnition && !oldHasDriver;

                        InteractCarUtils.EnterCar(ref commandBuffer, ref entityManager, ref soundConfig, ref soundEventQueue, newCarEntity, true, shouldIgnite, runtimeIndex, oldCarPosition);

                        GameObject car = EntityManager.GetComponentObject<Transform>(newCarEntity).gameObject;
                        GameObject npc = EntityManager.GetComponentObject<Transform>(playerNpcEntity).gameObject;

                        car.transform.position = oldCarPosition;
                        playerInteractCarService.EnterCar(car, npc);

                        if (!EntityManager.HasComponent<PlayerTag>(newCarEntity))
                        {
                            commandBuffer.AddComponent<PlayerTag>(newCarEntity);
                        }

                        commandBuffer.SetComponent(playerInteractCarStateEntity, new PlayerInteractCarStateComponent()
                        {
                            PlayerInteractCarState = PlayerInteractCarState.InCar
                        });

                        break;
                    }

                    if (mobGroup.CalculateEntityCount() > 0)
                    {
                        NativeArray<PlayerMobNpcComponent> mobNpcComponents = mobGroup.ToComponentDataArray<PlayerMobNpcComponent>(Allocator.TempJob);
                        NativeArray<Entity> mobEntities = mobGroup.ToEntityArray(Allocator.TempJob);

                        for (int i = 0; i < mobGroup.CalculateEntityCount(); i++)
                        {
                            PlayerMobNpcComponent mobNpcComponent = mobNpcComponents[i];

                            mobNpcComponent.TargetCarEntity = newCarEntity;

                            EntityManager.SetComponentData(mobEntities[i], mobNpcComponent);
                            EntityManager.AddComponent<NpcShouldEnterCarTag>(mobEntities[i]);
                        }

                        mobNpcComponents.Dispose();
                        mobEntities.Dispose();
                    }

                    entityArray.Dispose();
                }

                if (playerStateComponent.EnterCarState == EnterCarState.Leave)
                {
                    EntityManager.SetComponentData(playerStateEntity, new PlayerEnterCarStateComponent() { EnterCarState = EnterCarState.Default });

                    var playerCarEntity = playerGroup.GetSingletonEntity();
                    var playerCarPosition = EntityManager.GetComponentData<LocalToWorld>(playerCarEntity).Position;
                    var runtimeIndex = EntityManager.GetComponentData<CarModelComponent>(playerCarEntity).Value;

                    if (EntityManager.HasComponent<HasDriverTag>(playerCarEntity))
                    {
                        commandBuffer.RemoveComponent<HasDriverTag>(playerCarEntity);
                    }

                    if (EntityManager.HasComponent<PlayerTag>(playerCarEntity))
                    {
                        commandBuffer.RemoveComponent<PlayerTag>(playerCarEntity);
                    }

                    if (EntityManager.HasComponent<VehicleInputReader>(playerCarEntity))
                    {
                        commandBuffer.SetComponent(playerCarEntity, VehicleInputReader.GetBrake());
                    }

                    InteractCarUtils.ExitCar(ref commandBuffer, ref soundConfig, ref soundEventQueue, playerCarPosition, runtimeIndex);

                    if (hasIgnition)
                        InteractCarUtils.StopEngine(ref commandBuffer, playerCarEntity, true);
                    else
                        commandBuffer.RemoveComponent<HasDriverTag>(playerCarEntity);

                    GameObject car = EntityManager.GetComponentObject<Transform>(playerCarEntity).gameObject;

                    playerInteractCarService.ExitCar(car);

                    commandBuffer.SetComponent(playerInteractCarStateEntity, new PlayerInteractCarStateComponent()
                    {
                        PlayerInteractCarState = PlayerInteractCarState.CloseToCar
                    });

                    if (mobGroup.CalculateEntityCount() > 0)
                    {
                        NativeArray<PlayerMobNpcComponent> mobNpcComponents = mobGroup.ToComponentDataArray<PlayerMobNpcComponent>(Allocator.TempJob);
                        NativeArray<Entity> mobEntities = mobGroup.ToEntityArray(Allocator.TempJob);

                        for (int i = 0; i < mobGroup.CalculateEntityCount(); i++)
                        {
                            PlayerMobNpcComponent mobNpcComponent = mobNpcComponents[i];

                            mobNpcComponent.TargetCarEntity = Entity.Null;

                            EntityManager.SetComponentData(mobEntities[i], mobNpcComponent);

                            if (EntityManager.HasComponent<NpcShouldEnterCarTag>(mobEntities[i]))
                            {
                                EntityManager.RemoveComponent<NpcShouldEnterCarTag>(mobEntities[i]);
                            }
                        }

                        mobNpcComponents.Dispose();
                        mobEntities.Dispose();
                    }
                }
            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(IPlayerInteractCarService playerInteractCarService, ICarConverter carConverter)
        {
            this.playerInteractCarService = playerInteractCarService;
            this.carConverter = carConverter;
            Enabled = true;
        }
    }
}