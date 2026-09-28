#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using UnityEditor;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [CustomEditor(typeof(PlayerSpawnTrafficControlService))]
    public class PlayerSpawnTrafficControlServiceEditor : Editor
    {
        private PlayerSpawnTrafficControlService playerSpawnTrafficControlService;

        private void OnEnable()
        {
            playerSpawnTrafficControlService = target as PlayerSpawnTrafficControlService;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCache(serializedObject);
            DrawSettings(serializedObject, playerSpawnTrafficControlService.VehicleDataCollection);

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawCache(SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty("vehicleDataHolder"));
            EditorGUILayout.PropertyField(so.FindProperty("trackingCameraService"));
            EditorGUILayout.PropertyField(so.FindProperty("entityBindingService"));
            EditorGUILayout.PropertyField(so.FindProperty("playerActorTracker"));
            EditorGUILayout.PropertyField(so.FindProperty("trafficNodeEntitySelectorService"));
        }

        public static void DrawSettings(SerializedObject so, VehicleDataCollection vehicleDataCollection)
        {
            var carModelProp = so.FindProperty("carModel");

            VehicleCollectionExtension.DrawModelOptions(vehicleDataCollection, carModelProp);
            EditorGUILayout.PropertyField(so.FindProperty("spawnPoint"));
        }
    }
}
#endif
