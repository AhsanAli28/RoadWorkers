using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.DotsCity.Simulation.Common.Authoring
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        [SerializeField] private PhysicsShapeAuthoring physicsShape;
        [SerializeField] private NavMeshObstacle navMeshObstacle;

        class ObstacleAuthoringBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(ObstacleTag));
                AddComponent(entity, typeof(FactionTypeComponent));
                AddComponent(entity, typeof(HealthComponent));
                AddComponent(entity, typeof(VelocityComponent));
                AddComponent(entity, typeof(CarTag));
                AddComponent(entity, typeof(CarModelComponent));
                AddComponent(entity, typeof(AliveTag));
                AddComponent(entity, typeof(InViewOfCameraTag));

                if (authoring.physicsShape != null)
                {
                    var boxGeometry = authoring.physicsShape.GetBoxProperties();

                    float3 size = boxGeometry.Size;
                    float3 center = boxGeometry.Center;

                    AddComponent(entity, new BoundsComponent
                    {
                        Extents = size / 2,
                        Center = center
                    });

                    return;
                }

                if (authoring.navMeshObstacle != null)
                {
                    float3 size = new float3(authoring.navMeshObstacle.size.x * authoring.transform.lossyScale.x, authoring.navMeshObstacle.size.y * authoring.transform.lossyScale.y, authoring.navMeshObstacle.size.z * authoring.transform.lossyScale.z);
                    float3 center = authoring.navMeshObstacle.center;

                    AddComponent(entity, new BoundsComponent
                    {
                        Extents = size / 2,
                        Center = center
                    });
                }
            }
        }
    }
}
