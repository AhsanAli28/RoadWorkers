using Spirit604.DotsCity.Simulation.Navigation;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianPathfindingAuthoring : MonoBehaviour
    {
        public bool builtInPathfinding = true;

        class PedestrianPathfindingAuthoringBaker : Baker<PedestrianPathfindingAuthoring>
        {
            public override void Bake(PedestrianPathfindingAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(PathAgentComponent));

                AddComponent(entity, typeof(CustomReachTargetTag));
                SetComponentEnabled<CustomReachTargetTag>(entity, false);

                AddComponent(entity, typeof(PathAgentUpdateState));
                SetComponentEnabled<PathAgentUpdateState>(entity, false);

                AddBuffer<NodeSequenceElement>(entity);

                if (authoring.builtInPathfinding)
                {
                    AddComponent<PedestrianPathfindingSystem.PathRequest>(entity);
                    SetComponentEnabled<PedestrianPathfindingSystem.PathRequest>(entity, false);
                }
            }
        }
    }
}
