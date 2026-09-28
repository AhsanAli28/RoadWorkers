using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common.Authoring
{
    public class SimpleRouteFollowerAuthoring : MonoBehaviour
    {
        [SerializeField] private SimpleRoute route;
        [SerializeField] private bool hybridEntity;
        [SerializeField] private bool shouldFollow = true;
        [SerializeField][Range(0, 20)] private float movementSpeed = 4f;
        [SerializeField][Range(0, 10)] private float achieveDistance = 0.1f;

        class SimpleRouteFollowerAuthoringBaker : Baker<SimpleRouteFollowerAuthoring>
        {
            public override void Bake(SimpleRouteFollowerAuthoring authoring)
            {
                if (authoring.route == null || !authoring.shouldFollow)
                {
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.hybridEntity)
                {
                    AddComponent(entity, typeof(CopyTransformToGameObject));
                    AddComponentObject(entity, authoring.transform);
                }

                var points = authoring.route.Points;

                AddComponent(entity, typeof(SimpleRouteFollowerComponent));

                AddComponent(entity, new SimpleRouteFollowerSettingsComponent()
                {
                    MovementSpeed = authoring.movementSpeed,
                    AchieveDistance = authoring.achieveDistance
                });

                var bufferPoints = AddBuffer<SimpleRouteElement>(entity);

                for (int i = 0; i < points?.Length; i++)
                {
                    bufferPoints.Add(new SimpleRouteElement()
                    {
                        Position = points[i].transform.position
                    });
                }
            }
        }
    }
}
