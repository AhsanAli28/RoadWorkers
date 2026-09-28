using Pathfinding;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarTrafficStaticGraph : AstarStaticGraphBase
    {
        private EntityQuery trafficNodeQuery;
        private EntityQuery graphQuery;

        private void Awake()
        {
            trafficNodeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeComponent>());
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
        }

        protected override bool WaitCondition() => graphQuery.CalculateEntityCount() == 0;

        protected override void CreateGraph()
        {
            var entities = trafficNodeQuery.ToEntityArray(Allocator.Temp);

            AstarPath.active.AddWorkItem(() =>
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var pos = EntityManager.GetComponentData<LocalTransform>(entity).Position;

                    var graphNode = PointGraph.AddNode(new CustomEntityNode(AstarPath.active)
                    {
                        Entity = entity,

                    }, PosToInt(pos));

                    entityBinding.Add(entity, graphNode);
                    entityBindingInversed.Add(graphNode, entity);
                }

                var graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var pathBuffer = EntityManager.GetBuffer<PathConnectionElement>(entity, true);
                    var sourceGraphNode = entityBinding[entity];

                    for (int j = 0; j < pathBuffer.Length; j++)
                    {
                        var connectedEntity = pathBuffer[j].ConnectedNodeEntity;
                        var connectedGraphNode = entityBinding[connectedEntity];

                        var directionality = OffMeshLinks.Directionality.OneWay;

                        ref readonly var pathData = ref graph.GetPathData(pathBuffer[j].GlobalPathIndex);

                        PointNode.Connect(sourceGraphNode, connectedGraphNode, (uint)pathData.PathLength, directionality);
                    }
                }
            });

            AstarPath.active.FlushWorkItems();

            entities.Dispose();
        }
    }
}
