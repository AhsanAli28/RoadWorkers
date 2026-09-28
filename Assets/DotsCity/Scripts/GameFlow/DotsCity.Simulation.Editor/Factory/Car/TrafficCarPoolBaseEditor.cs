#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Traffic
{
    [CustomEditor(typeof(TrafficCarPoolBase), true)]
    public class TrafficCarPoolBaseEditor : Editor
    {
        private ReorderableList reorderableList;
        private TrafficCarPoolBase trafficCarPoolBase;

        private void OnEnable()
        {
            trafficCarPoolBase = target as TrafficCarPoolBase;
            InitList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficSettings"));

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("vehicleDataHolder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficCarPoolPreset"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                InitList();
            }

            reorderableList?.DoLayoutList();

            if (GUILayout.Button("Clear Nulls"))
            {
                trafficCarPoolBase.ClearNulls();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void InitList()
        {
            reorderableList = null;

            var preset = trafficCarPoolBase.CarPoolPreset;

            if (preset)
            {
                var so = new SerializedObject(preset);
                reorderableList = TrafficCarPoolPresetEditor.CreateList(preset, so, trafficCarPoolBase.VehicleDataCollection, preset.HybridPreset, monoPreset: preset.MonoPreset, nested: true);
            }
        }
    }
}
#endif