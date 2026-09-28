using Spirit604.AnimationBaker.EditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static Spirit604.AnimationBaker.CrowdSkinFactory;

namespace Spirit604.AnimationBaker
{
    [CustomEditor(typeof(CrowdSkinFactory), true)]
    public class CrowdSkinFactoryEditor : Editor
    {
        private const float LabelMargin = 1f;
        private const int FactoryScrollView = 8;
        private const float FactoryScrollViewHeight = 400f;

        private const int NewEntriesScrollView = 10;
        private const float EntriesScrollViewHeight = 200f;

        private const string LODsProp = "LODs";

        private CrowdSkinFactory crowdSkinFactory;
        private ReorderableList reorderableList;

        private SerializedProperty settingFoldoutProp;
        private SerializedProperty minAnimationTextMatchRateProp;
        private SerializedProperty createMaterialPathProp;
        private SerializedProperty createdMaterialNameProp;
        private SerializedProperty materialTemplateTypeProp;
        private SerializedProperty addTemplateNameProp;
        private SerializedProperty animationMaterialBaseProp;
        private SerializedProperty defaultAtlasTextureProp;
        private SerializedProperty entryKeySourceTypeProp;
        private SerializedProperty showFrameInfoOnSelectOnlyProp;
        private SerializedProperty autoBindOnGenerationProp;

        private SerializedProperty showOptionalAnimationPopupProp;
        private SerializedProperty animationCollectionContainerProp;
        private SerializedProperty characterAnimationContainerProp;
        private SerializedObject animationContainerSO;
        private SerializedProperty keysProp;
        private SerializedProperty valuesProp;
        private SerializedProperty selectedIndexProp;
        private SerializedProperty displayAddEntryTabProp;
        private SerializedProperty findAnimationsProp;
        private GUIStyle defaultLabelStyle;
        private GUIStyle bindLabelStyle;
        private GUIStyle selectedLabelStyle;
        private string[] animNames;
        private Vector2 factoryScrollViewPosition;
        private Vector2 entryScrollViewPosition;

        private List<AnimationCollectionContainer.AnimationData> optionalAnimations;
        private string[] optionalHeaders;
        private Dictionary<int, string> indexToGuid = new Dictionary<int, string>();
        private int newAnimIndex;

        protected virtual void OnEnable()
        {
            Init();
            crowdSkinFactory.OnInspectorEnabled();
        }

