#if UNITY_EDITOR
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNoTargetDebugger : ITrafficDebugger, ICustomTrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;
        private EntityQuery query;

        public TrafficNoTargetDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            this.query = entityManager.CreateEntityQuery(typeof(TrafficNoTargetTag));
        }

        public void DrawInspector()
        {
            if (query.CalculateEntityCount() > 0)
            {
                EditorGUILayout.LabelField($"No target vehicles:", EditorStyles.boldLabel);

                var entities = query.ToEntityArray(Allocator.TempJob);

                for (int i = 0; i < entities.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"TrafficCar {entities[i].Index} ");

                    if (GUILayout.Button("Focus"))
                    {
                        TrafficDebuggerUtils.FocusEntity(ref entityManager, entities[i]);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                entities.Dispose();
            }
            else
            {
                EditorGUILayout.LabelField($"All vehicles have target", EditorStyles.boldLabel);
            }
        }

        public void DrawSceneView(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficNoTargetTag)))
            {
                var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
                var boundsComponent = entityManager.GetComponentData<BoundsComponent>(entity);
                UnityMathematicsExtension.DrawSceneViewRotatedCube(localTransform.Position, localTransform.Rotation, boundsComponent.Bounds, Color.magenta);
            }
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficNoTargetTag)))
            {
                return string.Empty;
            }

            return null;
        }
    }
}
#endif