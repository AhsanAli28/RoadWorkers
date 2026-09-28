using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficNodeConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficCrossroadPostConversionSystem : SimpleSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity crossRoadEntity, ref CrossroadBakingData crossroadData, ref DynamicBuffer<SegmentTrafficNodeData> nodes) =>
            {
                for (int i = 0; i < crossroadData.TrafficNodeScopes.Length; i++)
                {
                    if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(crossroadData.TrafficNodeScopes[i]))
                        continue;

                    var scopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(crossroadData.TrafficNodeScopes[i]);

                    var rightLaneEntities = scopeData.RightLaneEntities;
                    var leftLaneEntities = scopeData.LeftLaneEntities;

                    for (int j = 0; j < rightLaneEntities.Length; j++)
                    {
                        AddEntity(rightLaneEntities[j].Entity, ref nodes);
                    }

                    for (int j = 0; j < leftLaneEntities.Length; j++)
                    {
                        AddEntity(leftLaneEntities[j].Entity, ref nodes);
                    }
                }

                if (crossroadData.SubNodes.IsCreated)
                {
                    for (int j = 0; j < crossroadData.SubNodes.Length; j++)
                    {
                        AddEntity(crossroadData.SubNodes[j], ref nodes);
                    }
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void AddEntity(Entity entity, ref DynamicBuffer<SegmentTrafficNodeData> nodes)
        {
            nodes.Add(new SegmentTrafficNodeData()
            {
                Entity = entity
            });
        }
    }
}