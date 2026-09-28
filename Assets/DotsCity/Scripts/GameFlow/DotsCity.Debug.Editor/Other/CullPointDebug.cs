using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class CullPointDebug : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonDebug.html#cullpoint-debugger")]
        [SerializeField] private string link;

        [SerializeField] private bool enableDebug;

#if UNITY_EDITOR

        private EntityManager entityManager;
        private EntityQuery cullSystemQuery;
        private EntityQuery cullPointGroup;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            cullSystemQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CullSystemConfigReference>());
            cullPointGroup = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<CullPointTag>());
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enableDebug) return;

            var count = cullSystemQuery.CalculateEntityCount();

            if (count == 0) return;

            var cullSystemConfig = cullSystemQuery.GetSingleton<CullSystemConfigReference>().Config.Value;

            if (count == 1)
            {
                var position = cullPointGroup.GetSingleton<LocalToWorld>().Position;

                DrawPoint(cullSystemConfig, position);
            }
            else
            {
                var pointArray = cullPointGroup.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

                for (var i = 0; i < pointArray.Length; i++)
                {
                    DrawPoint(cullSystemConfig, pointArray[i].Position);
                }

                pointArray.Dispose();
            }
        }

        private static void DrawPoint(CullSystemConfig cullSystemConfig, float3 position)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(position, cullSystemConfig.VisibleDistance);

            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(position, cullSystemConfig.MaxDistance);

            if (cullSystemConfig.PreinitDistance > 0)
            {
                Gizmos.color = Color.yellow;

                Gizmos.DrawWireSphere(position, cullSystemConfig.PreinitDistance);
            }
        }
#endif
    }
}