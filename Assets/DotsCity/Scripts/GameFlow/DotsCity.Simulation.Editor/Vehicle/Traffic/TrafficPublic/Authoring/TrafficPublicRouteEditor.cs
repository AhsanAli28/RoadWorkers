#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.Extensions;
using UnityEditor;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [CustomEditor(typeof(TrafficPublicRoute))]
    public class TrafficPublicRouteEditor : TrafficRouteEditor
    {
        private TrafficPublicRoute trafficRoute;

        protected override void OnEnable()
        {
            base.OnEnable();
            trafficRoute = target as TrafficPublicRoute;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPublicRouteSettings();
            DrawDefaultInspector(trafficRoute);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
        }

        protected void DrawPublicRouteSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Route Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vehicleDataCollection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVehicleCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("preferedIntervalDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficPublicType"));
                VehicleCollectionExtension.DrawModelOptions(trafficRoute.VehicleDataCollection, serializedObject.FindProperty("vehicleModel"));
            });
        }
    }
}
#endif