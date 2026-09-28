using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core.Authoring
{
    public class PlayerTrackerAuthoring : MonoBehaviour
    {
        class PlayerTrackerAuthoringBaker : Baker<PlayerTrackerAuthoring>
        {
            public override void Bake(PlayerTrackerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, typeof(CopyTransformToGameObject));
                AddComponent(entity, typeof(PlayerTrackerTag));
                AddComponentObject(entity, authoring.transform);
            }
        }
    }
}