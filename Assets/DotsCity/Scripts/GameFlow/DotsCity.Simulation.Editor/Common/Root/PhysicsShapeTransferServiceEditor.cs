#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    [CustomEditor(typeof(PhysicsShapeTransferService))]
    public class PhysicsShapeTransferServiceEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/structure.html#physicsshape-transfer-service";

        private PhysicsShapeTransferService physicsShapeTransferService;

        private void OnEnable()
        {
            physicsShapeTransferService = target as PhysicsShapeTransferService;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);
            DrawSettings(physicsShapeTransferService, serializedObject);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("physicsObjects"));

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawSettings(PhysicsShapeTransferService physicsShapeTransferService, SerializedObject serializedObject)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customRootSearch"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cleanComponents"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cleanChilds"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchType"));

            EditorGUILayout.BeginVertical("GroupBox");

            var layerCountProp = serializedObject.FindProperty("layerCount");

            EditorGUILayout.PropertyField(layerCountProp);

            var layersData = physicsShapeTransferService.LayersData;

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < layerCountProp.intValue; i++)
            {
                var layerData = layersData[i];

                EditorGUILayout.BeginVertical("Helpbox");

                EditorGUILayout.BeginHorizontal();

                const float layerWidth = 160f;

                if (physicsShapeTransferService.ByTagSearch)
                {
                    layerData.TagIndex = EditorGUILayout.Popup(layerData.TagIndex, InternalEditorUtility.tags, GUILayout.Width(layerWidth));
                }

                if (physicsShapeTransferService.ByLayerSearch)
                {
                    var localIndex = Array.IndexOf(InternalEditorUtility.layers, LayerMask.LayerToName(layerData.Layer));
                    var newLocalIndex = EditorGUILayout.Popup(localIndex, InternalEditorUtility.layers, GUILayout.Width(layerWidth));

                    if (newLocalIndex != localIndex)
                    {
                        layerData.Layer = LayerMask.NameToLayer(InternalEditorUtility.layers[newLocalIndex]);
                    }
                }

                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(30f));

                var labelWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 120f;

                EditorGUILayout.BeginVertical();

                layerData.LegacyColliderPerfType = (PhysicsShapeTransferService.ProccesingType)EditorGUILayout.EnumPopup("Proccesing Type", layerData.LegacyColliderPerfType);
                layerData.PreinitLayer = EditorGUILayout.Toggle("Preinit Layer", layerData.PreinitLayer);

                EditorGUILayout.EndVertical();

                EditorGUIUtility.labelWidth = labelWidth;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorSaver.SetObjectDirty(physicsShapeTransferService);
            }

            EditorGUILayout.EndVertical();

            var newLayerProp = serializedObject.FindProperty("newLayer");
            EditorGUILayout.PropertyField(newLayerProp);

            if (newLayerProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("newLayerValue"));
            }
        }
    }
}
#endif