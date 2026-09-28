using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public abstract class TrafficDebugBase : EntityDebuggerBase
    {
        private bool gizmos;

        public TrafficDebugBase(EntityManager entityManager, bool gizmos = true) : base(entityManager)
        {
            this.gizmos = gizmos;
        }

        public override void DrawDebug(Entity entity, float3 position, quaternion rotation)
        {
            var boundsComponent = EntityManager.GetComponentData<BoundsComponent>(entity);
            var bounds = new Bounds(boundsComponent.Center, boundsComponent.Size);
            Color color = GetBoundsColor(entity);

#if UNITY_EDITOR
            if (gizmos)
            {
                UnityMathematicsExtension.DrawGizmosRotatedCube(position, rotation, bounds, color);
            }
            else
            {
                UnityMathematicsExtension.DrawSceneViewRotatedCube(position, rotation, bounds, color);
            }
#endif
        }
    }
}
