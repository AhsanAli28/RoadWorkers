using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [BurstCompile]
    public partial struct EntityTrackerSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<EntityTrackerComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var entityTrackJob = new EntityTrackJob()
            {
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
            };

            entityTrackJob.ScheduleParallel();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [BurstCompile]
        public partial struct EntityTrackJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            void Execute(
                ref LocalTransform localTransform,
                EnabledRefRW<PooledEventTag> pooledEventTagRW,
                in EntityTrackerComponent entityTrackerComponent)
            {
                bool shouldDestroy = true;

                var linkedEntity = entityTrackerComponent.LinkedEntity;
                if (linkedEntity != Entity.Null &&
                LocalToWorldLookup.HasComponent(linkedEntity) &&
                (!entityTrackerComponent.TrackOnlyInView || (InViewOfCameraLookup.HasComponent(linkedEntity) && InViewOfCameraLookup.IsComponentEnabled(linkedEntity)))
                )
                {
                    var targetTransform = LocalToWorldLookup[linkedEntity];

                    localTransform.Position = targetTransform.Position;

                    if (entityTrackerComponent.HasOffset)
                    {
                        localTransform.Position += math.mul(targetTransform.Rotation, entityTrackerComponent.Offset);
                    }

                    localTransform.Rotation = targetTransform.Rotation;
                    shouldDestroy = false;
                }

                if (shouldDestroy)
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}