        protected virtual void OnDisable()
        {
            crowdSkinFactory.OnInspectorDisabled();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            GUILayout.BeginVertical("HelpBox");

            var showSettings = EditorGUILayout.Foldout(settingFoldoutProp.boolValue, "Settings");

            if (settingFoldoutProp.boolValue != showSettings)
            {
                settingFoldoutProp.boolValue = showSettings;
            }

            bool showSettingsDialogWindow = false;

            if (showSettings)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical("GroupBox");

                EditorGUILayout.PropertyField(minAnimationTextMatchRateProp);

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(createMaterialPathProp);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    showSettingsDialogWindow = true;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(createdMaterialNameProp);
                EditorGUILayout.PropertyField(materialTemplateTypeProp);

                var materialTemplateType = (CrowdSkinFactory.MaterialTemplateType)materialTemplateTypeProp.enumValueIndex;

                switch (materialTemplateType)
                {
                    case CrowdSkinFactory.MaterialTemplateType.EntryName:
                        EditorGUILayout.PropertyField(addTemplateNameProp);
                        break;
                    case CrowdSkinFactory.MaterialTemplateType.MeshName:
                        EditorGUILayout.PropertyField(addTemplateNameProp);
                        break;
                }

                EditorGUILayout.PropertyField(animationMaterialBaseProp);
                EditorGUILayout.PropertyField(defaultAtlasTextureProp);
                EditorGUILayout.PropertyField(entryKeySourceTypeProp);

                EditorGUILayout.PropertyField(showFrameInfoOnSelectOnlyProp);
                EditorGUILayout.PropertyField(autoBindOnGenerationProp);
                EditorGUILayout.PropertyField(showOptionalAnimationPopupProp);

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            if (showSettingsDialogWindow)
            {
                var newPath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select generated material path", createMaterialPathProp.stringValue);
                crowdSkinFactory.SetMaterialGenerationPath(newPath);
            }

            bool drawList = true;
            GUILayout.BeginVertical("HelpBox");

            EditorGUILayout.PropertyField(animationCollectionContainerProp);

            if (!crowdSkinFactory.HasAnimationCollection)
            {
                EditorGUILayout.HelpBox("Create & assign animation collection!", MessageType.Error);
                drawList = false;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(characterAnimationContainerProp);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                InitAnimationContainer(true);
            }

            if (characterAnimationContainerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Create & assign animation data container!", MessageType.Error);
                drawList = false;
            }

            GUILayout.EndVertical();

            if (!drawList)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (reorderableList == null)
            {
                Init();
                return;
            }

            if (reorderableList.count > FactoryScrollView)
            {
                factoryScrollViewPosition = EditorGUILayout.BeginScrollView(factoryScrollViewPosition, GUILayout.Height(FactoryScrollViewHeight));
            }

            reorderableList.DoLayoutList();

            if (reorderableList.count > FactoryScrollView)
            {
                EditorGUILayout.EndScrollView();
            }

            if (displayAddEntryTabProp.boolValue)
            {
                InspectorExtension.DrawDefaultInspectorGroupBlock("New Entry Settings", () =>
                {
                    var sourceKeyType = (EntryKeySourceType)entryKeySourceTypeProp.enumValueIndex;
                    var meshProp = serializedObject.FindProperty("mesh");

                    switch (sourceKeyType)
                    {
                        case EntryKeySourceType.Custom:
                            {
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("newKey"));
                                break;
                            }
                        case EntryKeySourceType.SelectedMeshName:
                            {
                                var key = crowdSkinFactory.GetEntryKey();

                                EditorGUILayout.TextField("New Key", key);

                                break;
                            }
                    }

                    EditorGUILayout.PropertyField(meshProp);

                    var availableMeshes = crowdSkinFactory.AvailableContainerMeshes;

                    if (meshProp.objectReferenceValue != null)
                    {
                        if (availableMeshes.Count == 0)
                        {
                            meshProp.objectReferenceValue = null;
                        }
                    }
                    else
                    {
                        if (availableMeshes.Count == 1)
                        {
                            meshProp.objectReferenceValue = availableMeshes[0].Mesh;
                        }
                        else
                        {
                            if (availableMeshes.Count > NewEntriesScrollView)
                            {
                                entryScrollViewPosition = EditorGUILayout.BeginScrollView(entryScrollViewPosition, GUILayout.Height(200f));
                            }

                            for (int i = 0; i < availableMeshes.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();

                                EditorGUILayout.ObjectField("Source Mesh", availableMeshes[i].Mesh, typeof(Mesh), false);

                                var lastRect = GUILayoutUtility.GetLastRect();

                                const float selectButtonWidth = 60f;

                                lastRect.x += EditorGUIUtility.labelWidth - selectButtonWidth;
                                lastRect.width = selectButtonWidth;

                                if (GUI.Button(lastRect, "Select"))
                                {
                                    meshProp.objectReferenceValue = availableMeshes[i].Mesh;
                                    crowdSkinFactory.UpdateKey();
                                }

                                EditorGUILayout.EndHorizontal();
                            }

                            if (availableMeshes.Count > NewEntriesScrollView)
                            {
                                EditorGUILayout.EndScrollView();
                            }

                            if (GUILayout.Button("Add All Entries"))
                            {
                                crowdSkinFactory.AddEntries();
                                InitAnimationContainerIfRequired();
                            }
                        }
                    }

                    GUI.enabled = (crowdSkinFactory.ReadyToCreate);

                    if (GUILayout.Button("Add Entry"))
                    {
                        crowdSkinFactory.AddKey();
                        InitAnimationContainerIfRequired();
                    }

                    GUI.enabled = true;
                });

                var rect = GUILayoutUtility.GetLastRect();

                rect.y += 5;
                rect.x += rect.width - 30;

                rect.width = 20f;
                rect.height = 20f;

                if (GUI.Button(rect, "x"))
                {
                    displayAddEntryTabProp.boolValue = false;
                }
            }

