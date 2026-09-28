using Spirit604.Attributes;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class NpcGroundConfigAuthoring : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/npc.html#npc-ground-config")]
        [SerializeField] private string link;

        [Tooltip("Raycast distance")]
        [SerializeField][Range(0, 10f)] private float castDistance = 6f;

        [Tooltip("Distance from the surface where the landing animation starts")]
        [SerializeField][Range(0, 20f)] private float stopFallingDistance = 6f;

        [Tooltip("Min distance from the surface where the falling state starts")]
        [SerializeField][Range(0, 10f)] private float fallingDistance = 2f;

        [Tooltip("Distance from the surface for ground state")]
        [SerializeField][Range(0, 1f)] private float groundedDistance = 0.1f;

        class NpcGroundConfigAuthoringBaker : Baker<NpcGroundConfigAuthoring>
        {
            public override void Bake(NpcGroundConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<NpcGroundConfig>();

                    root.CastDistance = authoring.castDistance;
                    root.StopFallingDistance = authoring.stopFallingDistance;
                    root.FallingDistance = authoring.fallingDistance;
                    root.GroundedDistance = authoring.groundedDistance;

                    var blobRef = builder.CreateBlobAssetReference<NpcGroundConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new NpcGroundConfigReference() { Config = blobRef });
                }
            }
        }
    }
}
