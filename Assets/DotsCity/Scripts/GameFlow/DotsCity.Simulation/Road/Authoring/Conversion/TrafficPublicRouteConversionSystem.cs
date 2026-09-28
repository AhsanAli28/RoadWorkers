using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.DotsCity.Simulation.TrafficPublic.Authoring;
using Spirit604.Gameplay.Road;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficPathConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficPublicRouteConversionSystem : SystemBase
    {
        private TrafficNodeConversionSystem trafficNodeConversionSystem;
        private TrafficPathConversionSystem trafficPathConversionSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            trafficNodeConversionSystem = World.GetExistingSystemManaged<TrafficNodeConversionSystem>();
            trafficPathConversionSystem = World.GetExistingSystemManaged<TrafficPathConversionSystem>();
        }

        protected override void OnUpdate()
        {
            trafficPathConversionSystem.GetDependency().Complete();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((
                Entity entity,
                ref DynamicBuffer<FixedRouteNodeElement> fixedRouteBuffer,
                in TrafficPublicRouteTempBakingData route) =>
            {
                for (int i = 0; i < route.RouteNodes.Length; i++)
                {
                    var fixedRouteBufferElement = fixedRouteBuffer[i];
                    var tempRouteNodeData = route.RouteNodes[i];

                    if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(tempRouteNodeData.TrafficNodeScopeEntity))
                    {
                        UnityEngine.Debug.Log($"TrafficPublicRouteConversionSystem. Converted path PathInstanceId {tempRouteNodeData.PathInstanceId} entity {tempRouteNodeData.TrafficNodeScopeEntity.Index} doesn't have TrafficNodeScope component{TrafficObjectFinderMessage.GetMessage()}");
                    }

                    var scopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(tempRouteNodeData.TrafficNodeScopeEntity);

                    Entity trafficNodeLaneEntity = Entity.Null;

                    if (tempRouteNodeData.RightPathDirection || scopeData.HasMerge)
                    {
                        if (scopeData.RightLaneEntities.Length > tempRouteNodeData.SourceLaneIndex)
                        {
                            trafficNodeLaneEntity = scopeData.RightLaneEntities[tempRouteNodeData.SourceLaneIndex].Entity;
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"TrafficPublicRouteConversionSystem RightLaneEntity not found. LaneCount {scopeData.RightLaneEntities.Length} LaneIndex {tempRouteNodeData.SourceLaneIndex} TrafficNode InstanceId {scopeData.TrafficNodeInstanceId}{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }
                    else
                    {
                        if (scopeData.LeftLaneEntities.Length > tempRouteNodeData.SourceLaneIndex)
                        {
                            trafficNodeLaneEntity = scopeData.LeftLaneEntities[tempRouteNodeData.SourceLaneIndex].Entity;
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"TrafficPublicRouteConversionSystem LeftLaneEntity not found. LaneCount {scopeData.LeftLaneEntities.Length} LaneIndex {tempRouteNodeData.SourceLaneIndex} TrafficNode InstanceId {scopeData.TrafficNodeInstanceId}{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }

                    if (trafficNodeLaneEntity != Entity.Null)
                    {
                        if (tempRouteNodeData.InitRotation)
                        {
                            var nodeRotation = EntityManager.GetComponentData<LocalTransform>(trafficNodeLaneEntity).Rotation;
                            fixedRouteBufferElement.Rotation = nodeRotation;
                        }
                    }

                    var globalPathIndex = trafficNodeConversionSystem.InstanceIdToGlobalIndexPathMap[tempRouteNodeData.PathInstanceId];

                    fixedRouteBufferElement.TrafficNodeEntity = trafficNodeLaneEntity;
                    fixedRouteBufferElement.PathKey = globalPathIndex;

                    fixedRouteBuffer[i] = fixedRouteBufferElement;
                }
            }).Run();
        }
    }
}