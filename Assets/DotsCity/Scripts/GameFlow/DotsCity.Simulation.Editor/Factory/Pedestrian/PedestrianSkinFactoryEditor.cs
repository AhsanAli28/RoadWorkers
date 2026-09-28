#if UNITY_EDITOR
using Spirit604.Extensions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static Spirit604.DotsCity.Simulation.Factory.Pedestrian.PedestrianSkinFactory;

namespace Spirit604.DotsCity.Simulation.Factory.Pedestrian
{
    [CustomEditor(typeof(PedestrianSkinFactory))]
    public class PedestrianSkinFactoryEditor : Editor
    {
        private const float LabelMargin = 2f;

        private PedestrianSkinFactory pedestrianSkinFactory;
        private ReorderableList prefabList;

        private void OnEnable()
        {
            pedestrianSkinFactory = target as PedestrianSkinFactory;
            pedestrianSkinFactory.OnInspectorEnabled();

            prefabList = new ReorderableList(serializedObject, serializedObject.FindProperty("newPrefabs"), true, true, true, true)
            {
                drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "New Entries");

                    const float dropBoxPadding = 250f;
                    rect.height = GetLabelHeight();
                    rect.x += dropBoxPadding;
                    rect.width -= dropBoxPadding;

                    var objs = InspectorExtension.DrawDropAreaGUI(rect, "Drag & Drop Prefabs");
                    pedestrianSkinFactory.AddObjects(objs);
                },
                headerHeight = GetLabelHeight() + 2f,
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var arrayElement = prefabList.serializedProperty.GetArrayElementAtIndex(index);

                    var r1 = rect;
                    r1.height = GetLabelHeight();

                    EditorGUI.BeginChangeCheck();

                    EditorGUI.PropertyField(r1, arrayElement.FindPropertyRelative("Prefab"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        pedestrianSkinFactory.UpdatePrefabData(index);
                    }

                    r1.y += GetLabelHeight();

                    EditorGUI.BeginChangeCheck();

                    var fileTypeProp = arrayElement.FindPropertyRelative("FileType");
                    EditorGUI.PropertyField(r1, fileTypeProp);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        pedestrianSkinFactory.UpdatePrefabData(index);
                    }

                    var fileType = (PedestrianSkinFactory.SourceFileType)fileTypeProp.enumValueIndex;

                    if (fileType == PedestrianSkinFactory.SourceFileType.FBX)
                    {
                        var prefabData = pedestrianSkinFactory.GetPrefabData(index);

                        if (prefabData != null)
                        {
                            r1.y += GetLabelHeight();

                            GUI.enabled = false;

                            EditorGUI.EnumPopup(r1, "Prefab Creation Type", PedestrianSkinFactory.PrefabCreationType.CreateNew);

                            GUI.enabled = true;

                            for (int i = 0; i < prefabData.skins.Count; i++)
                            {
                                r1.y += GetLabelHeight();
                                EditorGUI.ObjectField(r1, prefabData.skins[i], typeof(SkinnedMeshRenderer), false);
                            }
                        }
                    }
                },
                onAddCallback = (list) =>
                {
                    pedestrianSkinFactory.AddEntry();
                },
                onRemoveCallback = (list) =>
                {
                    pedestrianSkinFactory.RemoveEntry(list.index);
                },
                elementHeightCallback = (index) =>
                {
                    var elementHeight = 0f;

                    var prefabData = pedestrianSkinFactory.GetPrefabData(index);

                    if (prefabData != null)
                    {
                        elementHeight += 2 * GetLabelHeight();

                        if (prefabData.FileType == SourceFileType.FBX)
                        {
                            elementHeight += 1 * GetLabelHeight();
                            elementHeight += prefabData.skins.Count * GetLabelHeight();
                        }
                    }

                    return elementHeight;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Flag for fix layout error
            bool selectPath = false;

            InspectorExtension.DrawDefaultInspectorGroupBlock("", () =>
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabSavePath"));

                if (GUILayout.Button("+", GUILayout.Width(25f)))
                {
                    selectPath = true;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pedestrianSkinFactoryData"));
            });

            if (selectPath)
            {
                selectPath = false;
                pedestrianSkinFactory.SelectSavePath();
            }

            InspectorExtension.DrawDefaultInspectorGroupBlock("New Prefab Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("addPedestrianComponents"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("checkForAddedPrefabs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("allowOverwriteExistPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabCreationType"));

                var templateTypeProp = serializedObject.FindProperty("templateType");
                EditorGUILayout.PropertyField(templateTypeProp);

                var templateType = (TemplateType)templateTypeProp.enumValueIndex;

                switch (templateType)
                {
                    case TemplateType.PrefabName:
                        break;
                    case TemplateType.CustomTemplate:
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("templateName"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("startNameIndex"));
                            break;
                        }
                }

                prefabList.DoLayoutList();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabNames"));

                if (GUILayout.Button("Update Prefab Names"))
                {
                    pedestrianSkinFactory.UpdatePrefabNames();
                }

                GUI.enabled = pedestrianSkinFactory.CanCreatePrefabsn;

                if (GUILayout.Button("Try To Add Prefabs"))
                {
                    pedestrianSkinFactory.TryToAddPrefabs();
                }

                GUI.enabled = true;

            }, serializedObject.FindProperty("showAddNewPrefabSettings"));

            serializedObject.ApplyModifiedProperties();
        }

        private float GetLabelHeight()
        {
            return EditorGUIUtility.singleLineHeight + LabelMargin;
        }
    }
}
#endif