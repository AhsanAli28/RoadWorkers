#if UNITY_EDITOR
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [CustomEditor(typeof(VehicleDataCollection))]
    public class VehicleDataCollectionEditor : Editor
    {
        private VehicleDataCollection vehicleCollection;
        private SerializedProperty showCustomDataProp;
        private SerializedProperty showIndexProp;
        private ReorderableList allDataList;

        private void OnEnable()
        {
            vehicleCollection = target as VehicleDataCollection;

            var propHeight = EditorGUIUtility.singleLineHeight + 2;

            showCustomDataProp = serializedObject.FindProperty("showCustomData");
            showIndexProp = serializedObject.FindProperty("showIndex");
            allDataList = new ReorderableList(serializedObject, serializedObject.FindProperty("vehicleDataList"), false, false, false, true);

            allDataList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                if (vehicleCollection.CanShow(index))
                {
                    DrawElementList(allDataList, rect, index, (indexValue, newName) =>
                    {
                        vehicleCollection.UpdateName(index, newName);
                    });
                }
            };

            allDataList.onRemoveCallback = (ReorderableList list) =>
            {
                int i = allDataList.index;
                vehicleCollection.RemoveDataAt(i);
            };

            allDataList.elementHeightCallback = (index) =>
            {
                if (vehicleCollection.CanShow(index))
                {
                    SerializedProperty element = allDataList.serializedProperty.GetArrayElementAtIndex(index);

                    float dataHeight = 0;
                    int fieldCount = 0;

                    if (showCustomDataProp.boolValue)
                    {
                        fieldCount = 1;

                        var settingsTypeProp = element.FindPropertyRelative("SettingsType");
                        var flag = vehicleCollection.VehicleDataList[index].GetSettingsFlag();

                        if (flag.HasFlag(VehicleDataCollection.SettingsType.CustomEngine))
                        {
                            fieldCount += 5;
                        }

                        if (flag.HasFlag(VehicleDataCollection.SettingsType.CustomSound))
                        {
                            var customDataProp = element.FindPropertyRelative("CarSoundData");
                            var carSoundDataHeight = EditorGUI.GetPropertyHeight(customDataProp);
                            dataHeight += carSoundDataHeight + 10;
                        }

                        if (flag.HasFlag(VehicleDataCollection.SettingsType.Overwrite))
                        {
                            fieldCount += 1;
                        }
                    }
                    else
                    {
                        fieldCount = 1;
                    }

                    return fieldCount * propHeight + 2 + dataHeight;
                }

                return 0;
            };
        }

        private void DrawElementList(ReorderableList list, Rect rect, int index, Action<int, string> onChangedName)
        {
            var arrayElement = list.serializedProperty.GetArrayElementAtIndex(index);

            var propHeight = EditorGUIUtility.singleLineHeight + 2;
            var prop = arrayElement.FindPropertyRelative("Name");

            EditorGUI.BeginChangeCheck();

            var r = rect;
            r.height = propHeight;

            const float width = 120;
            const float spacing = 10;

            var previousWidth = r.width;
            var previousPos = r.x;

            if (showCustomDataProp.boolValue)
            {
                r.width = width;
            }

            const float indexWidth = 25f;
            const float indexOffset = 2f;

            var nameR1 = r;
            nameR1.width -= (indexWidth - indexOffset);

            var nameR2 = nameR1;
            nameR2.x += indexWidth + indexOffset;
            nameR1.width = indexWidth;

            EditorGUI.PropertyField(nameR2, prop, GUIContent.none);
            EditorGUI.LabelField(nameR1, $"[{index}]", EditorStyles.boldLabel);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                onChangedName(index, prop.stringValue);
            }

            if (showCustomDataProp.boolValue)
            {
                r.x += width + spacing;
                r.width = previousWidth - width - spacing;

                var settingsTypeProp = arrayElement.FindPropertyRelative("SettingsType");

                EditorGUI.BeginChangeCheck();

                settingsTypeProp.enumValueFlag = (int)(VehicleDataCollection.SettingsType)EditorGUI.EnumFlagsField(r, "Settings Type", (VehicleDataCollection.SettingsType)settingsTypeProp.enumValueFlag);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorSaver.SetObjectDirty(vehicleCollection);
                }

                var flag = vehicleCollection.VehicleDataList[index].GetSettingsFlag();

                if (flag.HasFlag(VehicleDataCollection.SettingsType.CustomEngine))
                {
                    var previousX = r.x;
                    var previousWidth2 = r.width;

                    r.width -= 20f;
                    r.x += 20f;

                    r.y += propHeight;
                    EditorGUI.PropertyField(r, arrayElement.FindPropertyRelative("MinPitch"));

                    r.y += propHeight;
                    EditorGUI.PropertyField(r, arrayElement.FindPropertyRelative("MaxPitch"));

                    r.y += propHeight;
                    EditorGUI.PropertyField(r, arrayElement.FindPropertyRelative("MaxLoadSpeed"));

                    r.y += propHeight;
                    EditorGUI.PropertyField(r, arrayElement.FindPropertyRelative("MaxVolumeSpeed"));

                    r.y += propHeight;
                    EditorGUI.PropertyField(r, arrayElement.FindPropertyRelative("MinVolume"));

                    r.width = previousWidth2;
                    r.x = previousX;
                }

                if (flag.HasFlag(VehicleDataCollection.SettingsType.CustomSound))
                {
                    r.y += propHeight;
                    r.width = previousWidth;
                    r.x = previousPos;

                    var customDataProp = arrayElement.FindPropertyRelative("CarSoundData");
                    var carSoundDataHeight = EditorGUI.GetPropertyHeight(customDataProp);

                    r.height = carSoundDataHeight;

                    EditorGUI.PropertyField(r, customDataProp);

                    r.y += carSoundDataHeight;

                    InspectorExtension.DrawInspectorLine(r, Color.gray);
                }

                if (flag.HasFlag(VehicleDataCollection.SettingsType.Overwrite))
                {
                    r.y += propHeight;

                    if (vehicleCollection.Options?.Length > 0)
                    {
                        var sourceVehicleProp = arrayElement.FindPropertyRelative("SourceVehicleID");

                        int modelIndex = 0;

                        if (!string.IsNullOrEmpty(sourceVehicleProp.stringValue))
                        {
                            modelIndex = vehicleCollection.VehicleDataKeys.IndexOf(sourceVehicleProp.stringValue);

                            if (modelIndex == -1)
                            {
                                modelIndex = 0;
                            }
                        }

                        var newIndex = EditorGUI.Popup(r, "Source Model", modelIndex, vehicleCollection.Options);
                        var newModel = vehicleCollection.VehicleDataKeys[newIndex];

                        if (sourceVehicleProp.stringValue != newModel)
                        {
                            sourceVehicleProp.stringValue = newModel;
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUI.Popup(r, "Source Model", 0, new string[1] { "No models found" });
                        GUI.enabled = true;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Collection Settings", () =>
            {
                EditorGUILayout.PropertyField(showCustomDataProp);

                if (showCustomDataProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showType"));

                    switch (vehicleCollection.CurrentShowType)
                    {
                        case VehicleDataCollection.ShowType.ByIndex:
                            {
                                var newVal = EditorGUILayout.IntSlider(showIndexProp.intValue, -1, vehicleCollection.Options.Length - 1);

                                if (showIndexProp.intValue != newVal)
                                {
                                    showIndexProp.intValue = newVal;
                                }

                                break;
                            }
                        case VehicleDataCollection.ShowType.Toolbar:
                            {
                                if (showIndexProp.intValue < 0)
                                {
                                    showIndexProp.intValue = 0;
                                }

                                var newVal = GUILayout.SelectionGrid(showIndexProp.intValue, vehicleCollection.Options, 4);

                                if (showIndexProp.intValue != newVal)
                                {
                                    showIndexProp.intValue = newVal;
                                }

                                break;
                            }
                        case VehicleDataCollection.ShowType.ByName:
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(serializedObject.FindProperty("searchName"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    serializedObject.ApplyModifiedProperties();
                                    vehicleCollection.UpdateSearchIndexes();
                                }
                                break;
                            }
                    }
                }

            }, serializedObject.FindProperty("showCommonSettings"));

            InspectorExtension.DrawGroupBox("Shared Sound Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minPitch"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPitch"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLoadSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxVolumeSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minVolume"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sharedCarSoundData"));

            }, serializedObject.FindProperty("showSoundData"));

            InspectorExtension.DrawGroupBox("Collection", () =>
            {
                allDataList.DoLayoutList();
            }, serializedObject.FindProperty("showVehicleData"));

            if (GUILayout.Button("Clear"))
            {
                vehicleCollection.ClearData();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif