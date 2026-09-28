using Pathfinding;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarPedestrianStaticGraph : AstarStaticGraphBase
    {
        private EntityQuery pedestrianNodeQuery;

        private void Awake()
        {
            pedestrianNodeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeSettingsComponent>());
        }

        protected override bool WaitCondition() => pedestrianNodeQuery.CalculateEntityCount() == 0;

        protected override void CreateGraph()
        {
            var entities = pedestrianNodeQuery.ToEntityArray(Allocator.Temp);

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

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity, true);
                    var pos = EntityManager.GetComponentData<LocalTransform>(entity).Position;
                    var sourceGraphNode = entityBinding[entity];

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        var connectedEntity = buffer[j].ConnectedEntity;
                        var connectedGraphNode = entityBinding[connectedEntity];
                        var connectedPos = EntityManager.GetComponentData<LocalTransform>(connectedEntity).Position;

                        var directionality = OffMeshLinks.Directionality.TwoWay;

                        if (!PedestrianNodeEntityUtils.IsConnectedTo(EntityManager, connectedEntity, entity))
                        {
                            directionality = OffMeshLinks.Directionality.OneWay;
                        }

                        PointNode.Connect(sourceGraphNode, connectedGraphNode, GetCost(pos, connectedPos), directionality);
                    }
                }
            });

            AstarPath.active.FlushWorkItems();

            entities.Dispose();
        }
    }
}
