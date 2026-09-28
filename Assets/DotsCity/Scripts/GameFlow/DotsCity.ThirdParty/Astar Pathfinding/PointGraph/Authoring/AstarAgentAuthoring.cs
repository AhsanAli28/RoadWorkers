using Pathfinding.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public struct ManagedStateInit : IComponentData { }

    public class AstarAgentAuthoring : MonoBehaviour
    {
        class AstarAgentAuthoringBaker : Baker<AstarAgentAuthoring>
        {
            public override void Bake(AstarAgentAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new AutoRepathPolicy()
                {
                    mode = Pathfinding.AutoRepathPolicy.Mode.Never,
                });

                AddComponent<DestinationPoint>(entity);
                AddComponent(entity, new AgentMovementPlane()
                {
                    value = new Pathfinding.Util.NativeMovementPlane(quaternion.identity)
                });

                AddComponent<ManagedStateInit>(entity);
            }
        }
    }
}

