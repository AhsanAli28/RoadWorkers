using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class CullStateObjectsDebugger : MonoBehaviourBase
    {
#if UNITY_EDITOR
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonDebug.html#cullstate-object-debugger")]
        [SerializeField] private string link;

        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private bool showEntityIndex;

        [ShowIf(nameof(enableDebug))]
        [SerializeField][Range(0, 10f)] private float gizmosRadius = 1f;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color culledColor = Color.red;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color inViewOfCameraColor = Color.green;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color preinitCameraColor = Color.yellow;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color closeEnoughColor = Color.blue;

        private EntityManager entityManager;
        private EntityQuery cullEntitiesQuery;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            cullEntitiesQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CullStateComponent>(), ComponentType.ReadOnly<LocalToWorld>());
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            var entities = cullEntitiesQuery.ToEntityArray(Allocator.TempJob);
            var cullStates = cullEntitiesQuery.ToComponentDataArray<CullStateComponent>(Allocator.TempJob);
            var cullStatesPositions = cullEntitiesQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

            for (int i = 0; i < cullStates.Length; i++)
            {
                var color = GetColor(cullStates[i].State);

                Gizmos.color = color;
                Gizmos.DrawSphere(cullStatesPositions[i].Position, gizmosRadius);

                if (showEntityIndex)
                {
                    EditorExtension.DrawWorldString($"{entities[i].Index}", cullStatesPositions[i].Position);
                }
            }

            entities.Dispose();
            cullStates.Dispose();
            cullStatesPositions.Dispose();
        }

        private Color GetColor(CullState cullState)
        {
            switch (cullState)
            {
                case CullState.Culled:
                    {
                        return culledColor;
                    }
                case CullState.InViewOfCamera:
                    {
                        return inViewOfCameraColor;
                    }
                case CullState.PreInitInCamera:
                    {
                        return preinitCameraColor;
                    }
                case CullState.CloseToCamera:
                    {
                        return closeEnoughColor;
                    }
            }

            return default;
        }
#endif
    }
}
