#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Traffic
{
    [CustomEditor(typeof(TrafficCarPoolPreset), true)]
    public class TrafficCarPoolPresetEditor : SharedSettingsEditorBase<TrafficCarPoolPresetEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool ShowPreview;
        }

        private ReorderableList reorderableList;
        private TrafficCarPoolPreset trafficCarPoolPreset;
        private VehicleDataCollection vehicleDataCollection;

        protected override string SaveKey => "TrafficCarPoolPresetSettings";

        protected override void OnEnable()
        {
            base.OnEnable();

            var vehicleDataHolder = ObjectUtils.FindObjectOfType<VehicleDataHolder>();

            if (vehicleDataHolder)
            {
                vehicleDataCollection = vehicleDataHolder.VehicleDataCollection;
            }

            trafficCarPoolPreset = target as TrafficCarPoolPreset;
            CreateList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var showPreview = EditorGUILayout.Toggle("Show Preview", SharedSettings.ShowPreview);

            if (SharedSettings.ShowPreview != showPreview)
            {
                SharedSettings.ShowPreview = showPreview;
                CreateList();
            }

            EditorGUILayout.EnumPopup(StringExtension.CamelToLabel(nameof(trafficCarPoolPreset.TrafficEntityType)), trafficCarPoolPreset.TrafficEntityType);

            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        public static ReorderableList CreateList(TrafficCarPoolPreset preset, SerializedObject serializedObject, VehicleDataCollection vehicleDataCollection = null, bool hybridPreset = true, bool showPreview = false, bool monoPreset = false, bool nested = false)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight + 3;
            float fieldHeight = lineHeight - 2;

            var reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("prefabDatas"), true, false, true, true);
            const float previewSize = 100f;

            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var arrayElement = reorderableList.serializedProperty.GetArrayElementAtIndex(index);

                var r1 = rect;
                r1.width /= 2;
                r1.height = fieldHeight;

                var width = r1.width;

                r1.width -= 10f;

                var hullPrefabProp = arrayElement.FindPropertyRelative("HullPrefab");
                var entityPrefabProp = arrayElement.FindPropertyRelative("EntityPrefab");

                var id = VehicleCollectionExtension.GetVehicleID(entityPrefabProp.objectReferenceValue);

                var r2 = r1;

                GUI.enabled = false;

                EditorGUI.TextField(r1, id);

                GUI.enabled = true;

                if (!string.IsNullOrEmpty(id) && entityPrefabProp.objectReferenceValue != null)
                {
                    if (vehicleDataCollection)
                    {
                        var keyIndex = vehicleDataCollection.GetCarModelIndexByID(id);

                        if (keyIndex == -1)
                        {
                            r2.x += 70f;
                            r2.width = 60f;

                            EditorGUI.HelpBox(r2, "Missing", MessageType.Error);

                            r2.x = EditorGUIUtility.currentViewWidth * 0.45f;
                            r2.width = 40;

                            if (GUI.Button(r2, "Add"))
                            {
                                vehicleDataCollection.AddData(id, id);
                            }
                        }
                    }
                }

                r1.x += width + 10f;

                if (showPreview)
                {
                    UnityEngine.Object previewRef = !monoPreset ? entityPrefabProp.objectReferenceValue : hullPrefabProp.objectReferenceValue;

                    if (previewRef != null)
                    {
                        var previewTexture = AssetPreview.GetAssetPreview(previewRef);

                        if (previewTexture != null)
                        {
                            var source = r1;

                            r1.width = previewSize;
                            r1.height = previewSize;

                            EditorGUI.DrawPreviewTexture(r1, previewTexture);

                            r1.x = source.x;
                            r1.width = source.width;
                            r1.height = source.height;

                            r1.y += previewSize;
                        }
                    }
                }

                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f;

                if (hybridPreset)
                {
                    EditorGUI.PropertyField(r1, hullPrefabProp);

                    r1.y += lineHeight;
                }

                EditorGUI.PropertyField(r1, entityPrefabProp);

                r1.y += lineHeight;

                EditorGUI.PropertyField(r1, arrayElement.FindPropertyRelative("Weight"));

                EditorGUIUtility.labelWidth = labelWidth;
            };

            reorderableList.onRemoveCallback = (list) =>
            {
                if (nested)
                    preset.RemoveEntry(reorderableList.index);

                reorderableList.serializedProperty.DeleteArrayElementAtIndex(reorderableList.index);
            };

            reorderableList.elementHeightCallback += (index) =>
        {
            int fieldCount = 2;
            float size = 0;

            if (hybridPreset)
            {
                fieldCount++;
            }

            if (showPreview)
            {
                size += previewSize;
            }

            return lineHeight * fieldCount + size + 3f;
        };

            return reorderableList;
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }

        private void CreateList()
        {
            reorderableList = CreateList(trafficCarPoolPreset, serializedObject, vehicleDataCollection: vehicleDataCollection, hybridPreset: trafficCarPoolPreset.HybridPreset, showPreview: SharedSettings.ShowPreview, monoPreset: trafficCarPoolPreset.MonoPreset);
        }
    }
}
#endif