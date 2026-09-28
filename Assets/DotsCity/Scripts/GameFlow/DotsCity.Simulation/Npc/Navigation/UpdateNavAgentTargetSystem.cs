#if REESE_PATH
using Reese.Path;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [UpdateAfter(typeof(NpcRecalculateNavTargetSystem))]
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct UpdateNavAgentTargetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<IdleTag>()
                .WithDisabled<PathPlanning, PathDestination>()
                .WithAll<UpdateNavTargetTag, NavAgentTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navTargetJob = new NavTargetJob()
            {
                PathDestinationLookup = SystemAPI.GetComponentLookup<PathDestination>(false),
                EnabledNavigationLookup = SystemAPI.GetComponentLookup<EnabledNavigationTag>(false),
                PathBufferLookup = SystemAPI.GetBufferLookup<PathBufferElement>(false),
                NavAgentConfigReference = SystemAPI.GetSingleton<NavAgentConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            navTargetJob.Schedule();
        }

        [WithNone(typeof(IdleTag))]
        [WithDisabled(typeof(PathPlanning), typeof(PathDestination))]
        [WithAll(typeof(UpdateNavTargetTag), typeof(NavAgentTag))]
        [BurstCompile]
        public partial struct NavTargetJob : IJobEntity
        {
            public ComponentLookup<PathDestination> PathDestinationLookup;
            public ComponentLookup<EnabledNavigationTag> EnabledNavigationLookup;
            public BufferLookup<PathBufferElement> PathBufferLookup;

            [ReadOnly]
            public NavAgentConfigReference NavAgentConfigReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref NavAgentComponent navAgentComponent,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW)
            {
                updateNavTargetTagRW.ValueRW = false;

                if (Timestamp - navAgentComponent.LastUpdateTimestamp <= NavAgentConfigReference.Config.Value.UpdateFrequency)
                    return;

                if (navAgentComponent.PathEndPosition.IsEqual(float3.zero))
                    return;

                navAgentComponent.LastUpdateTimestamp = Timestamp;

                navAgentComponent.PathIndex = 0;

                float3 target = navAgentComponent.PathEndPosition;

                var pathDestinationPath = new PathDestination()
                {
                    WorldPoint = target
                };

                if (PathBufferLookup.HasBuffer(entity))
                {
                    var localPathBuffer = PathBufferLookup[entity];
                    localPathBuffer.Clear();
                }

                if (!EnabledNavigationLookup.IsComponentEnabled(entity))
                {
                    EnabledNavigationLookup.SetComponentEnabled(entity, true);
                }

                PathDestinationLookup[entity] = pathDestinationPath;
                PathDestinationLookup.SetComponentEnabled(entity, true);
            }
        }
    }
}

#endif
