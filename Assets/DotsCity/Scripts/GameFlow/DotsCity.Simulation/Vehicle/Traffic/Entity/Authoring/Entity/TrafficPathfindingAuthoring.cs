using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Entities;
using UnityEngine;
using static Spirit604.DotsCity.Simulation.Traffic.TrafficPathfindingSystem;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficPathfindingAuthoring : MonoBehaviour
    {
        public bool builtInPathfinding = true;

        class PedestrianPathfindingAuthoringBaker : Baker<TrafficPathfindingAuthoring>
        {
            public override void Bake(TrafficPathfindingAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(TrafficFixedRouteTag));
                SetComponentEnabled<TrafficFixedRouteTag>(entity, false);
                AddComponent(entity, typeof(TrafficFixedRouteComponent));

                AddBuffer<FixedRouteNodeElement>(entity);

                AddComponent(entity, typeof(PathAgentComponent));

                AddComponent(entity, typeof(PathAgentUpdateState));
                SetComponentEnabled<PathAgentUpdateState>(entity, false);

                if (authoring.builtInPathfinding)
                {
                    AddComponent(entity, typeof(PathRequest));
                    SetComponentEnabled<PathRequest>(entity, false);
                }
            }
        }
    }
}