            Action animationCallback = () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("animationTextureDataContainer"));

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    crowdSkinFactory.OnTextureDataChanged();

                    if (findAnimationsProp.boolValue)
                    {
                        crowdSkinFactory.FindAnimations();
                    }
                }

                if (crowdSkinFactory.HasSelectedIndex && keysProp.arraySize > selectedIndexProp.intValue)
                {
                    var selectedKey = keysProp.GetArrayElementAtIndex(selectedIndexProp.intValue).stringValue;
                    EditorGUILayout.LabelField($"Selected Entry: [{selectedKey}]", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"No entry selected", EditorStyles.boldLabel);
                }

                Material material = null;
                bool materialIsMatched = false;

                if (crowdSkinFactory.SourceDataExist && crowdSkinFactory.HasSelectedIndex)
                {
                    crowdSkinFactory.GetSkinData(selectedIndexProp.intValue, out var mesh, out material);

                    if (material)
                    {
                        var materialTexture = material.GetTexture(Constans.AnimationTexture);

                        var container = crowdSkinFactory.GetContainerBy(container => container.GetTexture(0) == materialTexture);

                        materialIsMatched = container != null;

                        if (!materialIsMatched)
                        {
                            EditorGUILayout.HelpBox("Texture from AnimationSheetData not matched with selected entry material", MessageType.Warning);
                        }
                    }
                }

                var selectedAnimationIndexProp = serializedObject.FindProperty("selectedAnimationIndex");
                var newselectedAnimationIndex = EditorGUILayout.Popup(selectedAnimationIndexProp.intValue, animNames);

                if (newselectedAnimationIndex != selectedAnimationIndexProp.intValue)
                {
                    selectedAnimationIndexProp.intValue = newselectedAnimationIndex;
                }

                var findAnimations = GUILayout.Toggle(findAnimationsProp.boolValue, "Find Related Animations", "Button");

                if (findAnimationsProp.boolValue != findAnimations)
                {
                    findAnimationsProp.boolValue = findAnimations;
                    serializedObject.ApplyModifiedProperties();

                    if (findAnimations)
                    {
                        crowdSkinFactory.FindAnimations();
                    }
                }

                if (crowdSkinFactory.CanFindAnimation)
                {
                    var relatedAnimations = crowdSkinFactory.RelatedAnimations;

                    if (relatedAnimations.Count > 0)
                    {
                        GUI.enabled = materialIsMatched;

                        if (GUILayout.Button("Auto Bind Animations"))
                        {
                            crowdSkinFactory.AutoBindAnimation();
                        }

                        GUI.enabled = true;
                    }

                    for (int i = 0; i < relatedAnimations.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField($"{relatedAnimations[i].AnimationName}", GUILayout.Width(EntriesScrollViewHeight));
                        EditorGUILayout.LabelField($"Frame offset [{relatedAnimations[i].TextureOffset.x}, {relatedAnimations[i].TextureOffset.y}]", GUILayout.MinWidth(140f), GUILayout.MaxWidth(220f));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"FrameCount {relatedAnimations[i].FrameCount}", GUILayout.MinWidth(70f), GUILayout.MaxWidth(100f));

                        if (GUILayout.Button("Assign"))
                        {
                            crowdSkinFactory.AssignAnimationData(i);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Animation Assign Settings", animationCallback);

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void Init()
        {
            crowdSkinFactory = target as CrowdSkinFactory;

            settingFoldoutProp = serializedObject.FindProperty("settingFoldout");
            minAnimationTextMatchRateProp = serializedObject.FindProperty("minAnimationTextMatchRate");
            createMaterialPathProp = serializedObject.FindProperty("createMaterialPath");
            createdMaterialNameProp = serializedObject.FindProperty("createdMaterialName");
            materialTemplateTypeProp = serializedObject.FindProperty("materialTemplateType");
            addTemplateNameProp = serializedObject.FindProperty("addTemplateName");
            animationMaterialBaseProp = serializedObject.FindProperty("animationMaterialBase");
            defaultAtlasTextureProp = serializedObject.FindProperty("defaultAtlasTexture");
            entryKeySourceTypeProp = serializedObject.FindProperty("entryKeySourceType");
            showFrameInfoOnSelectOnlyProp = serializedObject.FindProperty("showFrameInfoOnSelectOnly");
            autoBindOnGenerationProp = serializedObject.FindProperty("autoBindOnGeneration");
            showOptionalAnimationPopupProp = serializedObject.FindProperty("showOptionalAnimationPopup");

            animationCollectionContainerProp = serializedObject.FindProperty("animationCollectionContainer");

            selectedIndexProp = serializedObject.FindProperty("selectedSkinIndex");
            displayAddEntryTabProp = serializedObject.FindProperty("displayAddTab");
            findAnimationsProp = serializedObject.FindProperty("findAnimations");

            var animList = crowdSkinFactory.AnimationNameList;

            if (animList != null)
            {
                animNames = animList.ToArray();
            }
            else
            {
                animNames = new string[0];
            }

            InitAnimationContainer();
        }

        private void InitAnimationContainerIfRequired()
        {
            if (keysProp.arraySize <= 1)
            {
                InitAnimationContainer();
                Repaint();
            }
        }

        private void InitAnimationContainer(bool validate = false)
        {
            characterAnimationContainerProp = serializedObject.FindProperty("characterAnimationContainer");

            if (characterAnimationContainerProp.objectReferenceValue == null)
            {
                return;
            }

            var container = characterAnimationContainerProp.objectReferenceValue as CharacterAnimationContainer;
            animationContainerSO = new SerializedObject(container);
            keysProp = animationContainerSO.FindProperty("keys");
            valuesProp = animationContainerSO.FindProperty("values");

            defaultLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            bindLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            selectedLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            selectedLabelStyle.normal.textColor = Color.green;

            if (validate)
            {
                crowdSkinFactory.ValidateAnimations();
            }

            InitAnimations();
            InitList(container);
        }

        private void InitAnimations()
        {
            indexToGuid.Clear();
            optionalAnimations = crowdSkinFactory.AnimationCollectionContainer.GetAnimations(AnimationUseType.Optional);

            optionalHeaders = new string[optionalAnimations.Count + 1];
            optionalHeaders[0] = "None";

            for (int i = 0; i < optionalAnimations.Count; i++)
            {
                optionalHeaders[i + 1] = optionalAnimations[i].Name;

                indexToGuid.Add(i, optionalAnimations[i].Guid);
            }
        }

        private void InitList(CharacterAnimationContainer characterContainer)
        {
            reorderableList = new ReorderableList(animationContainerSO, keysProp, true, false, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var keyProp = keysProp.GetArrayElementAtIndex(index);
                    var valueArrayElement = valuesProp.GetArrayElementAtIndex(index);

                    DrawEntry(rect, index, characterContainer, keyProp, valueArrayElement);
                },
                onSelectCallback = (list) =>
                {
                    var previousIndex = selectedIndexProp.intValue;
                    selectedIndexProp.intValue = list.index;
                    serializedObject.ApplyModifiedProperties();
                    crowdSkinFactory.OnSkinIndexSelectedChanged(previousIndex, selectedIndexProp.intValue);
                },
                onRemoveCallback = (list) =>
                {
                    var removedIndex = list.index;
                    keysProp.DeleteArrayElementAtIndex(removedIndex);
                    valuesProp.DeleteArrayElementAtIndex(removedIndex);
                    animationContainerSO.ApplyModifiedProperties();
                },
                onAddCallback = (list) =>
                {
                    displayAddEntryTabProp.boolValue = !displayAddEntryTabProp.boolValue;
                    serializedObject.ApplyModifiedProperties();
                },
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    crowdSkinFactory.CharacterAnimationContainer.MoveEntry(oldIndex, newIndex);
                },
                elementHeightCallback = (index) =>
                {
                    var animationCount = crowdSkinFactory.GetAnimationCount(index);

                    if (showFrameInfoOnSelectOnlyProp.boolValue && !crowdSkinFactory.SkinIsSelected(index))
                    {
                        animationCount = 0;
                    }

                    int defaultSettingsCount = 3;

                    if (crowdSkinFactory.CharacterAnimationContainer)
                    {
                        var container = crowdSkinFactory.CharacterAnimationContainer;

                        var skinData = container.GetSkinData(index);

                        if (skinData != null && !skinData.GetMaterial(crowdSkinFactory.SelectedLodLevel))
                        {
                            defaultSettingsCount += 2;
                        }
                    }

                    if (showOptionalAnimationPopupProp.boolValue)
                    {
                        defaultSettingsCount += 1;
                    }

                    float height = (animationCount + defaultSettingsCount) * GetFieldOffset();

                    return height;
                },
            };
        }

        private void DrawEntry(Rect rect, int index, CharacterAnimationContainer characterContainer, SerializedProperty keyProp, SerializedProperty valueArrayElement)
        {
            Mesh mesh = null;
            var skinData = characterContainer.GetSkinData(index);

            if (skinData != null)
                mesh = skinData.GetMesh();

            var animationTextureDataContainer = crowdSkinFactory.GetContainerBy(container => container.ContainsMesh(mesh));

            animationContainerSO.Update();
            var propertyChanged = false;

            var r1 = rect;
            var r2 = rect;
            r1.width = r1.width * 1 / 4f - 10f;
            r2.width = r2.width * 3 / 4f;
            r2.x += r1.width + 5f;

            r1.height = GetFieldSize();
            r2.height = GetFieldSize();

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(r1, keyProp, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                propertyChanged = true;
            }

            EditorGUI.BeginChangeCheck();

            var skinProp = valueArrayElement.FindPropertyRelative(LODsProp).GetArrayElementAtIndex(crowdSkinFactory.SelectedLodLevel);

            EditorGUI.PropertyField(r2, skinProp.FindPropertyRelative("Mesh"), GUIContent.none);

            r2.y += GetFieldOffset();

            var materialProp = skinProp.FindPropertyRelative("Material");
            EditorGUI.PropertyField(r2, materialProp, GUIContent.none);

            if (materialProp.objectReferenceValue == null && characterContainer)
            {
                var sourceLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 130f;

                var generateButtonRect = r2;

                const float floatGenerateButtonWidth = 70f;

                generateButtonRect.width = floatGenerateButtonWidth;
                generateButtonRect.x -= floatGenerateButtonWidth + 5f;

                r2.y += GetFieldOffset();

                var sourceMainTexture = characterContainer.GetMainTexture(index);
                var newMainTexture = EditorGUI.ObjectField(r2, "Main Texture", sourceMainTexture, typeof(Texture2D), false) as Texture2D;

                if (sourceMainTexture != newMainTexture)
                {
                    characterContainer.SetMainTexture(index, newMainTexture);
                }

                r2.y += GetFieldOffset();

                Texture2D animationTexture = null;
                Texture2D normalTexture = null;

                if (animationTextureDataContainer)
                {
                    animationTexture = animationTextureDataContainer.GetTexture(0);
                    normalTexture = animationTextureDataContainer.GetTexture(1);
                }

                GUI.enabled = false;

                EditorGUI.ObjectField(r2, "Animation Texture", animationTexture, typeof(Texture2D), false);

                GUI.enabled = true;

                bool containsTexture = crowdSkinFactory.CurrentSheetContainsMesh(index);

                if (animationTexture && !containsTexture)
                {
                    var helpBoxRect = r2;
                    helpBoxRect.width = 20f;
                    helpBoxRect.x -= 25f;

                    EditorGUI.HelpBox(helpBoxRect, "", MessageType.Error);
                }

                GUI.enabled = newMainTexture && animationTexture && containsTexture;

                if (GUI.Button(generateButtonRect, "Generate"))
                {
                    var currentIndex = index;
                    crowdSkinFactory.GenerateMaterial(currentIndex, newMainTexture, animationTexture, normalTexture);
                }

                GUI.enabled = true;

                EditorGUIUtility.labelWidth = sourceLabelWidth;
            }

            if (crowdSkinFactory.HasRagdoll)
            {
                r2.y += GetFieldOffset();

                EditorGUI.PropertyField(r2, valueArrayElement.FindPropertyRelative("Ragdoll"), GUIContent.none);
            }

            var animationsProp = skinProp.FindPropertyRelative("Animations");

            if (EditorGUI.EndChangeCheck())
            {
                propertyChanged = true;
            }

            var r3 = r2;
            var r4 = r2;
            r3.y += GetFieldOffset();
            r4.y += GetFieldOffset();

            const float labelOffset = 80f;

            r3.width = labelOffset;
            r4.width -= labelOffset;
            r4.x += labelOffset;

            if (!showFrameInfoOnSelectOnlyProp.boolValue || crowdSkinFactory.SkinIsSelected(index))
            {
                var r9 = r3;
                float maxWidth = 0;

                for (int j = 0; j < animationsProp.arraySize; j++)
                {
                    var animationProp = animationsProp.GetArrayElementAtIndex(j);

                    var clipName = animationProp.FindPropertyRelative("ClipName").stringValue;
                    float width = EditorStyles.label.CalcSize(new GUIContent(clipName)).x;

                    if (width > maxWidth) maxWidth = width;
                }

                for (int j = 0; j < animationsProp.arraySize; j++)
                {
                    var animationProp = animationsProp.GetArrayElementAtIndex(j);

                    var animationData = skinData.GetAnimationData(j, crowdSkinFactory.SelectedLodLevel);
                    var containerAnimationData = crowdSkinFactory.GetContainerAnimation(animationData.Guid);

                    if (GUI.Button(r3, string.Empty, GUIStyle.none))
                    {
                        crowdSkinFactory.SelectAnimation(index, j);
                    }

                    var animNameText = string.Empty;
                    var animSelected = false;
                    var isNan = false;

                    if (containerAnimationData != null)
                    {
                        animNameText = $"[{containerAnimationData.Name}]";
                        animSelected = crowdSkinFactory.AnimationIsSelected(index, j);
                    }
                    else
                    {
                        isNan = true;
                        animNameText = $"[{animationData.ClipName}]";
                    }

                    GUIStyle currentLabelStyle = null;

                    if (animSelected)
                    {
                        currentLabelStyle = selectedLabelStyle;
                    }
                    else
                    {
                        var bindingState = crowdSkinFactory.GetBindingState(j);

                        switch (bindingState)
                        {
                            case CrowdSkinFactory.AnimBindingState.Success:
                                currentLabelStyle = bindLabelStyle;
                                currentLabelStyle.normal.textColor = Color.yellow;
                                break;
                            case CrowdSkinFactory.AnimBindingState.PartialSuccess:
                                currentLabelStyle = bindLabelStyle;
                                currentLabelStyle.normal.textColor = Color.blue;
                                break;
                            case CrowdSkinFactory.AnimBindingState.Failed:
                                currentLabelStyle = bindLabelStyle;
                                currentLabelStyle.normal.textColor = Color.red;
                                break;
                            default:
                                currentLabelStyle = defaultLabelStyle;
                                break;
                        }
                    }

                    EditorGUI.LabelField(r3, animNameText, currentLabelStyle);

                    if (isNan)
                    {
                        var msgRect = r3;

                        msgRect.x -= 60f;
                        msgRect.width = 60;

                        EditorGUI.HelpBox(msgRect, "Missing", MessageType.Error);
                    }

                    bool isOptional = containerAnimationData != null && containerAnimationData.AnimationType == AnimationUseType.Optional;

                    if (isOptional)
                    {
                        var rRemove = r3;

                        rRemove.x -= 23;
                        rRemove.width = 20f;

                        if (GUI.Button(rRemove, "x"))
                        {
                            skinData.RemoveAnimationData(containerAnimationData.Guid);
                        }
                    }

                    r9 = r3;

                    var r5 = r4;
                    var r6 = r4;
                    var r7 = r4;

                    r5.x += 10;
                    r6.x += 10;
                    r7.x += 10;

                    const int fieldCount = 3;

                    r5.width = 60f;
                    r6.width = 60f;
                    r7.width = r7.width / fieldCount;

                    r6.x += r6.width + 5;
                    r7.x += r6.width + r5.width + 10f;
                    r7.width = maxWidth + 5;

                    var r8 = r7;
                    r8.x += r8.width + 5;
                    r8.width += 40;

                    var totalWidth = r5.width + r6.width + r7.width;

                    var labelWidth = EditorGUIUtility.labelWidth;

                    EditorGUIUtility.labelWidth = 12f;

                    EditorGUI.BeginChangeCheck();

                    var xProp = animationProp.FindPropertyRelative("FrameOffsetX");
                    var yProp = animationProp.FindPropertyRelative("FrameOffsetY");

                    EditorGUI.PropertyField(r5, xProp, new GUIContent("X"));
                    EditorGUI.PropertyField(r6, yProp, new GUIContent("Y"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        propertyChanged = true;
                    }

                    EditorGUIUtility.labelWidth = labelWidth;

                    var clipName = animationProp.FindPropertyRelative("ClipName").stringValue;
                    EditorGUI.LabelField(r7, clipName, EditorStyles.boldLabel);

                    if (animationData.CompressionValuePerc != 0)
                    {
                        var text = string.Empty;

                        if (EditorGUIUtility.currentViewWidth - totalWidth > 450)
                        {
                            text = $"Compressed {animationData.CompressionValuePerc}%";
                        }
                        else
                        {
                            text = $"[C {animationData.CompressionValuePerc}%]";
                            r8.width -= 50f;
                        }

                        if (EditorGUIUtility.currentViewWidth - totalWidth > 330)
                            EditorGUI.LabelField(r8, text, EditorStyles.boldLabel);
                    }

                    r3.y += GetFieldOffset();
                    r4.y += GetFieldOffset();
                }

                if (showOptionalAnimationPopupProp.boolValue)
                {
                    ShowAddOptionalPopup(r9, index);
                }
            }

            if (propertyChanged)
            {
                animationContainerSO.ApplyModifiedProperties();
            }
        }

        private void ShowAddOptionalPopup(Rect r, int index)
        {
            r.y += GetFieldOffset();
            r.width += 113;

            newAnimIndex = EditorGUI.Popup(r, string.Empty, newAnimIndex, optionalHeaders);
            var realIndex = newAnimIndex - 1;

            bool hasAnimation = true;
            string guid = string.Empty;

            if (realIndex >= 0)
            {
                guid = indexToGuid[realIndex];
                hasAnimation = crowdSkinFactory.HasAnimation(index, guid);
            }

            r.x += r.width + 2;
            r.width = 20f;

            GUI.enabled = !hasAnimation;

            if (GUI.Button(r, "+"))
            {
                crowdSkinFactory.AddAnimation(index, guid);
            }

            GUI.enabled = true;
        }

        private float GetFieldSize() => EditorGUIUtility.singleLineHeight;

        private float GetFieldOffset() => GetFieldSize() + LabelMargin;
    }
}
