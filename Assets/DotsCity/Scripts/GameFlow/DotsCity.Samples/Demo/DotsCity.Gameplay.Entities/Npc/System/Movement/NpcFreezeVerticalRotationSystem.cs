using Spirit604.DotsCity.Simulation.Npc;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(BeginSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcFreezeVerticalRotationSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<NpcTypeComponent, PhysicsMass, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var freezeAxisJob = new FreezeAxisJob()
            {
            };

            freezeAxisJob.Run();
        }

        [WithAll(typeof(NpcTypeComponent))]
        [BurstCompile]
        public partial struct FreezeAxisJob : IJobEntity
        {
            void Execute(
                ref PhysicsMass mass, ref LocalTransform localTransform)
            {
                mass.InverseInertia.xz = new float2(0.0f);
                localTransform.Rotation = new quaternion(0, localTransform.Rotation.value.y, 0, localTransform.Rotation.value.w);
            }
        }
    }
}