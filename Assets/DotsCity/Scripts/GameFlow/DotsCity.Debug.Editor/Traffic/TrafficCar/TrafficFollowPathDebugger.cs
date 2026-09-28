#if UNITY_EDITOR
using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Extensions;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficFollowPathDebugger : ITrafficDebugger, ICustomTrafficDebugger
    {
        private const float LineWidth = 2f;

        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;
        private EntityQuery query;
        private Entity selectedEntity = Entity.Null;

        private CityEditorSettings Settings => CityEditorSettings.GetOrCreateSettings();

        public TrafficFollowPathDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            this.query = entityManager.CreateEntityQuery(typeof(TrafficFixedRouteComponent));
        }

        public void DrawInspector()
        {
            EditorGUI.BeginChangeCheck();

            Settings.FollowPathColor = EditorGUILayout.ColorField("Path Color", Settings.FollowPathColor);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.MakeDirty();
            }

            if (query.CalculateEntityCount() > 0)
            {
                EditorGUILayout.LabelField($"Custom follow path vehicles:", EditorStyles.boldLabel);

                var entities = query.ToEntityArray(Allocator.TempJob);

                for (int i = 0; i < entities.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"TrafficCar {entities[i].Index} ");

                    if (GUILayout.Button("Focus"))
                    {
                        selectedEntity = entities[i];
                        TrafficDebuggerUtils.FocusEntity(ref entityManager, entities[i]);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (selectedEntity != Entity.Null)
                {
                    if (GUILayout.Button("Unselect"))
                    {
                        selectedEntity = Entity.Null;
                    }
                }

                entities.Dispose();
            }
            else
            {
                EditorGUILayout.LabelField($"No vehicles with follow path", EditorStyles.boldLabel);
            }
        }

        public void DrawSceneView(Entity entity)
        {
            if (!entityManager.HasComponent<TrafficFixedRouteComponent>(entity) || (entity != selectedEntity && selectedEntity != Entity.Null))
                return;

            var routeData = entityManager.GetComponentData<TrafficFixedRouteComponent>(entity);

            if (routeData.RouteEntity == Entity.Null)
                return;

            var route = entityManager.GetBuffer<FixedRouteNodeElement>(routeData.RouteEntity, true);

            for (int i = 0; i < route.Length - 1; i++)
            {
                var a1 = route[i].Position;
                var a2 = route[i + 1].Position;

                DebugLine.DrawThickLine(a1, a2, LineWidth, Settings.FollowPathColor, false);
            }
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficFixedRouteComponent)))
            {
                return string.Empty;
            }

            return null;
        }
    }
}
#endif