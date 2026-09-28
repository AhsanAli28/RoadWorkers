using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if REESE_PATH
using Reese.Path;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Extensions;
using UnityEngine;
#endif

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class MobEnterCarSystem : BeginSimulationSystemBase
    {
        private const float DistanceToEnterCar = 0.7f;

        private EntityQuery playerQuery;
        private EntityQuery npcQuery;

        private PlayerActorTracker playerTargetHandler;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, LocalToWorld>()
                .Build(this);

            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<NpcShouldEnterCarTag, NpcEnterCarTag>()
                .WithAll<PlayerMobNpcComponent>()
                .Build(this);

            RequireForUpdate(npcQuery);
            Enabled = false;
        }

        protected override void OnUpdate()
        {
#if REESE_PATH
            if (playerQuery.CalculateEntityCount() != 1)
            {
                return;
            }

            var commandBuffer = GetCommandBuffer();

            var playerPosition = playerQuery.GetSingleton<LocalToWorld>().Position;

            Entities
            .WithBurst()
            .WithNone<NpcEnterCarTag>()
            .WithNone<NpcCustomDestinationComponent, NpcCustomReachComponent, UpdateNavTargetTag>()
            .WithAll<NpcShouldEnterCarTag, PlayerMobNpcComponent, AliveTag>()
            .ForEach((
                Entity entity,
                ref InputComponent inputComponent,
                ref NavAgentComponent navAgentComponent,
                in NavAgentSteeringComponent navAgentSteeringComponent,
                in LocalToWorld worldTransform) =>
            {
                float3 npcPosition = worldTransform.Position.Flat();

                var targetPosition = GetCarTargetPosition(playerPosition, npcPosition);

                float distanceToCar = math.distance(npcPosition, navAgentComponent.PathEndPosition);

                bool hasTarget = distanceToCar > DistanceToEnterCar && navAgentSteeringComponent.HasSteeringTarget;

                if (distanceToCar > DistanceToEnterCar)
                {
                    bool pathPlanning = SystemAPI.HasComponent<PathPlanning>(entity) && SystemAPI.IsComponentEnabled<PathPlanning>(entity);

                    if (!pathPlanning && !navAgentComponent.PathEndPosition.IsEqual(targetPosition, 0.2f))
                    {
                        navAgentComponent.PathEndPosition = targetPosition;
                        commandBuffer.SetComponentEnabled<UpdateNavTargetTag>(entity, true);
                    }
                }
                else
                {
                    commandBuffer.AddComponent(entity, new NpcEnterCarTag());
                }

                if (hasTarget)
                {
                    inputComponent.MovingInput = ((Vector3)(navAgentSteeringComponent.SteeringTargetValue.Flat() - npcPosition)).normalized.ToVector2_2DSpace();
                }
                else
                {
                    inputComponent.MovingInput = ((Vector3)math.normalize(targetPosition.Flat() - npcPosition)).ToVector2_2DSpace();
                }
            }).Schedule();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<NpcEnterCarTag, PlayerMobNpcComponent>()
            .ForEach((
                Entity entity,
                Transform npcTransform) =>
            {
                var targetCar = playerTargetHandler.Actor.transform;
                var npc = npcTransform.GetComponent<NpcBehaviourBase>();
                var newNpc = targetCar?.GetComponent<CarSlots>()?.EnterCar(npc.ID, npc.gameObject);
            }).Run();

            AddCommandBufferForProducer();
#endif
        }

        private static float3 GetCarTargetPosition(float3 target, float3 npcPosition)
        {
            float3 directionToTarget = math.normalize(target - npcPosition);

            float3 targetPosition = target - directionToTarget * 1f;

            return targetPosition;
        }


        public void Initialize(PlayerActorTracker playerTargetHandler)
        {
            this.playerTargetHandler = playerTargetHandler;
            Enabled = true;
        }
    }
}