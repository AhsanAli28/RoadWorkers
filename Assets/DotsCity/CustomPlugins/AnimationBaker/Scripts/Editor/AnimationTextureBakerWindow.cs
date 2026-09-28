#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class AnimationTextureBakerWindow : EditorWindowBase
    {
        #region Constans

        public const int DefaultFrameRate = 24;
        public const int MaxFrameRate = 60;

        private const float FieldSizeMultiplier = 1.1f;
        private const float FieldOffset = 2f;
        private const int SelectionGridRowCount = 3;

        private const string AnimationMaterialBaseName = "AnimationTransitionMaterialBase";
        private const string AnimationCompressedMaterialBaseName = "AnimationCompressedTransitionMaterialBase";
        private const string AnimationCollectionPathKey = "AnimationTextureBaker_AnimationCollectionPath";

        private readonly int[] TextureSizes = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        private readonly int[] TextureSizesSq = new[] { 32 * 32, 64 * 64, 128 * 128, 256 * 256, 512 * 512, 1024 * 1024, 2048 * 2048, 4096 * 4096, 8192 * 8192, 16384 * 16384 };

        #endregion

        #region Helper types

        private enum FrameRateSourceType { Clip, CommonValue }
        private enum TextureBakeType { SingleTexture, MultipleTextures }
        private enum NamePatternType { Index, MeshName }
        private enum ClipDataSource { Custom, Template }
        private enum CompressionType { Uncompressed, Compressed }
        private enum TransitionType { PlayOnce, PlayForever }

        [Flags]
        private enum SettingsType
        {
            None = 0,
            FrameRate = 1 << 0,
            Common = 1 << 1,
            TransitionMode = 1 << 2,
            AnimationBinding = 1 << 3,
            Multimesh = 1 << 4,
        }

        #endregion

        #region Serializable variables

        [SerializeField] private bool settingsFoldout = true;

        [SerializeField] private AnimationCollectionContainer animationCollection;

        [SerializeField] private Material animationMaterialBase;

        [SerializeField] private FrameRateSourceType frameRateSource;

        [SerializeField][Range(1, MaxFrameRate)] private int frameRate = DefaultFrameRate;

        [Tooltip("<b>Transition mode</b> - enable preview animation of transitions")]
        [SerializeField] private SettingsType settingsType = SettingsType.FrameRate;

        [SerializeField] private bool addNormalTexture = true;

        [SerializeField] private bool bakeRelativeParent = true;

        [SerializeField] private CompressionType compressionType;

        [SerializeField] private TextureBakeType textureBakeType;

        [SerializeField] private bool limitTextureSize = true;

        [SerializeField] private int limitTextureIndex = 7;

        [SerializeField] private string clipDataTemplatePath;

        [SerializeField] private string saveTextureDataPath;

        [SerializeField] private string saveTexturePath;

        [SerializeField] private string saveMeshPath;

        [SerializeField] private string saveTextureDataName;

        [SerializeField] private string saveTextureName;

        [SerializeField] private NamePatternType namePatternType;

        [SerializeField] private AnimationTextureDataContainer createdTextureData;

        [SerializeField] private List<AnimationTextureDataContainer> animationTextureDataContainers = new List<AnimationTextureDataContainer>();

        [SerializeField] private bool sourceDataFoldout = true;

        [SerializeField] private List<Animator> sourceSkins = new List<Animator>();

        [SerializeField] private List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>();

        [SerializeField] private int selectedSkinIndex;

        [Tooltip("Time to end of current animation when interpolation transition between animations is enabled")]
        [SerializeField][Range(0, 2f)] private float transitionDuration = 0.4f;

        [SerializeField] private TransitionType transitionType;

        [SerializeField] private bool clipsFoldout = true;

        [SerializeField] private ClipDataSource clipDataSource;

        [SerializeField] private List<ClipData> clips = new List<ClipData>();

        [SerializeField] private int selectedClipTemplateIndex;

        [SerializeField] private bool textureDataFoldout = true;

        [SerializeField] private bool transitionFoldout = true;

        [Tooltip("Used to bake replacement textures")]
        [SerializeField] private SkinnedMeshRenderer samplingSkin;

        #endregion

        #region Variables

        private ReorderableList reorderableList;
        private SerializedObject so;

        private int selectedTextureIndex;
        private Vector2 skinsScrollPosition;
        private Vector2 clipsScrollPosition;
        private Vector2 windowScrollPosition;

        private TemplateClipDataContainer selectedTemplate;

        private List<TempSkinData> tempSkins = new List<TempSkinData>();

        private List<TemplateClipDataContainer> clipTemplates = new List<TemplateClipDataContainer>();
        private List<TempTextureData> textureDatas = new List<TempTextureData>();
        private string[] textureHeaders;
        private string[] clipTemplateHeaders;

        private Dictionary<int, TempParentData> parents = new Dictionary<int, TempParentData>();
        private SkinnedMeshRenderer selectedSkin;
        private TempSkinData tempSelectedSkin;
        private GameObject createdPreviewObject;
        private MeshFilter previewMeshFilter;
        private MeshRenderer previewMeshRenderer;
        private AnimationTextureData previewTextureData;

        private string[] skinHeaders;
        private Material tempPreviewMaterial;
        private bool previewIsPlaying;
        private bool transitionIsPlaying;
        private bool transitionComplete;
        private int selectedPreviewAnimIndex = -1;
        private int startPreviewAnimIndex = -1;
        private int nextPreviewAnimIndex = -1;
        private float previousTime;
        private float playbackTime;
        private string[] availableAnimations;
        private Dictionary<string, string> guidToAnim = new Dictionary<string, string>();
        private Dictionary<string, int> guidToIndex = new Dictionary<string, int>();

        #endregion

        #region Properties

        private bool HasSkin => !MultiMeshMode && skins.Count > 0 || MultiMeshMode && sourceSkins.Count > 0;
        private int SelectedSkinIndex => selectedSkinIndex;
        private int TextureCount => textureDatas.Count;
        private bool PreviewIsPlaying => previewIsPlaying && TextureCount > 0 && selectedPreviewAnimIndex != -1 && selectedSkinIndex != -1;
        private bool TransitionIsAvailable => PreviewIsPlaying;
        private bool TransitionIsPlaying => TransitionIsAvailable && TransitionMode && transitionIsPlaying;
        private int LocalTextureCount => addNormalTexture ? 2 : 1;
        private float CurrentTime => (float)EditorApplication.timeSinceStartup;
        private bool FrameRateSettings => settingsType.HasFlag(SettingsType.FrameRate);
        private bool CommonSettings => settingsType.HasFlag(SettingsType.Common);
        private bool TransitionMode => settingsType.HasFlag(SettingsType.TransitionMode);
        private bool BindingMode => settingsType.HasFlag(SettingsType.AnimationBinding);
        private bool MultiMeshMode => settingsType.HasFlag(SettingsType.Multimesh);
        private bool MultiTexture => textureBakeType == TextureBakeType.MultipleTextures;

        #endregion

        #region Overriden methods

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(400, 650);
        }

        #endregion

        #region Unity methods

        [MenuItem("Spirit604/Animation Baker")]
        public static void Open()
        {
            AnimationTextureBakerWindow window = (AnimationTextureBakerWindow)GetWindow(typeof(AnimationTextureBakerWindow));
            window.titleContent = new GUIContent("Animation Baker");
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            LoadData();

            so = new SerializedObject(this);

            InitBinding();
            InitList();
            LoadSheetData();
            CheckForPathFields();
            CheckForClipData(true);
            SelectCurrentClipTemplate();
            CheckForInitialNullData();
            FindPreviewMaterial();
            UpdateSkinHeaders(true);
            SelectPreviewSkin();

            EditorApplication.update += EditorApplication_update;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SaveData();
            CleanupPreviewObject();
            CleanTemp();
            EnableSelectedSkin();
            EditorApplication.update -= EditorApplication_update;
        }

        private void OnGUI()
        {
            so.Update();

            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition, false, false, GUILayout.ExpandHeight(true));

            ShowSettings();
            ShowSourceData();
            ShowTextureSettings();
            ShowTransitionAnimationData();

            EditorGUILayout.EndScrollView();

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Private methods

        private void ShowSettings()
        {
            Action settingsCallback = () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(animationCollection)));

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    SaveCollection();
                    InitBinding();
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(animationMaterialBase)));

                EditorGUILayout.PropertyField(so.FindProperty(nameof(frameRateSource)));

                switch (frameRateSource)
                {
                    case FrameRateSourceType.Clip:
                        break;
                    case FrameRateSourceType.CommonValue:
                        {
                            EditorGUI.indentLevel++;

                            frameRate = EditorGUILayout.IntSlider("Frame Rate", frameRate, 1, 60);

                            EditorGUI.indentLevel--;
                            break;
                        }
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(settingsType)));

                if (EditorGUI.EndChangeCheck())
                {
                    var previousSettings = settingsType;

                    so.ApplyModifiedProperties();

                    OnSettingsChanged(previousSettings, settingsType);
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(addNormalTexture)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(bakeRelativeParent)));

                GUI.enabled = false;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(compressionType)));

                GUI.enabled = true;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(textureBakeType)));

                if (!MultiTexture)
                {
                    var limitTextureSizeProp = so.FindProperty(nameof(limitTextureSize));

                    EditorGUILayout.PropertyField(limitTextureSizeProp);

                    if (limitTextureSizeProp.boolValue)
                    {
                        limitTextureIndex = EditorGUILayout.IntSlider($"Limit Texture [{TextureSizes[limitTextureIndex]}]", limitTextureIndex, 0, TextureSizes.Length - 1);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    TextureSettingsChanged();
                }

                const float textureNameWidth = 140f;
                const float selectButtonWidth = 20f;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(clipDataTemplatePath)));

                if (GUILayout.Button("+", GUILayout.Width(selectButtonWidth)))
                {
                    var clipDataPath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select clip data asset path", clipDataTemplatePath, "");

                    if (!string.IsNullOrEmpty(clipDataPath))
                    {
                        clipDataTemplatePath = clipDataPath;
                        LoadClipTemplateData(true);
                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(saveTextureDataPath)));

                EditorGUILayout.PropertyField(so.FindProperty(nameof(saveTextureDataName)), GUIContent.none, GUILayout.Width(textureNameWidth));

                if (GUILayout.Button("+", GUILayout.Width(selectButtonWidth)))
                {
                    var textureDataPath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select animation texture asset path", saveTextureDataPath, "");

                    if (!string.IsNullOrEmpty(textureDataPath))
                    {
                        saveTextureDataPath = textureDataPath;
                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(saveTexturePath)));

                EditorGUILayout.PropertyField(so.FindProperty(nameof(saveTextureName)), GUIContent.none, GUILayout.Width(textureNameWidth));

                if (GUILayout.Button("+", GUILayout.Width(selectButtonWidth)))
                {
                    var texturePath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select animation texture path", saveTexturePath, "");

                    if (!string.IsNullOrEmpty(texturePath))
                    {
                        saveTexturePath = texturePath;
                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (MultiMeshMode)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(saveMeshPath)));

                    if (GUILayout.Button("+", GUILayout.Width(selectButtonWidth)))
                    {
                        var newPath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select combined mesh path", saveMeshPath, "");

                        if (!string.IsNullOrEmpty(newPath))
                        {
                            saveMeshPath = newPath;
                            Repaint();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                switch (textureBakeType)
                {
                    case TextureBakeType.SingleTexture:
                        {
                            break;
                        }
                    case TextureBakeType.MultipleTextures:
                        {
                            EditorGUILayout.PropertyField(so.FindProperty(nameof(namePatternType)));
                            break;
                        }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", settingsCallback, ref settingsFoldout);
        }

        private void UpdateSkinHeaders(bool clear = false)
        {
            var skinCount = skins?.Count ?? 0;

            skinHeaders = new string[skinCount + 1];
            skinHeaders[0] = "None";

            for (int i = 0; i < skinCount; i++)
            {
                if (skins[i] == null)
                {
                    continue;
                }

                if (!MultiMeshMode)
                {
                    skinHeaders[i + 1] = skins[i].name;
                }
                else
                {
                    skinHeaders[i + 1] = sourceSkins[i].name;
                }
            }

            if (selectedSkinIndex >= skinHeaders.Length)
            {
                selectedSkinIndex = 0;
            }

            if (clear)
                Clear();
        }

        private void ShowSourceData()
        {
            Action sourceDataCallback = () =>
            {
                GUILayout.BeginVertical("HelpBox");

                EditorGUI.BeginChangeCheck();

                if (MultiMeshMode)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceSkins)));
                }
                else
                {
                    var skinProp = so.FindProperty(nameof(skins));

                    bool isExpanded = skinProp.isExpanded;

                    if (isExpanded)
                    {
                        float height = 100f;

                        if (skins.Count > 0)
                        {
                            height = skins.Count * 50f;
                        }

                        skinsScrollPosition = EditorGUILayout.BeginScrollView(skinsScrollPosition, GUILayout.MinHeight(150f), GUILayout.MaxHeight(height));
                    }

                    EditorGUILayout.PropertyField(skinProp);

                    if (isExpanded)
                    {
                        EditorGUILayout.EndScrollView();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    UpdateSkinHeaders(true);
                }

                if (skins?.Count > 0)
                {
                    if (skinHeaders == null || skins.Count != skinHeaders.Length - 1)
                    {
                        UpdateSkinHeaders();
                    }

                    var sourceSkinIndex = selectedSkinIndex + 1;

                    var gridRowCount = SelectionGridRowCount;

                    if (skins.Count > 10)
                    {
                        gridRowCount = Mathf.FloorToInt((float)skins.Count / 10) + 3;
                        gridRowCount = Mathf.Clamp(gridRowCount, 0, 6);
                    }

                    var newSelectedSkinIndex = GUILayout.SelectionGrid(sourceSkinIndex, skinHeaders, gridRowCount) - 1;

                    if (selectedSkinIndex != newSelectedSkinIndex)
                    {
                        selectedSkinIndex = newSelectedSkinIndex;

                        if (selectedSkinIndex >= 0)
                        {
                            SelectPreviewSkin(selectedSkinIndex, true);
                        }
                        else
                        {
                            EnableSelectedSkin();
                        }
                    }
                }
                else
                {
                    if (selectedSkinIndex != -1)
                    {
                        selectedSkinIndex = -1;
                        EnableSelectedSkin();
                    }
                }

                GUILayout.EndVertical();

                if (HasSkin)
                {
                    GUILayout.BeginVertical("HelpBox");

                    clipsFoldout = EditorGUILayout.Foldout(clipsFoldout, $"Clips [{clips.Count}]");

                    DropAreaGUI(GUILayoutUtility.GetLastRect());

                    if (clipsFoldout)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(so.FindProperty(nameof(clipDataSource)));

                        if (EditorGUI.EndChangeCheck())
                        {
                            so.ApplyModifiedProperties();

                            CheckForClipData(selectTemplate: true);
                            SelectCurrentClipTemplate();
                        }

                        if (clipDataSource == ClipDataSource.Template)
                        {
                            var newSelectedTemplateIndex = EditorGUILayout.Popup(selectedClipTemplateIndex, clipTemplateHeaders);

                            if (clipTemplateHeaders.Length > 0 && selectedClipTemplateIndex != newSelectedTemplateIndex)
                            {
                                selectedClipTemplateIndex = newSelectedTemplateIndex;
                                SelectClipTemplate(selectedClipTemplateIndex);
                            }
                        }

                        EditorGUILayout.EndHorizontal();

                        if (clipDataSource == ClipDataSource.Template)
                        {
                            EditorGUILayout.BeginHorizontal();

                            GUI.enabled = selectedTemplate;

                            if (GUILayout.Button("Save"))
                            {
                                SaveSelectedClipTemplate();
                            }

                            GUI.enabled = true;

                            if (GUILayout.Button("Save As"))
                            {
                                SaveNewClipTemplate();
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        clipsScrollPosition = EditorGUILayout.BeginScrollView(clipsScrollPosition, GUILayout.MinHeight(200f), GUILayout.MaxHeight(500f));

                        reorderableList.DoLayoutList();

                        EditorGUILayout.EndScrollView();
                    }

                    GUILayout.BeginHorizontal();

                    string text = textureDatas.Count == 0 ? "Create New" : "Update Texture";

                    if (GUILayout.Button(text))
                    {
                        CreateAnimationTexture();
                    }

                    GUILayout.EndHorizontal();

                    if (textureDatas.Count > 0)
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Save As New"))
                        {
                            Save();
                        }

                        GUI.enabled = TextureCount >= 1;

                        if (GUILayout.Button("Override Exist"))
                        {
                            Save(true);
                        }

                        GUI.enabled = true;

                        if (GUILayout.Button("Clear"))
                        {
                            Clear();
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();

                    if (previewIsPlaying)
                    {
                        if (GUILayout.Button("Focus Preview Animation"))
                        {
                            FocusPreviewAnimation();
                        }
                    }
                }
                else
                {
                    string messageText = !MultiMeshMode ? "Assign skinned mesh renderers!" : "Assign character parents!";
                    EditorGUILayout.HelpBox(messageText, MessageType.Info);
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Source Data", sourceDataCallback, ref sourceDataFoldout);
        }

        private void FindPreviewMaterial()
        {
            var materials = AssetDatabase.FindAssets(AnimationMaterialBaseName);

            if (materials?.Length > 0)
            {
                animationMaterialBase = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(materials[0]), typeof(Material)) as Material;
            }
            else
            {
                Debug.LogError($"Preview material '{AnimationMaterialBaseName}' not found");
            }
        }

        private void SelectPreviewSkin()
        {
            SelectPreviewSkin(SelectedSkinIndex);
        }

        private void SelectPreviewSkin(int skinIndex, bool transition = false)
        {
            selectedSkinIndex = skinIndex;

            if (skinIndex < 0)
            {
                return;
            }

            EnableSelectedSkin();

            for (int i = 0; i < skins.Count; i++)
            {
                if (!parents.ContainsKey(skins[i].GetInstanceID()))
                {
                    var parentData = GetParent(skins[i]);
                    parents.Add(skins[i].GetInstanceID(), parentData);
                }
            }

            if (skinIndex >= 0 && skins.Count > skinIndex && skins[skinIndex] != null && tempSkins.Count > skinIndex)
            {
                selectedSkin = skins[skinIndex];
                tempSelectedSkin = tempSkins[skinIndex];

                CreatePreviewObject();

                if (PreviewIsPlaying)
                {
                    StartPreviewPlayback(selectedPreviewAnimIndex, transition);
                }
            }
        }

        private TempParentData GetParent(SkinnedMeshRenderer selectedSkin)
        {
            var parentAnimator = selectedSkin.GetComponentInParent<Animator>(true);
            GameObject parent = null;

            if (parentAnimator && (parentAnimator.transform == selectedSkin.transform.parent || parentAnimator.transform == selectedSkin.transform))
            {
                parent = parentAnimator.gameObject;
            }

            if (parent == null)
            {
                parent = selectedSkin.gameObject;
            }

            return new TempParentData()
            {
                Parent = parent,
                PreviousActiveState = parentAnimator.gameObject.activeSelf,
            };
        }

        private void EnableSelectedSkin(bool unsignSkin = true)
        {
            if (unsignSkin)
            {
                selectedSkin = null;
            }

            foreach (var parent in parents)
            {
                parent.Value.RevertState();
            }

            if (createdPreviewObject)
            {
                createdPreviewObject.SetActive(false);
            }
        }

        private void CreatePreviewObject()
        {
            CleanupPreviewObject();

            if (selectedSkin == null)
            {
                return;
            }

            if (animationMaterialBase == null)
            {
                Debug.LogError("Base preview material not assigned");
                return;
            }

            createdPreviewObject = new GameObject("Preview Mesh");
            createdPreviewObject.hideFlags = HideFlags.HideAndDontSave;
            previewMeshFilter = createdPreviewObject.AddComponent<MeshFilter>();
            previewMeshRenderer = createdPreviewObject.AddComponent<MeshRenderer>();

            var copyMesh = Instantiate(tempSelectedSkin.Mesh);
            tempPreviewMaterial = Instantiate(animationMaterialBase);

            copyMesh.hideFlags = HideFlags.DontSave;
            tempPreviewMaterial.hideFlags = HideFlags.DontSave;

            previewMeshFilter.sharedMesh = copyMesh;
            previewMeshRenderer.sharedMaterial = tempPreviewMaterial;

            tempPreviewMaterial.SetTexture(Constans.MainTexture, selectedSkin.sharedMaterial.mainTexture);

            createdPreviewObject.transform.position = selectedSkin.transform.position;
        }

        private void StartPreviewPlayback()
        {
            StartPreviewPlayback(selectedPreviewAnimIndex);
        }

        private void StartPreviewPlayback(int clipIndex, bool setNewAnim = true, bool transition = false)
        {
            if (!selectedSkin || selectedSkinIndex < 0)
            {
                SelectPreviewSkin(0);
            }

            if (!previewIsPlaying)
            {
                previewIsPlaying = true;
                FocusPreviewAnimation();
            }

            if (!tempPreviewMaterial)
            {
                Debug.LogError("Preview material is not assigned");
                return;
            }

            if (clips.Count <= clipIndex)
            {
                selectedPreviewAnimIndex = -1;
                previewIsPlaying = false;
                return;
            }

            foreach (var parent in parents)
            {
                parent.Value.SwitchActiveState(false);
            }

            createdPreviewObject.gameObject.SetActive(true);

            if (!transition)
            {
                if (setNewAnim)
                {
                    selectedPreviewAnimIndex = clipIndex;

                    if (nextPreviewAnimIndex != -1)
                    {
                        ResetTransition();
                    }
                }
            }
            else
            {
                startPreviewAnimIndex = selectedPreviewAnimIndex;
                nextPreviewAnimIndex = clipIndex;
                StartTransition();
            }

            var skinData = tempSkins[SelectedSkinIndex];
            var tempTextureData = GetTempTextureData(skinData);

            if (tempTextureData != null)
            {
                previewTextureData = GetTextureData(selectedSkinIndex, clipIndex);

                var clipData = clips[clipIndex];

                tempPreviewMaterial.SetTexture(Constans.AnimationTexture, tempTextureData.GetTexture(0));
                tempPreviewMaterial.SetTexture(Constans.NormalTexture, tempTextureData.GetTexture(1));
                tempPreviewMaterial.SetFloat(Constans.ClipLengthParam, previewTextureData.ClipLength);
                tempPreviewMaterial.SetFloat(Constans.FrameCountParam, previewTextureData.FrameCount);
                tempPreviewMaterial.SetFloat(Constans.VertexCountParam, previewTextureData.VertexCount);
                tempPreviewMaterial.SetFloat(Constans.FrameStepInvParam, previewTextureData.FrameStepInverted);
                tempPreviewMaterial.SetVector(Constans.FrameOffsetParam, new Vector2(previewTextureData.TextureOffset.x, previewTextureData.TextureOffset.y));
                tempPreviewMaterial.SetInt(Constans.InterpolateParam, clipData.InterpolateValue);

                if (!transition && nextPreviewAnimIndex == -1)
                {
                    StopPreviewPlayback(true, false);
                }
                else
                {
                    var tPreviewTextureData = GetTextureData(selectedSkinIndex, nextPreviewAnimIndex);

                    tempPreviewMaterial.SetFloat(Constans.TransitionTime, transitionDuration);
                    tempPreviewMaterial.SetVector(Constans.TargetFrameOffsetParam, new Vector4(tPreviewTextureData.TextureOffset.x, tPreviewTextureData.TextureOffset.y));
                    tempPreviewMaterial.SetFloat(Constans.ManualAnimation, 1);
                    tempPreviewMaterial.SetFloat(Constans.Transition, 1);
                }
            }
        }

        private void StopPreviewPlayback(bool transition = false, bool restartAnim = true)
        {
            if (!transition)
            {
                previewIsPlaying = false;
                selectedPreviewAnimIndex = -1;
                EnableSelectedSkin(false);
            }
            else
            {
                if (transitionIsPlaying)
                {
                    transitionIsPlaying = false;
                    nextPreviewAnimIndex = -1;

                    if (tempPreviewMaterial)
                    {
                        tempPreviewMaterial.SetVector(Constans.TargetFrameOffsetParam, Vector2.one * -1);
                        tempPreviewMaterial.SetFloat(Constans.PlaybackTime, -1);
                        tempPreviewMaterial.SetFloat(Constans.ManualAnimation, 0);
                        tempPreviewMaterial.SetFloat(Constans.Transition, 0);
                    }

                    if (PreviewIsPlaying && restartAnim)
                    {
                        StartPreviewPlayback();
                    }
                }
            }
        }

        private void FocusPreviewAnimation()
        {
            if (createdPreviewObject)
            {
                SceneView.lastActiveSceneView.Frame(new Bounds(createdPreviewObject.transform.position, Vector3.one * 3), false);
            }
        }

        private void StartTransition()
        {
            transitionIsPlaying = true;
            transitionComplete = false;
            previousTime = CurrentTime;
            playbackTime = 0;
        }

        private void ResetTransition(bool restartAnim = false)
        {
            StartTransition();

            if (restartAnim)
            {
                StartPreviewPlayback(startPreviewAnimIndex);
            }
        }

        private float GetFrameRate(ClipData clipData)
        {
            if (!clipData.HasCustomFrameRate)
            {
                var currentFrameRate = 0f;

                if (frameRateSource == FrameRateSourceType.Clip)
                {
                    currentFrameRate = clipData?.Clip?.frameRate ?? 0;
                }
                else
                {
                    currentFrameRate = frameRate;
                }

                return currentFrameRate;
            }
            else
            {
                return clipData.CustomFrameRate;
            }
        }

        private void CleanupPreviewObject()
        {
            if (createdPreviewObject != null)
            {
                DestroyImmediate(createdPreviewObject);
            }
        }

        private void CheckForClipData(bool force = false, bool selectClipData = false, bool selectTemplate = false)
        {
            if (clipDataSource == ClipDataSource.Template || force)
            {
                LoadClipTemplateData(selectTemplate);
            }

            if (selectClipData && clipDataSource == ClipDataSource.Template)
            {
                SelectCurrentClipTemplate();
            }
        }

        private void LoadClipTemplateData(bool selectTemplate = false)
        {
            clipTemplates = AssetDatabaseExtension.TryGetUnityObjectsOfTypeFromPath<TemplateClipDataContainer>(clipDataTemplatePath);

            if (clipTemplates?.Count > 0 && selectTemplate)
            {
                SelectClipTemplate(0);
            }

            UpdateClipTemplateHeaders();
        }

        private void UpdateClipTemplateHeaders()
        {
            var clipTemplateCount = clipTemplates?.Count ?? 0;
            clipTemplateHeaders = new string[clipTemplateCount];

            for (int i = 0; i < clipTemplateCount; i++)
            {
                clipTemplateHeaders[i] = clipTemplates[i].name;
            }
        }

        private void SelectCurrentClipTemplate()
        {
            SelectClipTemplate(selectedClipTemplateIndex);
        }

        private void SelectClipTemplate(int templateIndex)
        {
            if (clipTemplates?.Count > templateIndex)
            {
                selectedTemplate = clipTemplates[templateIndex];
                selectedClipTemplateIndex = templateIndex;
            }
            else if (clipTemplates?.Count > 0)
            {
                selectedTemplate = clipTemplates[clipTemplates.Count - 1];
                selectedClipTemplateIndex = clipTemplates.Count - 1;
                UpdateClipTemplateHeaders();
            }
            else
            {
                selectedTemplate = null;
                selectedClipTemplateIndex = 0;
            }

            if (selectedTemplate && clipDataSource == ClipDataSource.Template)
            {
                clips = selectedTemplate.GetClips();
            }
        }

        private void SaveSelectedClipTemplate()
        {
            if (selectedTemplate)
            {
                selectedTemplate.SaveClips(clips, true, true);
                Debug.Log("Templated saved.");
            }
        }

        private void SaveNewClipTemplate()
        {
            var savePath = EditorUtility.SaveFilePanel("Select template clip data save path", clipDataTemplatePath, "TemplateClipData", "asset");

            if (string.IsNullOrEmpty(savePath))
                return;

            savePath = AssetDatabaseExtension.ConvertToLocalProjectPath(savePath);

            var createdContainer = AssetDatabaseExtension.CreatePersistScriptableObject<TemplateClipDataContainer>(savePath);

            if (createdContainer != null)
            {
                savePath = savePath.Substring(0, savePath.LastIndexOf("/") + 1);
                clipDataTemplatePath = savePath;
                createdContainer.SaveClips(clips);

                selectedTemplate = createdContainer;
                clipTemplates.Add(createdContainer);

                UpdateClipTemplateHeaders();

                selectedTextureIndex = clipTemplates.Count - 1;

                EditorSaver.SetObjectDirty(this);
            }
        }

        private List<Texture2D> GetTextures(int index)
        {
            List<Texture2D> textures = null;

            if (textureDatas.Count > index)
            {
                var animTexture = textureDatas[index].GetTexture(0);

                if (animTexture)
                {
                    textures = new List<Texture2D>();
                    textures.Add(animTexture);
                }

                var normalTexture = textureDatas[index].GetTexture(1);

                if (normalTexture)
                {
                    textures.Add(normalTexture);
                }
            }

            return textures;
        }

        private AnimationTextureData GetTextureData(int skinIndex, int clipIndex)
        {
            var skinData = tempSkins[SelectedSkinIndex];
            var tempTextureData = GetTempTextureData(skinData);

            if (tempTextureData != null)
            {
                return tempTextureData.GetTextureData(skinIndex, clips.Count, clipIndex);
            }

            return null;
        }

        private TempTextureData GetTempTextureData(TempSkinData tempSkinData, bool ensureCapacity = false)
        {
            return GetTempTextureData(tempSkinData.DataIndex, ensureCapacity);
        }

        private TempTextureData GetTempTextureData(int index, bool ensureCapacity = false)
        {
            if (ensureCapacity)
            {
                EnsureCapacity(index);
            }

            if (textureDatas.Count > index)
            {
                return textureDatas[index];
            }

            return null;
        }

        private AnimationTextureDataContainer GetAnimationContainerData(int index)
        {
            if (animationTextureDataContainers.Count > index)
            {
                return animationTextureDataContainers[index];
            }

            return null;
        }

        private void SetTexture(int index, int localIndex, Texture2D texture)
        {
            EnsureCapacity(index);
            textureDatas[index].SetTexture(texture, localIndex);
        }

        private void EnsureCapacity(int index)
        {
            if (textureDatas.Count <= index)
            {
                EnsureTextureCapacity(index);

                CheckHeader();
            }
        }

        private void CheckHeader()
        {
            if (textureHeaders != null && textureHeaders.Length == textureDatas.Count)
                return;

            textureHeaders = new string[textureDatas.Count];

            if (textureBakeType == TextureBakeType.MultipleTextures)
            {
                for (int i = 0; i < textureHeaders.Length; i++)
                {
                    textureHeaders[i] = $"{skins[i].name}";
                }
            }
            else
            {
                for (int i = 0; i < textureHeaders.Length; i++)
                {
                    textureHeaders[i] = $"Atlas {i + 1}";
                }
            }
        }

        private void EnsureTextureCapacity(int index)
        {
            int addCount = index - textureDatas.Count + 1;

            for (int i = 0; i < addCount; i++)
            {
                textureDatas.Add(new TempTextureData());
            }
        }

        private void SetTextureData(AnimationTextureDataContainer textureData, int index, bool ensureCapacity = true)
        {
            if (ensureCapacity)
            {
                EnsureCapacity(index);
            }

            if (animationTextureDataContainers.Count <= index)
            {
                int addCount = index - animationTextureDataContainers.Count + 1;

                for (int i = 0; i < addCount; i++)
                {
                    animationTextureDataContainers.Add(null);
                }
            }

            animationTextureDataContainers[index] = textureData;
        }

        private void SaveMesh(int index)
        {
            if (!MultiMeshMode)
                return;

            if (!tempSkins[index].NewMeshFlag)
                return;

            var path = GetMeshSavePath(index);

            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Mesh '{path}' deleted");
            }

            AssetDatabase.CreateAsset(tempSkins[index].Mesh, path);

            Debug.Log($"Mesh '{path}' saved");
        }

        private void ShowTextureSettings()
        {
            var textureData = GetTempTextureData(selectedTextureIndex);

            Action textureDataCallback = () =>
            {
                if (TextureCount > 0)
                {
                    if (TextureCount > 1)
                    {
                        selectedTextureIndex = GUILayout.SelectionGrid(selectedTextureIndex, textureHeaders, SelectionGridRowCount);
                    }
                    else
                    {
                        selectedTextureIndex = 0;
                    }

                    var sq = Mathf.RoundToInt(Mathf.Sqrt(textureData.VertexCount));
                    GUILayout.Label($"Data texture size: [{textureData.DataWidth}x{textureData.DataHeight}] [Square size {sq}x{sq}]", EditorStyles.boldLabel);

                    const float TextureSize = 196;

                    var texture = textureData.GetTexture(0);

                    if (texture != null)
                    {
                        GUILayout.Label($"Texture size: [{texture.width}x{texture.height}]", EditorStyles.boldLabel);
                    }

                    GUILayout.BeginHorizontal("GroupBox");

                    for (int i = 0; i < LocalTextureCount; i++)
                    {
                        var localTexture = textureData.GetTexture(i);

                        if (localTexture != null)
                        {
                            GUILayout.Label(localTexture, GUILayout.Width(TextureSize), GUILayout.Height(TextureSize));
                        }
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("Texture not found");
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Texture Data", textureDataCallback, ref textureDataFoldout);
        }

        private void ShowTransitionAnimationData()
        {
            if (!TransitionMode)
                return;

            InspectorExtension.DrawDefaultInspectorGroupBlock("Transition Data", () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(transitionType)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(transitionDuration)));

                GUI.enabled = TransitionIsPlaying;

                if (GUILayout.Button("Stop Transition"))
                {
                    StopPreviewPlayback(true);
                }

                if (GUILayout.Button("Restart"))
                {
                    ResetTransition(true);
                }

                GUI.enabled = true;

            }, ref transitionFoldout);
        }

        private void DropAreaGUI(Rect rect)
        {
            Event evt = Event.current;
            Rect drop_area = rect;

            const float mult = 0.6f;

            drop_area.x = rect.x;
            drop_area.y = rect.y;
            drop_area.width = rect.width * mult;
            drop_area.height += 2f;
            drop_area.x += rect.width * (1 - mult);

            GUI.Box(drop_area, "Drag & drop animation clips");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object dragged_object in DragAndDrop.objectReferences)
                        {
                            AddClip(dragged_object);
                        }
                    }

                    break;
            }
        }

        private void CreateAnimationTexture()
        {
            var recreate = textureDatas.Count != 0;

            var prevPreviewAnimIndex = -1;
            var prevPreviewIsPlaying = false;

            if (recreate)
            {
                prevPreviewAnimIndex = selectedPreviewAnimIndex;
                prevPreviewIsPlaying = previewIsPlaying;
            }

            Clear();

            if (clips.Count == 0)
                return;

            if (MultiMeshMode)
            {
                for (int i = 0; i < sourceSkins.Count; i++)
                {
                    var skinParent = sourceSkins[i].gameObject;

                    if (skinParent == null)
                        continue;

                    skinParent.SetActive(true);
                    var newParent = Instantiate(skinParent);
                    newParent.transform.position = default;
                    newParent.transform.rotation = Quaternion.identity;
                    newParent.hideFlags = HideFlags.HideAndDontSave;

                    var inactiveSkins = newParent.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(a => !a.gameObject.activeInHierarchy).ToArray();

                    foreach (var inactiveSkin in inactiveSkins)
                    {
                        DestroyImmediate(inactiveSkin.gameObject);
                    }

                    Mesh mesh = null;

                    var skinnedRenderers = newParent.GetComponentsInChildren<SkinnedMeshRenderer>();

                    bool newMeshFlag = false;

                    SkinnedMeshRenderer newSkin = null;

                    if (skinnedRenderers.Length > 1)
                    {
                        var currentMeshes = skinnedRenderers.Select(a => a.sharedMesh).ToArray();

                        SkinnedMeshRenderer sourceSkin = skinnedRenderers[0];

                        float xBounds = float.MaxValue;

                        for (int k = 0; k < skinnedRenderers.Length; k++)
                        {
                            if (skinnedRenderers[k].bounds.center.y < xBounds)
                            {
                                sourceSkin = skinnedRenderers[k];
                                xBounds = skinnedRenderers[k].bounds.center.y;
                            }
                        }

                        for (int k = 0; k < skinnedRenderers.Length; k++)
                        {
                            if (skinnedRenderers[k] == sourceSkin)
                                continue;

                            MeshCombiner.UpdateMeshBones(sourceSkin.rootBone, sourceSkin, skinnedRenderers[i]);
                        }

                        var bindPoses = sourceSkin.sharedMesh.bindposes;
                        newSkin = MeshCombiner.CombineFast(sourceSkin.rootBone, sourceSkin.sharedMaterial, sourceSkin.bones, currentMeshes, bindPoses, customParent: newParent.transform, name: "Skin");

                        skins.Add(newSkin);

                        parents.Add(newSkin.GetInstanceID(), new TempParentData()
                        {
                            PreviousActiveState = true,
                            Parent = skinParent
                        });

                        mesh = newSkin.sharedMesh;

                        var oldSkins = newParent.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(a => a != newSkin).ToArray();

                        foreach (var oldSkin in oldSkins)
                        {
                            DestroyImmediate(oldSkin.gameObject);
                        }

                        List<GameObject> oldGos = new List<GameObject>();

                        for (int j = 0; j < newParent.transform.childCount; j++)
                        {
                            var item = newParent.transform.GetChild(j);

                            if (item.transform.childCount == 0 && item.gameObject.GetComponents<Component>().Length <= 1)
                            {
                                oldGos.Add(item.gameObject);
                            }
                        }

                        foreach (var oldGo in oldGos)
                        {
                            DestroyImmediate(oldGo.gameObject);
                        }

                        newMeshFlag = true;
                    }
                    else
                    {
                        newSkin = skinnedRenderers[0];
                        mesh = newSkin.sharedMesh;
                        skins.Add(newSkin);

                        parents.Add(newSkin.GetInstanceID(), new TempParentData()
                        {
                            PreviousActiveState = true,
                            Parent = skinParent
                        });
                    }

                    var filters = newParent.GetComponentsInChildren<MeshFilter>();

                    if (filters?.Length > 0)
                    {
                        newMeshFlag = true;

                        List<CombineInstance> instances = new List<CombineInstance>();

                        instances.Add(new CombineInstance()
                        {
                            mesh = mesh,
                            transform = Matrix4x4.identity
                        });

                        for (int j = 0; j < filters.Length; j++)
                        {
                            instances.Add(new CombineInstance()
                            {
                                mesh = filters[j].sharedMesh,
                                transform = Matrix4x4.TRS(filters[j].transform.position, filters[j].transform.rotation, filters[j].transform.localScale)
                            });
                        }

                        var newMesh = new Mesh();

                        newMesh.name = mesh.name;
                        newMesh.CombineMeshes(instances.ToArray(), true, true);

                        mesh = newMesh;

                        newMesh.RecalculateBounds();
                    }

                    mesh.name = newParent.name;

                    tempSkins.Add(new TempSkinData()
                    {
                        Animator = newParent.GetComponent<Animator>(),
                        TempParent = newParent,
                        Mesh = mesh,
                        Skin = newSkin,
                        Attachments = filters,
                        NewMeshFlag = newMeshFlag
                    });

                    newParent.gameObject.SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < skins.Count; i++)
                {
                    SkinnedMeshRenderer skin = skins[i];

                    if (skin == null)
                        continue;

                    var animator = skin.GetComponentInParent<Animator>(true);

                    tempSkins.Add(new TempSkinData()
                    {
                        Animator = animator,
                        Mesh = skin.sharedMesh,
                        Skin = skin,
                    });
                }
            }

            for (int i = 0; i < tempSkins.Count; i++)
            {
                var tempSkin = tempSkins[i];

                if (!tempSkin.HasSkin)
                    continue;

                var tempData = new TempTextureData();
                tempData.SkinIndices.Add(i);

                foreach (var clipData in clips)
                {
                    var clip = clipData.Clip;

                    if (clip == null)
                    {
                        Debug.LogError("Animation clip is empty");
                        continue;
                    }

                    int frameCount = CalculateFrameCount(clipData);
                    clipData.FrameCount = frameCount;

                    var currentFrameRate = GetFrameRate(clipData);

                    int skinVertexCount = 0;
                    int animVertexCount = 0;

                    var skin = tempSkin.Skin;
                    var currentSkinVertexCount = skin.sharedMesh.vertexCount;

                    skinVertexCount += currentSkinVertexCount;
                    animVertexCount += currentSkinVertexCount * frameCount;

                    var animData = new AnimationTextureData()
                    {
                        SourceMesh = tempSkins[i].Mesh,
                        SourceMaterial = tempSkins[i].Skin.sharedMaterial,
                        SourceClip = clip,
                        FrameRate = currentFrameRate,
                        FrameCount = frameCount,
                        VertexCount = skinVertexCount,
                        AnimationGUID = clipData.Guid,
                        AnimationName = clipData.AnimationName,
                        BakeOffset = clipData.Offset,
                        Interpolate = clipData.Interpolate
                    };

                    tempData.VertexCount += animVertexCount;

                    if (MultiMeshMode)
                    {
                        for (int j = 0; j < tempSkin.Attachments?.Length; j++)
                        {
                            var attachment = tempSkin.Attachments[j];

                            tempData.VertexCount += attachment.sharedMesh.vertexCount * frameCount;
                            animData.VertexCount += attachment.sharedMesh.vertexCount;
                        }
                    }

                    tempData.Data.Add(animData);
                }

                int mergeIndex = -1;

                TempTextureData sourceTextureData = null;

                if (textureBakeType == TextureBakeType.SingleTexture)
                {
                    if (limitTextureSize)
                    {
                        for (int j = 0; j < textureDatas.Count; j++)
                        {
                            if (textureDatas[j].VertexCount + tempData.VertexCount <= TextureSizesSq[limitTextureIndex])
                            {
                                sourceTextureData = textureDatas[j];
                                mergeIndex = j;
                                break;
                            }
                        }
                    }
                    else if (textureDatas.Count > 0)
                    {
                        sourceTextureData = textureDatas[textureDatas.Count - 1];
                        mergeIndex = textureDatas.Count - 1;
                    }
                }

                if (sourceTextureData == null)
                {
                    textureDatas.Add(tempData);
                    tempSkin.DataIndex = textureDatas.Count - 1;
                }
                else
                {
                    sourceTextureData.Merge(tempData);
                    tempSkin.DataIndex = mergeIndex;
                }
            }

            for (int i = 0; i < tempSkins.Count; i++)
            {
                var tempSkin = tempSkins[i];

                if (!tempSkin.HasSkin)
                    continue;

                CreateAnimationTexture(tempSkin);
            }

            CheckHeader();

            for (int i = 0; i < textureDatas.Count; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var texture = textureDatas[i].GetTexture(j);

                    if (texture != null) texture.Apply(false);
                }
            }

            SelectPreviewSkin();

            selectedPreviewAnimIndex = prevPreviewAnimIndex;
            previewIsPlaying = prevPreviewIsPlaying;

            if (PreviewIsPlaying)
            {
                StartPreviewPlayback();
            }
        }

        private void CreateAnimationTexture(TempSkinData tempSkin)
        {
            if (!tempSkin.Animator)
            {
                Debug.LogError($"Target skin '{tempSkin.Name}' doesn't have animator component. Add an animator component to bake animations.");
                return;
            }

            var currentIndex = tempSkin.DataIndex;
            var targetGameObject = tempSkin.Animator.gameObject;

            var tempData = GetTempTextureData(currentIndex, true);

            int currentWidth = TextureSizes[0];
            int currentHeight = TextureSizes[0];

            for (int i = 0; i < TextureSizesSq.Length; i++)
            {
                if (tempData.VertexCount < TextureSizesSq[i])
                {
                    currentWidth = TextureSizes[i];
                    currentHeight = TextureSizes[i];
                    break;
                }
            }

            var textures = GetTextures(currentIndex);

            if (textures == null)
            {
                textures = CreateTextures(currentWidth, currentHeight);

                for (int idx = 0; idx < textures.Count; idx++)
                {
                    Texture2D texture = textures[idx];
                    SetTexture(currentIndex, idx, texture);
                }
            }

            tempData.AtlasWidth = currentWidth;
            tempData.AtlasHeight = Mathf.Max(tempData.AtlasHeight, currentHeight);

            foreach (var clipData in clips)
            {
                var clip = clipData.Clip;

                if (clip == null)
                    continue;

                var duration = clip.length;
                var frameCount = clipData.FrameCount;

                BakeAnimation(tempSkin, targetGameObject, clip, textures, tempData, frameCount, duration, clipData.Offset);

                tempData.AnimationDataIndex++;
            }

            tempData.DataWidth = tempData.BakingFrameOffset.x;
            tempData.DataHeight = tempData.AtlasHeight;
        }

        private void CopyTexture(Texture2D srcTexture, Texture2D dstTexture)
        {
            if (srcTexture.width != dstTexture.width || srcTexture.height != dstTexture.height)
            {
                var copyWidth = Mathf.Min(srcTexture.width, dstTexture.width);
                var copyHeight = Mathf.Min(srcTexture.height, dstTexture.height);

                for (int x = 0; x < copyWidth; x++)
                {
                    for (int y = 0; y < copyHeight; y++)
                    {
                        dstTexture.SetPixel(x, y, srcTexture.GetPixel(x, y));
                    }
                }
            }
            else
            {
                var pixels = srcTexture.GetPixels();
                dstTexture.SetPixels(pixels);
            }
        }

        private int CalculateFrameCount(AnimationClip clip, float frameRate)
        {
            var duration = clip.length;
            var frameCount = Mathf.Max((int)(duration * frameRate), 1);
            return frameCount;
        }

        private int CalculateFrameCount(ClipData clip)
        {
            var currentFrameRate = GetFrameRate(clip);
            return CalculateFrameCount(clip.Clip, currentFrameRate);
        }

        private List<Texture2D> CreateTextures(int width, int height)
        {
            var textures = new List<Texture2D>();

            var texture = CreateTexture(width, height);

            textures.Add(texture);

            if (addNormalTexture)
            {
                var textureNormal = CreateTexture(width, height);

                textures.Add(textureNormal);
            }

            return textures;
        }

        private Texture2D CreateTexture(int width, int height)
        {
            var textureFormat = TextureFormat.RGBAHalf;
            var filterMode = FilterMode.Point;
            var wrapMode = TextureWrapMode.Clamp;

            var texture = new Texture2D(
                width,
                height,
                textureFormat,
                false,
                false);

            texture.filterMode = filterMode;
            texture.wrapMode = wrapMode;

            return texture;
        }

        private void Save(bool replace = false)
        {
            AnimationTextureDataMultiContainer multiContainer = null;

            if (TextureCount > 1)
            {
                string textureMultiDataName;

                if (saveTextureDataName.Contains("Data"))
                {
                    textureMultiDataName = saveTextureDataName.Replace("Data", "MultiData");
                }
                else
                {
                    textureMultiDataName = $"{saveTextureDataName}MultiData";
                }

                var assetPath = Path.Combine(saveTextureDataPath, textureMultiDataName) + ".asset";

                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                multiContainer = ScriptableObject.CreateInstance<AnimationTextureDataMultiContainer>();
                AssetDatabase.CreateAsset(multiContainer, assetPath);
            }

            for (int i = 0; i < TextureCount; i++)
            {
                var data = GetTempTextureData(i);
                var textures = GetTextures(i);
                var lastTexture = i == TextureCount - 1;

                CreateAsset(data, textures, i, replace, lastTexture, multiContainer);
            }

            if (MultiMeshMode)
            {
                for (int i = 0; i < skins.Count; i++)
                {
                    SaveMesh(i);
                }
            }
        }

        private void BakeAnimation(
            TempSkinData tempSkin,
            GameObject targetGameObject,
            AnimationClip clip,
            List<Texture2D> textures,
            TempTextureData tempTextureData,
            int frameCount,
            float duration,
            Vector3 vertexOffset)
        {
            var mesh = new Mesh();

            var lastFrameIndex = frameCount - 1;

            tempTextureData.Data[tempTextureData.AnimationDataIndex].TextureOffset = tempTextureData.BakingFrameOffset;

            var previousPos = targetGameObject.transform.position;
            var previousRot = targetGameObject.transform.rotation;

            targetGameObject.transform.position = default;
            targetGameObject.transform.rotation = Quaternion.identity;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var time = (float)frameIndex / lastFrameIndex * duration;
                clip.SampleAnimation(targetGameObject, time);

                var skin = tempSkin.Skin;
                skin.BakeMesh(mesh);

                var vertices = mesh.vertices;
                var normals = mesh.normals;

                for (int i = 0; i < vertices.Length; i++)
                {
                    var vertexPosition = vertices[i];

                    if (bakeRelativeParent)
                    {
                        vertexPosition = targetGameObject.transform.TransformPoint(vertexPosition);
                    }

                    var position = vertexPosition + vertexOffset;

                    var positionColor = new Color(position.x, position.y, position.z);

                    var bakingFrameOffset = tempTextureData.BakingFrameOffset;
                    textures[0].SetPixel(bakingFrameOffset.x, bakingFrameOffset.y, positionColor);

                    if (textures.Count > 1)
                    {
                        var normal = normals[i];

                        if (bakeRelativeParent)
                        {
                            normal = targetGameObject.transform.TransformDirection(normal);
                        }

                        var normalColor = new Color(normal.x, normal.y, normal.z);

                        textures[1].SetPixel(bakingFrameOffset.x, bakingFrameOffset.y, normalColor);
                    }

                    if (bakingFrameOffset.y + 1 < tempTextureData.AtlasHeight)
                    {
                        bakingFrameOffset.y++;
                    }
                    else
                    {
                        bakingFrameOffset.y = 0;
                        bakingFrameOffset.x++;
                    }

                    tempTextureData.BakingFrameOffset = bakingFrameOffset;
                }

                if (MultiMeshMode)
                {
                    for (int idx = 0; idx < tempSkin.Attachments?.Length; idx++)
                    {
                        var attachment = tempSkin.Attachments[idx];
                        var attachmentMesh = attachment.sharedMesh;
                        vertices = attachmentMesh.vertices;
                        normals = attachmentMesh.normals;

                        for (int i = 0; i < vertices.Length; i++)
                        {
                            var vertexPosition = attachment.transform.TransformPoint(vertices[i]);

                            if (!bakeRelativeParent)
                            {
                                vertexPosition = targetGameObject.transform.InverseTransformPoint(vertexPosition);
                            }

                            var position = vertexPosition + vertexOffset;
                            var positionColor = new Color(position.x, position.y, position.z);

                            var bakingFrameOffset = tempTextureData.BakingFrameOffset;

                            textures[0].SetPixel(bakingFrameOffset.x, bakingFrameOffset.y, positionColor);

                            if (textures.Count > 1)
                            {
                                var normal = attachment.transform.TransformDirection(normals[i]);

                                if (!bakeRelativeParent)
                                {
                                    normal = targetGameObject.transform.InverseTransformDirection(normal);
                                }

                                var normalColor = new Color(normal.x, normal.y, normal.z);

                                textures[1].SetPixel(bakingFrameOffset.x, bakingFrameOffset.y, normalColor);
                            }

                            if (bakingFrameOffset.y + 1 < tempTextureData.AtlasHeight)
                            {
                                bakingFrameOffset.y++;
                            }
                            else
                            {
                                bakingFrameOffset.y = 0;
                                bakingFrameOffset.x++;
                            }

                            tempTextureData.BakingFrameOffset = bakingFrameOffset;
                        }
                    }
                }
            }

            targetGameObject.transform.position = previousPos;
            targetGameObject.transform.rotation = previousRot;

            DestroyImmediate(mesh);
        }

        private void CreateAsset(TempTextureData tempTextureData, List<Texture2D> textures, int index, bool replace = false, bool lastTexture = false, AnimationTextureDataMultiContainer multiContainer = null)
        {
            string textureDataSavePath = GetTextureDataSavePath(index);

            if (!replace)
            {
                textureDataSavePath = AssetDatabase.GenerateUniqueAssetPath(textureDataSavePath);
            }
            else
            {
                DeleteAsset(textureDataSavePath);
            }

            AnimationTextureDataContainer animationSheetData = ScriptableObject.CreateInstance<AnimationTextureDataContainer>();
            SaveData(animationSheetData, tempTextureData, textures);

            for (int i = 0; i < textures.Count; i++)
            {
                string texturePath = GetTextureSavePath(index, i);

                if (!replace)
                {
                    texturePath = AssetDatabase.GenerateUniqueAssetPath(texturePath);
                }
                else
                {
                    DeleteAsset(texturePath);
                }

                AssetDatabase.CreateAsset(textures[i], texturePath);
                Debug.Log($"Texture '{texturePath}' saved");
            }

            if (multiContainer == null)
            {
                AssetDatabase.CreateAsset(animationSheetData, textureDataSavePath);
            }
            else
            {
                if (!MultiTexture)
                {
                    animationSheetData.name = $"Atlas {index + 1}";
                }
                else
                {
                    animationSheetData.name = skinHeaders[index + 1];
                }

                AssetDatabase.AddObjectToAsset(animationSheetData, multiContainer);
                multiContainer.AddContainer(animationSheetData);
            }

            SetTextureData(animationSheetData, index);

            for (int i = 0; i < textures.Count; i++)
            {
                animationSheetData.SetTexture(textures[i], i);
            }

            EditorSaver.SetObjectDirty(animationSheetData);

            LoadSheetData(animationSheetData, index);

            if (multiContainer == null)
                Debug.Log($"TextureData '{textureDataSavePath}' saved");

            if (lastTexture)
            {
                if (multiContainer != null)
                    Debug.Log($"TextureMultiData '{AssetDatabase.GetAssetPath(multiContainer)}' saved");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (multiContainer == null)
                {
                    EditorGUIUtility.PingObject(animationSheetData);
                }
                else
                {
                    EditorGUIUtility.PingObject(multiContainer);
                }
            }
        }

        private string GetTextureSavePath(int index, int localIndex)
        {
            var textureName = GetName(saveTextureName, index);
            var additionalText = localIndex == 0 ? string.Empty : "_normal";
            var texturePath = $"{Path.Combine(saveTexturePath, textureName)}{additionalText}.asset";
            return texturePath;
        }

        private string GetMeshSavePath(int index)
        {
            var meshName = skinHeaders[index + 1];
            var texturePath = $"{Path.Combine(saveMeshPath, meshName)}_anim.mesh";
            return texturePath;
        }

        private string GetTextureDataSavePath(int index)
        {
            var textureDataName = GetName(saveTextureDataName, index);
            var assetPath = Path.Combine(saveTextureDataPath, textureDataName) + ".asset";
            return assetPath;
        }

        private void AddClip(Object clipObj)
        {
            if (clipObj == null)
            {
                return;
            }

            if (clipObj is AnimationClip)
            {
                AddClip(clipObj as AnimationClip);
                return;
            }

            if (clipObj is GameObject)
            {
                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(clipObj));

                foreach (Object obj in objects)
                {
                    var clip = obj as AnimationClip;

                    if (clip != null && !clip.name.Contains("_preview_"))
                    {
                        AddClip(clip);
                    }
                }

                return;
            }
        }

        private void AddClip(AnimationClip clip)
        {
            if (HasClip(clip))
            {
                return;
            }

            clips.Add(new ClipData()
            {
                Clip = clip
            });
        }

        private bool HasClip(AnimationClip clip)
        {
            return clips.Count(a => a.Clip == clip) > 0;
        }

        private void SaveData(AnimationTextureDataContainer animationSheetData, TempTextureData tempTextureData, List<Texture2D> textures)
        {
            var newData = new List<AnimationTextureData>();

            for (int i = 0; i < tempTextureData.Data.Count; i++)
            {
                var dataEntry = tempTextureData.Data[i].Clone() as AnimationTextureData;
                newData.Add(dataEntry);
            }

            animationSheetData.TextureDatas = newData;

            for (int i = 0; i < textures.Count; i++)
            {
                animationSheetData.SetTexture(textures[i], i);
            }

            animationSheetData.AtlasWidth = tempTextureData.AtlasWidth;
            animationSheetData.AtlasHeight = tempTextureData.AtlasHeight;
            EditorSaver.SetObjectDirty(animationSheetData);
        }

        private void LoadSheetData()
        {
            for (int i = 0; i < TextureCount; i++)
            {
                LoadSheetData(animationTextureDataContainers[i], i);
            }
        }

        private void LoadSheetData(AnimationTextureDataContainer animationSheetData, int index, bool ensureCapacity = true)
        {
            if (animationSheetData == null)
                return;

            var tempData = GetTempTextureData(index, ensureCapacity);

            tempData.Data.Clear();

            for (int i = 0; i < animationSheetData.TextureDatas.Count; i++)
            {
                var dataEntry = animationSheetData.TextureDatas[i].Clone() as AnimationTextureData;
                tempData.Data.Add(dataEntry);
            }

            var duplicateTextures = CreateTextures(animationSheetData.GetTexture(0).width, animationSheetData.GetTexture(0).height);

            for (int i = 0; i < duplicateTextures.Count; i++)
            {
                Texture2D duplicateTexture = duplicateTextures[i];

                CopyTexture(animationSheetData.GetTexture(i), duplicateTexture);

                tempData.SetTexture(duplicateTexture, i);
            }

            for (int i = 0; i < LocalTextureCount; i++)
            {
                tempData.GetTexture(i).Apply(false);
            }

            tempData.AtlasWidth = animationSheetData.AtlasWidth;
            tempData.AtlasHeight = animationSheetData.AtlasHeight;
        }

        private string GetName(string sourceName, int index)
        {
            if (textureBakeType == TextureBakeType.MultipleTextures)
            {
                switch (namePatternType)
                {
                    case NamePatternType.Index:
                        return $"{sourceName} {index + 1}";
                    case NamePatternType.MeshName:
                        return $"{sourceName} {skins[index].sharedMesh.name}";
                }
            }
            else
            {
                return $"{sourceName} Atlas {index + 1}";
            }

            return sourceName;
        }

        private void CheckForInitialNullData()
        {
            SkinnedMeshRenderer skin = null;

            if (skins?.Count > 0 && skins[0] != null)
            {
                try
                {
                    skin = skins[0].GetComponent<SkinnedMeshRenderer>();
                }
                catch { }
            }

            if (!skin)
            {
                skins.Clear();
                Clear();
            }

            if (sourceSkins.Count > 0)
            {
                try
                {
                    if (sourceSkins[0] == null || sourceSkins[0].GetComponent<Animator>() == null)
                    {
                        sourceSkins.Clear();
                    }
                }
                catch { sourceSkins.Clear(); }
            }

            int index = 0;

            while (index < clips.Count)
            {
                bool isNull = true;

                try
                {
                    if (clips[index] != null && clips[index].Clip != null && clips[index].Clip.length >= 0)
                    {
                        isNull = false;
                    }
                }
                catch { }

                if (isNull)
                {
                    clips.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void DeleteAsset(string assetPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private int GetTextureSize(int sourceSize)
        {
            for (int i = 0; i < TextureSizes.Length; i++)
            {
                if (TextureSizes[i] > sourceSize)
                {
                    return TextureSizes[i];
                }
            }

            return sourceSize;
        }

        private float GetDeltaTime(float time)
        {
            var delta = time - previousTime;
            previousTime = time;
            return delta;
        }

        private void Tick()
        {
            if (!PreviewIsPlaying)
            {
                return;
            }

            if (!TransitionIsPlaying)
            {
                Shader.SetGlobalFloat(Constans.GlobalTime, CurrentTime);
            }
            else
            {
                var dt = GetDeltaTime(CurrentTime);

                playbackTime += dt;

                if (playbackTime >= previewTextureData.ClipLength)
                {
                    playbackTime -= previewTextureData.ClipLength;

                    if (!transitionComplete)
                    {
                        transitionComplete = true;
                        StartPreviewPlayback(nextPreviewAnimIndex, false);
                    }
                    else
                    {
                        transitionComplete = false;

                        switch (transitionType)
                        {
                            case TransitionType.PlayOnce:
                                {
                                    StopPreviewPlayback(true);
                                    StartPreviewPlayback(startPreviewAnimIndex);
                                    break;
                                }
                            case TransitionType.PlayForever:
                                {
                                    StartPreviewPlayback(startPreviewAnimIndex, false);
                                    break;
                                }
                        }
                    }
                }

                tempPreviewMaterial.SetFloat(Constans.PlaybackTime, playbackTime);
            }
        }

        private void CheckForPathFields()
        {
            if (string.IsNullOrEmpty(clipDataTemplatePath))
            {
                clipDataTemplatePath = "Assets/";
            }

            if (string.IsNullOrEmpty(saveTextureDataPath))
            {
                saveTextureDataPath = "Assets/";
            }

            if (string.IsNullOrEmpty(saveTexturePath))
            {
                saveTexturePath = "Assets/";
            }

            if (string.IsNullOrEmpty(saveMeshPath))
            {
                saveMeshPath = "Assets/";
            }

            if (string.IsNullOrEmpty(saveTextureDataName))
            {
                saveTextureDataName = "AnimationTextureData";
            }

            if (string.IsNullOrEmpty(saveTextureName))
            {
                saveTextureName = "AnimationTexture";
            }

            var collectionPath = GetCollectionPath();

            if (!string.IsNullOrEmpty(collectionPath))
            {
                try
                {
                    animationCollection = AssetDatabase.LoadAssetAtPath<AnimationCollectionContainer>(collectionPath);
                }
                catch { }

                if (animationCollection == null)
                {
                    SaveCollectionPath(string.Empty);
                }

                InitBinding();
            }
        }

        private string GetCollectionPath() => EditorPrefs.GetString(GetCollectionKey(), string.Empty);

        private string GetCollectionKey() => AnimationCollectionPathKey;

        private void SaveCollection()
        {
            if (animationCollection == null)
            {
                SaveCollectionPath(string.Empty);
            }
            else
            {
                SaveCollectionPath(AssetDatabase.GetAssetPath(animationCollection));
            }
        }

        private void SaveCollectionPath(string path) => EditorPrefs.SetString(GetCollectionKey(), path);

        private string TryToGetGuid(int index) => index >= 0 && index < animationCollection.GetAnimationCount() ? animationCollection.GetAnimation(index).Guid : string.Empty;

        private int TryToGetIndex(string guid) => !string.IsNullOrEmpty(guid) && guidToIndex.ContainsKey(guid) ? guidToIndex[guid] : -1;

        private void DrawAnimationBinding(Rect r, ClipData clipData)
        {
            if (!animationCollection)
            {
                GUI.enabled = false;
                EditorGUI.Popup(r, "Animation", 0, availableAnimations);
                GUI.enabled = true;
                return;
            }

            var sourceGuid = clipData.Guid;
            var sourceIndex = TryToGetIndex(sourceGuid) + 1;

            var index = EditorGUI.Popup(r, "Animation", sourceIndex, availableAnimations);

            if (index != sourceIndex)
            {
                var realIndex = index - 1;

                string newGuid = TryToGetGuid(realIndex);
                clipData.Guid = newGuid;
            }
        }

        private void InitBinding()
        {
            int count = 0;

            if (animationCollection != null)
            {
                count = animationCollection.GetAnimationCount();
            }

            guidToAnim.Clear();
            guidToIndex.Clear();
            availableAnimations = new string[count + 1];
            availableAnimations[0] = "None";

            if (animationCollection == null)
                return;

            var animations = animationCollection.GetAnimations();

            for (int i = 0; i < count; i++)
            {
                availableAnimations[i + 1] = animations[i].Name;

                guidToAnim.Add(animations[i].Guid, animations[i].Name);
                guidToIndex.Add(animations[i].Guid, i);
            }
        }

        private void InitList()
        {
            reorderableList = new ReorderableList(so, so.FindProperty(nameof(clips)), true, false, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var arrayElement = reorderableList.serializedProperty.GetArrayElementAtIndex(index);

                    var r1 = rect;
                    var r2 = rect;

                    r1.height = GetLineSize();
                    r2.height = GetLineSize();

                    var clipR = r1;
                    var buttonWidth = 60f;

                    clipR.width -= buttonWidth;

                    var clipProp = arrayElement.FindPropertyRelative("Clip");

                    string clipText = string.Empty;

                    var clipData = clips[index];

                    if (clipProp.objectReferenceValue == null)
                    {
                        clipText = "Clip";
                    }
                    else
                    {
                        var frameRate = GetFrameRate(clipData);

                        clipText = $"Clip [Frame Rate {frameRate}]";
                    }

                    if (TransitionMode)
                    {
                        clipR.width -= 30f;
                    }

                    EditorGUI.PropertyField(clipR, clipProp, new GUIContent(clipText));

                    clipR.x += clipR.width;

                    if (TransitionMode)
                    {
                        clipR.x += 2f;
                    }

                    clipR.width = buttonWidth;

                    var buttonIsEnabled = TextureCount > 0;
                    GUI.enabled = buttonIsEnabled;

                    var clipIndex = index;

                    var selectedPreview = clipIndex == selectedPreviewAnimIndex;

                    var oldColor = GUI.backgroundColor;

                    var previewIsPlaying = buttonIsEnabled && PreviewIsPlaying && selectedPreview;

                    if (previewIsPlaying)
                    {
                        GUI.backgroundColor = Color.green;
                    }

                    if (GUI.Button(clipR, "Preview"))
                    {
                        if (!previewIsPlaying)
                        {
                            StartPreviewPlayback(clipIndex);
                        }
                        else
                        {
                            StopPreviewPlayback();
                        }
                    }

                    GUI.backgroundColor = oldColor;

                    GUI.enabled = true;

                    if (TransitionMode)
                    {
                        GUI.enabled = buttonIsEnabled && TransitionIsAvailable;

                        bool transitionSelected = clipIndex == nextPreviewAnimIndex;

                        if (transitionSelected && transitionIsPlaying && buttonIsEnabled)
                        {
                            GUI.backgroundColor = Color.blue;
                        }

                        var clipR2 = clipR;

                        clipR2.x += clipR2.width + 3;
                        clipR2.width = 25;

                        if (GUI.Button(clipR2, "To"))
                        {
                            if (!TransitionIsPlaying || !transitionSelected)
                            {
                                StartPreviewPlayback(clipIndex, transition: true);
                                StartPreviewPlayback(startPreviewAnimIndex, false);
                            }
                            else
                            {
                                StopPreviewPlayback(true);
                            }
                        }

                        GUI.enabled = true;
                    }

                    GUI.backgroundColor = oldColor;

                    if (BindingMode)
                    {
                        r2.y += GetLineOffset();
                        DrawAnimationBinding(r2, clipData);
                    }

                    if (FrameRateSettings)
                    {
                        r2.y += GetLineOffset();

                        var hasCustomFrameRateProp = arrayElement.FindPropertyRelative("HasCustomFrameRate");

                        var toggleR = r2;

                        toggleR.width = 20f;
                        toggleR.x -= 16f;

                        hasCustomFrameRateProp.boolValue = EditorGUI.Toggle(toggleR, hasCustomFrameRateProp.boolValue);

                        if (!hasCustomFrameRateProp.boolValue)
                        {
                            GUI.enabled = false;
                            EditorGUI.IntSlider(r2, "Custom Frame Rate", frameRate, 0, MaxFrameRate);
                            GUI.enabled = true;
                        }
                        else
                        {
                            var customFrameRateProp = arrayElement.FindPropertyRelative("CustomFrameRate");
                            EditorGUI.PropertyField(r2, customFrameRateProp);
                        }

                        var r3 = r2;

                        r3.x += r2.width - 119;
                        r3.width = 120;
                        r3.y += GetLineOffset();

                        var compression = clipData.GetCompressionPerc(frameRate);
                        var compressionText = compression.ToString("00.00");
                        EditorGUI.LabelField(r3, $"Compressed {compressionText}%");

                        r2.y += GetLineOffset();

                        EditorGUI.BeginChangeCheck();

                        EditorGUI.PropertyField(r2, arrayElement.FindPropertyRelative("Interpolate"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            so.ApplyModifiedProperties();
                            OnClipSettingsChanged(index);
                        }
                    }

                    if (CommonSettings)
                    {
                        r2.y += GetLineOffset();

                        var offsetProp = arrayElement.FindPropertyRelative("Offset");
                        var labelWidth = EditorGUIUtility.labelWidth;

                        if (!EditorGUIUtility.wideMode)
                        {
                            EditorGUIUtility.wideMode = true;
                        }

                        EditorGUI.PropertyField(r2, offsetProp);

                        EditorGUIUtility.labelWidth = labelWidth;

                        r2.y += GetLineOffset();

                        EditorGUI.PropertyField(r2, arrayElement.FindPropertyRelative("CustomAnimationName"));
                    }
                },
                elementHeightCallback = (index) =>
                {
                    int fieldCount = 1;

                    if (BindingMode)
                    {
                        fieldCount++;
                    }

                    if (FrameRateSettings)
                    {
                        fieldCount += 2;
                    }

                    if (CommonSettings)
                    {
                        fieldCount += 2;
                    }

                    return GetLineOffset() * fieldCount + FieldOffset + 5f;
                }
            };
        }

        private float GetLineSize() => EditorGUIUtility.singleLineHeight * FieldSizeMultiplier;

        private float GetLineOffset() => GetLineSize() + FieldOffset;

        private void Clear()
        {
            animationTextureDataContainers.Clear();
            textureDatas.Clear();

            CleanTemp();

            tempSkins.Clear();

            selectedTextureIndex = 0;

            if (MultiMeshMode)
            {
                skins.Clear();
            }

            StopPreviewPlayback(true, false);
            StopPreviewPlayback(false, false);
        }

        private void CleanTemp()
        {
            foreach (var combinedSkin in tempSkins)
            {
                if (combinedSkin.TempParent != null)
                {
                    DestroyImmediate(combinedSkin.TempParent);
                }
            }
        }

        private void OnSettingsChanged(SettingsType previousSettings, SettingsType settingsType)
        {
            var previousMulti = previousSettings.HasFlag(SettingsType.Multimesh);
            var currentMulti = settingsType.HasFlag(SettingsType.Multimesh);
            InitList();

            if (!TransitionMode)
            {
                StopPreviewPlayback(true);
            }

            if (currentMulti != previousMulti)
            {
                skinHeaders = null;
                skins.Clear();

                CheckForInitialNullData();


                Clear();
            }
        }

        private void OnClipSettingsChanged(int index)
        {
            if (PreviewIsPlaying && index == selectedPreviewAnimIndex && tempPreviewMaterial)
            {
                var clipData = clips[index];
                tempPreviewMaterial.SetInt(Constans.InterpolateParam, clipData.InterpolateValue);
            }
        }

        private void TextureSettingsChanged()
        {
            if (TextureCount > 0)
            {
                CreateAnimationTexture();
            }
        }

        private void EditorApplication_update()
        {
            Tick();
        }

        #endregion
    }
}
#endif