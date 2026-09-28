using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcCustomTargetHandlerSystem : ISystem
    {
        private const float achieveDistance = 0.2f;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<NpcShouldEnterCarTag, NpcCustomReachComponent, NpcInitializeCustomDestinationTag>()
                .WithAll<InputComponent, NpcCustomDestinationComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var customTargetJob = new CustomTargetJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            };

            customTargetJob.Schedule();
        }

        [WithNone(typeof(NpcShouldEnterCarTag), typeof(NpcCustomReachComponent), typeof(NpcInitializeCustomDestinationTag))]
        [BurstCompile]
        public partial struct CustomTargetJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref InputComponent inputComponent,
                in NpcCustomDestinationComponent npcCustomTarget,
                in NavAgentSteeringComponent navAgentSteeringComponent)
            {
                float distanceToTarget = math.distance(transform.Position, npcCustomTarget.Destination);

                bool hasTarget = false;

                if (distanceToTarget < achieveDistance)
                {
                    if (!npcCustomTarget.DstRotation.Equals(quaternion.identity))
                    {
                        transform.Rotation = npcCustomTarget.DstRotation;
                    }

                    CommandBuffer.AddComponent<NpcCustomReachComponent>(entity);
                    CommandBuffer.RemoveComponent<NpcCustomDestinationComponent>(entity);
                }
                else if (navAgentSteeringComponent.HasSteeringTarget)
                {
                    hasTarget = true;
                }

                if (hasTarget)
                {
                    inputComponent.MovingInput = math.normalize(navAgentSteeringComponent.SteeringTargetValue - transform.Position).To2DSpace();
                }
                else
                {
                    inputComponent.MovingInput = float2.zero;
                }
            }
        }
    }
}