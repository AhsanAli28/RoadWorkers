using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train.Authoring
{
    [TemporaryBakingType]
    public struct TrafficParentWagonBakingTag : IComponentData { }

    public class TrafficParentWagonAuthoring : MonoBehaviour
    {
        [SerializeField] private float wagonOffset = 0.5f;
        [SerializeField] private List<GameObject> wagons = new List<GameObject>();

        class TrafficParentWagonAuthoringBaker : Baker<TrafficParentWagonAuthoring>
        {
            public override void Bake(TrafficParentWagonAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent<TrafficParentWagonBakingTag>(entity);
                AddComponent<TrainTag>(entity);
                AddComponent<TrafficCustomTargetingTag>(entity);

                AddComponent(entity, new TrainDataComponent()
                {
                    WagonOffset = authoring.wagonOffset
                });

                AddComponent(entity, new TrainComponent()
                {
                    IsParent = true
                });

                var buffer = AddBuffer<TrafficWagonElement>(entity);

                for (int i = 0; i < authoring.wagons.Count; i++)
                {
                    var wagonEntity = GetEntity(authoring.wagons[i].gameObject, TransformUsageFlags.Dynamic);

                    buffer.Add(new TrafficWagonElement()
                    {
                        Entity = wagonEntity
                    });
                }
            }
        }
    }
}