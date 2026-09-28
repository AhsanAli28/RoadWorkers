using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [InternalBufferCapacity(0)]
    public struct RukhankaHybridBoneAnchorElement : IBufferElementData
    {
        public int LocalBoneIndex;
        public Entity BoneEntity;
    }

    public class RukhankaHybridBoneAnchorAuthoring : MonoBehaviour
    {
        public List<Transform> TargetBones = new List<Transform>();

        class RukhankaBoneAnchorAuthoringBaker : Baker<RukhankaHybridBoneAnchorAuthoring>
        {
            public override void Bake(RukhankaHybridBoneAnchorAuthoring authoring)
            {
                if (authoring.TargetBones.Count == 0)
                    return;

                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<RukhankaHybridBoneAnchorElement>(entity);

                for (int i = 0; i < authoring.TargetBones.Count; i++)
                {
                    var targetBone = authoring.TargetBones[i];

                    if (!targetBone)
                        continue;

                    buffer.Add(new RukhankaHybridBoneAnchorElement()
                    {
                        LocalBoneIndex = i,
                        BoneEntity = GetEntity(targetBone.gameObject, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}