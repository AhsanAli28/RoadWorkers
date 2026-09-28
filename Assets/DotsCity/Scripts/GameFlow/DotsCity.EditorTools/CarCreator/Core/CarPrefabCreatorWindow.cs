using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.NavMesh.Authoring;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Car.Custom.Authoring;
using Spirit604.DotsCity.Simulation.Factory;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Mono;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.DotsCity.Simulation.TrafficPublic.Authoring;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if !DOTS_SIMULATION
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Authoring;
using Spirit604.Gameplay.Car;
#endif

namespace Spirit604.DotsCity.EditorTools
{
    public class CarPrefabCreatorWindow : EditorWindowBase
    {
        #region Consts

        private const float OpenButtonWidth = 40f;
        private const float PlusButtonSize = 20f;
        private const float EntrySpacing = 5f;
        private const float ScrollTemplateHeight = 300f;
        private const float PreviewSize = 100f;
        private const float HelpboxSpacing = 2f;
        public const int LodCount = 3;
        private bool ShowTabsPanel = false;
        private readonly Vector2 WheelSizeRate = new Vector2(0.9f, 1.1f);

        private const string TRAFFIC_CONVERTER_TEMPLATE_PATH = "Other/Traffic Convert Template.asset";
        private const string PLAYERCAR_CONVERTER_TEMPLATE_PATH = "Other/PlayerCar Convert Template.asset";
        private const string VEHICLE_DATA_FILE_NAME = "VehicleDataCollection";
        private const string CACHE_DATA_FILE_NAME = "Cache Container";
        private const string EmptyID = "None";
        private const string HybridMonoPrefabMessage = "Individually configured in the source Prefab.";

        private readonly string[] AdditionalHeaders = new string[] { "Settings", "Physics", "Graphics" };
        private readonly HashSet<char> Symbols = new HashSet<char>() { '.', '_' };

        private readonly string[] DefaultWheelFRWords = new string[] { "frontright", "front_right", "rightfront", "right_front", "front_r", "fr", "f.r", "f_r", };
        private readonly string[] DefaultWheelFLWords = new string[] { "frontleft", "front_left", "leftfront", "left_front", "front_l", "fl", "f.l", "f_l", };
        private readonly string[] DefaultWheelBRWords = new string[] { "br", "rr", "r.r", "r_r", "rearright", "rear_right", "rightrear", "right_rear", "right_back", "rightback" };
        private readonly string[] DefaultWheelBLWords = new string[] { "bl", "rl", "r.l", "r_l", "rearleft", "rear_left", "leftrear", "left_rear", "left_back", "leftback" };
        private readonly string[] DefaultWheelMWords = new string[] { "ml", "mr", "m.l", "m.r", "m_l", "m_r", "lm", "rm", "mid" };
        private readonly string[] DefaultIgnoreWords = new string[] { "door", "light", "mirror", "glass", "signal", "window", "bump", "caliper", "wiper", "handle", "steer", "carpet", "hood", "disk", "rim", "blink", "lamp" };

        #endregion

        #region Helper types

        private readonly List<WheelType> MandotaryTypes = new List<WheelType>() { WheelType.WheelFR, WheelType.WheelFL, WheelType.WheelBR, WheelType.WheelBL };
        private readonly List<WheelType> OptionalTypes = new List<WheelType>() { WheelType.WheelMiddle };

        [Serializable]
        public class WheelTemplateDictionary : AbstractSerializableDictionary<WheelType, WheelTemplateHolder> { }

        [Serializable]
        public class WheelTemplateHolder
        {
            public List<string> WheelTemplates;
        }

        public enum WheelType { WheelFL, WheelFR, WheelBR, WheelBL, WheelMiddle, None }
        public enum CollectionEditType { AddToExist, Override }
        public enum PlayerEntityType { HybridEntityCustomPhysics, HybridEntityMonoPhysics }

        #endregion

        #region Inspector variables

        [Tooltip("Prefabs will be taken from the selected root from the scene")]
        [SerializeField] private Transform targetPrefabsParent;

        [SerializeField] private VehicleOwnerType carType;
        [SerializeField] private PlayerEntityType playerEntityType = PlayerEntityType.HybridEntityCustomPhysics;
        [SerializeField] private ControllerType controllerType = ControllerType.Arcade;

        [Tooltip("Cached data from vehicle creation")]
        [SerializeField] private CacheContainer cacheContainer;

        [SerializeField] private VehicleDataCollection vehicleDataCollection;

        [Tooltip("Template which contains traffic prefab template")]
        [SerializeField] private CarConvertTemplate trafficConvertTemplate;

        [Tooltip("Template which contains player prefab template")]
        [SerializeField] private CarConvertTemplate playerCarConvertTemplate;

        [SerializeField] private string newCustomMaterialPath = "Assets/";

        [Tooltip("Does the car contain `UnityEngine.AI.NavMeshObstacle` component")]
        [SerializeField] private bool hasNavmeshObstacle;
        [SerializeField] private bool carveStationary;
        [SerializeField][Range(0, 5f)] private float moveThreshold = 0.5f;
        [SerializeField][Range(0, 5f)] private float carvingTimeToStationary = 0.5f;

        [SerializeField] private bool addPlayerComponents = true;

        [Tooltip("Preset will replace an existing preset on scene")]
        [SerializeField] private bool assignNewPresetToScene = true;

        [Tooltip("Project path where to create a new preset")]
        [SerializeField] private string newPresetPath;

        [Tooltip("New preset name")]
        [SerializeField] private string newPresetName = "ExamplePresetName";
        [SerializeField] private EntityType entityType = EntityType.PureEntitySimplePhysics;
        [SerializeField] private bool allowOverride;

        [SerializeField] private string trafficHullSavePath;
        [SerializeField] private string playerHullSavePath;
        [SerializeField] private string trafficSavePath;
        [SerializeField] private string playerSavePath;
        [SerializeField] private bool cloneHullPrefab;
        [SerializeField] private List<string> hullNameTemplates = new List<string>() { "hull", "body", "chassis" };
        [SerializeField] private bool showWheelTemplate;

        [SerializeField]
        private WheelTemplateDictionary wheelNameTemplates = new WheelTemplateDictionary
        {
            { WheelType.WheelFR, new WheelTemplateHolder(){ WheelTemplates = new List<string>() } },
            { WheelType.WheelFL, new WheelTemplateHolder(){ WheelTemplates = new List<string>() } },
            { WheelType.WheelBR, new WheelTemplateHolder(){ WheelTemplates = new List<string>() } },
            { WheelType.WheelBL, new WheelTemplateHolder(){ WheelTemplates = new List<string>() } },
            { WheelType.WheelMiddle, new WheelTemplateHolder(){ WheelTemplates = new List<string>() } },
        };

        [SerializeField] private List<string> ignoreWords = new List<string>();
        [SerializeField] private bool showPreview = true;
        [SerializeField] private bool showAdditionalSettings = true;
        [SerializeField] private bool showCustomSettings = true;
        [SerializeField] private bool pingFolderAfterCreation = true;
        [SerializeField] private int additionalTabIndex;
        [SerializeField] private List<CarPrefabInfo> prefabsInfo = new List<CarPrefabInfo>();
        [SerializeField] private int selectedUIIndex;
        [SerializeField] private int selectedTabIndex;
        [SerializeField] private bool prefabSettingsFoldout = true;
        [SerializeField] private bool commonSettingsFoldout = true;
        [SerializeField] private bool saveSettingsFoldout = true;
        [SerializeField] private bool templateSettingsFoldout = true;
        [SerializeField] private bool previewSettingsFoldout = true;
        [SerializeField] private bool additionalSettingsFoldout = true;
        [SerializeField] private bool prefabInfoFoldout = true;
        [SerializeField] private CollectionEditType collectionEditType;
        [SerializeField] private string[] availableIds = new string[] { EmptyID };

        #endregion

        #region Variables

        private Vector2 sourcePrefabScrollPosition;
        private Vector2 prefabScrollPosition;
        private Vector2 templateScrollPosition;
        private ReorderableList reorderableList;
        private SerializedObject so;
        private SerializedObject cacheSo;
        private string[] texts;
        private string[] tabHeaders;
        private Action[] tabs;
        private string[] templateHeaders;
        private VehicleCustomTemplate[] templates;
        private MeshRenderer[] hullLods;
        private MeshRenderer[][] wheelLods;
        private string latestCache;
        private Dictionary<string, CarPrefabInfo> idToSettings = new Dictionary<string, CarPrefabInfo>();
        private HashSet<GameObject> duplicateIds = new HashSet<GameObject>();
        private string[] idHeaders;
        private Dictionary<string, int> idBinding = new Dictionary<string, int>();
        private bool scanned;

        #endregion

        #region Properties

        private List<GameObject> Prefabs
        {
            get => cacheContainer?.Prefabs ?? null;
            set
            {
                if (cacheContainer && cacheContainer.Prefabs != value)
                {
                    cacheContainer.Prefabs = value;
                    EditorSaver.SetObjectDirty(cacheContainer);
                }
            }
        }

        private VehicleOwnerType CurrentOwnerType
        {
            get
            {
#if DOTS_SIMULATION
                return VehicleOwnerType.Traffic;
#else
                return carType;
#endif
            }
        }

        private bool IsPlayerOwner => CurrentOwnerType == VehicleOwnerType.Player;

        private MaterialType CurrentMaterialType => cacheContainer.materialType;

        private ScanIDSourceType CurrentScanIDSourceType => cacheContainer.scanIDSourceType;

        private WheelSearchType WheelSearchType => cacheContainer.wheelSearchType;

        private bool FitPhysicsShapeToMesh => cacheContainer.fitPhysicsShapeToMesh;

        private bool IncludeWheels => cacheContainer.IncludeWheels(EntityType);

        private bool PhysicsShapeAtFloor => cacheContainer.physicsShapeAtFloor;

        private bool HasWheels => cacheContainer.hasWheels && !UserCustomVehicle;

        private bool AddOffset => cacheContainer.GetAddOfset(EntityType);

        private bool FixPivot => cacheContainer.GetFixPivot(EntityType);

        private bool AddWheelOffset => cacheContainer.GetAddWheelOffset(EntityType);

        private float LocalOffset => cacheContainer.GetLocalOffset(EntityType);

        private TrafficCarPoolPreset SelectedPreset => cacheContainer.GetSelectedPreset(EntityType, IsPlayerOwner);

        private bool AddToExistPreset => cacheContainer.addToExistPreset;

        private PresetSourceType CurrentPresetSourceType => cacheContainer.presetSourceType;

        private Material CustomAtlasMaterial
        {
            get => cacheContainer?.CustomMaterial ?? null;
            set
            {
                if (cacheContainer && cacheContainer.CustomMaterial != value)
                {
                    cacheContainer.CustomMaterial = value;
                    EditorSaver.SetObjectDirty(cacheContainer);
                }
            }
        }

        private string SaveHullPath => CurrentOwnerType == VehicleOwnerType.Traffic ? trafficHullSavePath : playerHullSavePath;

        private string SavePath => CurrentOwnerType == VehicleOwnerType.Traffic ? trafficSavePath : playerSavePath;

        private bool CustomPhysics => IsCustomPhysics(EntityType) || HybridMono && !UserCustomVehicle;

        private bool IsCustomPhysics(EntityType entityType) => cacheContainer.IsCustom(entityType);

        private bool HasTemplateSettings => CustomPhysics && !HybridMono;

        private bool SharedWheel => WheelSourceType != WheelMeshSourceType.ModelUnique;

        private WheelMeshSourceType WheelSourceType => cacheContainer.WheelSourceType;

        private WheelRotationType CurrentWheelRotationType => cacheContainer.WheelRotationType;

        private Mesh SharedWheelMesh => cacheContainer.SharedWheelMesh;

        private Mesh SharedWheelMeshLOD1 => cacheContainer.SharedWheelMeshLOD1;

        private Mesh SharedWheelMeshLOD2 => cacheContainer.SharedWheelMeshLOD2;

        private bool HasLods => cacheContainer.HasLods;

        private float Lod0ScreenSize => cacheContainer.Lod0ScreenSize;

        private float Lod1ScreenSize => cacheContainer.Lod1ScreenSize;

        private float Lod2ScreenSize => cacheContainer.Lod2ScreenSize;

        private float WheelRadius { get => cacheContainer.WheelRadius; set => cacheContainer.WheelRadius = value; }

        private float WheelOffset { get => cacheContainer.GetWheelOffset(EntityType); set => cacheContainer.SetWheelOffset(EntityType, value); }

        private float SuspensionLength { get => cacheContainer.SuspensionLength; set => cacheContainer.SuspensionLength = value; }

        private float AdditiveOffset { get => cacheContainer.additiveOffset; set => cacheContainer.additiveOffset = value; }

        private float MaxSteeringAngle { get => cacheContainer.maxSteeringAngle; set => cacheContainer.maxSteeringAngle = value; }

        private Vector3 SizeOffset { get => cacheContainer.SizeOffset; set => cacheContainer.SizeOffset = value; }

        private Vector3 CenterOffset { get => cacheContainer.CenterOffset; set => cacheContainer.CenterOffset = value; }

        private Vector3 CenterOfMass { get => cacheContainer.CenterOfMass; set => cacheContainer.CenterOfMass = value; }

        private float BevelRadius { get => cacheContainer.BevelRadius; set => cacheContainer.BevelRadius = value; }

        private float Mass { get => cacheContainer.GetMass(EntityType); set => cacheContainer.SetMass(EntityType, value); }

        private bool HybridMono => EntityType == EntityType.HybridEntityMonoPhysics;

        private bool Hybrid => EntityType == EntityType.HybridEntityMonoPhysics || EntityType == EntityType.HybridEntitySimplePhysics || EntityType == EntityType.HybridEntityCustomPhysics;

        private EntityType EntityType => !IsPlayerOwner ? entityType : CurrentPlayerEntityType;

        private EntityType CurrentPlayerEntityType
        {
            get
            {
                if (playerEntityType == PlayerEntityType.HybridEntityMonoPhysics)
                {
                    return EntityType.HybridEntityMonoPhysics;
                }

                return EntityType.HybridEntityCustomPhysics;
            }
        }

        private bool AddPlayerComponents => addPlayerComponents && IsPlayerOwner && HybridMono;

        private bool CreateAvailable => scanned;

        private bool ShowCloneHull => Hybrid && !HybridMono;

        private bool CloneHull => cloneHullPrefab && ShowCloneHull || HybridMono && !UserController;

        private bool UserCustomVehicle => HybridMono && UserController;

        private bool UserController => controllerType == ControllerType.CustomUser;

        #endregion

        #region Unity lifecycle

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Car Prefab Creator")]
        public static CarPrefabCreatorWindow ShowWindow()
        {
            CarPrefabCreatorWindow mapTileSetup = (CarPrefabCreatorWindow)GetWindow(typeof(CarPrefabCreatorWindow));
            mapTileSetup.titleContent = new GUIContent("Car Prefab Creator");

            return mapTileSetup;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(450, 500);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadData();
            TryToLoadConfig();
            so = new SerializedObject(this);

            if (cacheContainer)
            {
                cacheSo = new SerializedObject(cacheContainer);
            }

            texts = new string[] { "Toolbar", "Tabs" };
            tabHeaders = new string[] { "Prefab", "Common", "Save", "Template", "Preview", "Additional", "Prefab Info" };

            tabs = new Action[]
            {
                () =>
                {
                    InspectorExtension.DrawHelpBox(()=>
                    {
                        DrawPrefabSettingsContent();
                    });
                },
                () =>
                {
                    InspectorExtension.DrawHelpBox(()=>
                    {
                        DrawCommonSettingsContent();
                    });
                },
                () =>
                {
                    DrawSaveSettingsContent();
                },
                () =>
                {
                    InspectorExtension.DrawHelpBox(()=>
                    {
                        DrawTemplateSettingsContent();
                    });
                },
                () =>
                {
                    InspectorExtension.DrawHelpBox(()=>
                    {
                        DrawPreviewSettingsContent();
                    });
                },
                () =>
                {
                    InspectorExtension.DrawHelpBox(()=>
                    {
                        DrawAdditionalSettingsContent();
                    });
                },
                () =>
                {
                    var newTabIndex = GUILayout.Toolbar(additionalTabIndex, AdditionalHeaders);

                    if (newTabIndex != additionalTabIndex)
                    {
                        additionalTabIndex = newTabIndex;
                        InitList();
                    }

                    DrawPrefabInfoSettingsContent();
                },
            };

            VehicleCustomTemplateContainer.LoadPresets(out templates, out var localTemplateHeaders);

            var tempList = new List<string>();
            tempList.Add("None");

            if (localTemplateHeaders?.Length > 0)
            {
                tempList.AddRange(localTemplateHeaders);
            }

            templateHeaders = tempList.ToArray();

            InitList();

            if (cacheContainer != null)
            {
                var current = cacheContainer.name;

                if (latestCache != current)
                {
                    latestCache = current;

                    if (Prefabs.Count > 0)
                    {
                        Scan();
                    }
                }
            }

            UpdateCollectionHeaders();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveData();
        }

        #endregion

        #region GUI methods

        private void OnGUI()
        {
            so.Update();
            cacheSo?.Update();

            if (cacheContainer == null)
            {
                DrawCachedContainerProp();
                EditorGUILayout.HelpBox("Assign or create cached container", MessageType.Error);
                so.ApplyModifiedProperties();
                return;
            }

            if (ShowTabsPanel)
            {
                selectedUIIndex = GUILayout.Toolbar(selectedUIIndex, texts);
            }
            else
            {
                selectedUIIndex = 0;
            }

            switch (selectedUIIndex)
            {
                case 0:
                    {
                        selectedTabIndex = GUILayout.SelectionGrid(selectedTabIndex, tabHeaders, 3);

                        if (tabs?.Length > selectedTabIndex && selectedTabIndex >= 0)
                            tabs[selectedTabIndex].Invoke();

                        break;
                    }
                case 1:
                    {
                        DrawPrefabSettings();

                        DrawCommonSettings();

                        DrawSaveSettings();

                        DrawTemplateSettings();

                        DrawPreviewSettings();

                        DrawAdditionalSettings();

                        DrawPrefabInfoSettings();

                        break;
                    }
            }

            so.ApplyModifiedProperties();
            cacheSo.ApplyModifiedProperties();

            DrawButtons();
        }

        private void DrawPrefabSettings()
        {
            Action prefabSettingsCallback = () =>
            {
                DrawPrefabSettingsContent();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Prefab Settings", prefabSettingsCallback, ref prefabSettingsFoldout);
        }

        private void DrawPrefabSettingsContent(bool drawInnerBox = false)
        {
            var prefabsProp = cacheSo.FindProperty(nameof(cacheContainer.Prefabs));

            var isExpanded = prefabsProp.isExpanded;

            if (isExpanded)
            {
                const float maxHeight = 300f;
                float step = EditorGUIUtility.singleLineHeight + 7f;
                float height = Mathf.Clamp(cacheContainer.Prefabs.Count * step + 50, step + 50, maxHeight);

                sourcePrefabScrollPosition = EditorGUILayout.BeginScrollView(sourcePrefabScrollPosition, GUILayout.MinHeight(100f), GUILayout.Height(height));
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(prefabsProp);

            if (EditorGUI.EndChangeCheck())
            {
                scanned = false;
            }

            if (isExpanded)
            {
                EditorGUILayout.EndScrollView();
            }

#if !DOTS_SIMULATION
            EditorGUILayout.PropertyField(so.FindProperty(nameof(carType)));
#endif

            DrawCachedContainerProp();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(vehicleDataCollection)));

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                UpdateCollectionHeaders();
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficConvertTemplate)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(playerCarConvertTemplate)));
        }

        private void DrawCommonSettings()
        {
            Action commonSettingsCallback = () =>
            {
                DrawCommonSettingsContent();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Common Settings", commonSettingsCallback, ref commonSettingsFoldout);
        }

        private void DrawCommonSettingsContent()
        {
            if (!HybridMono)
            {
                cacheContainer.DrawCommonSettings(cacheSo, EntityType);
            }
            else
            {
                cacheContainer.DrawMonoCommonSettings(cacheSo, EntityType);
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(hasNavmeshObstacle)));

            if (hasNavmeshObstacle)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(moveThreshold)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(carveStationary)));

                if (carveStationary)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(carvingTimeToStationary)));
                }

                EditorGUI.indentLevel--;
            }

            if (!UserCustomVehicle)
            {
                cacheContainer.DrawOffsetSettings(cacheSo, EntityType);
            }

            if (IsPlayerOwner)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(addPlayerComponents)));
            }
        }

        private void DrawSaveSettings()
        {
            Action saveSettingsCallback = () =>
            {
                DrawSaveSettingsContent();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Save Settings", saveSettingsCallback, ref saveSettingsFoldout);
        }

        private void DrawSaveSettingsContent()
        {
            GUILayout.BeginVertical("GroupBox");

#if !DOTS_SIMULATION
            EditorGUILayout.PropertyField(so.FindProperty(nameof(carType)));
#endif

            EditorGUI.BeginChangeCheck();

            if (!IsPlayerOwner)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(entityType)));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(playerEntityType)), new GUIContent("Entity Type"));
            }

            if (HybridMono)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(controllerType)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                Scan();
                InitList();
                Repaint();
            }

            if (UserCustomVehicle)
            {
                EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.InputScript)));
            }

            if (ShowCloneHull)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(cloneHullPrefab)));
            }

            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.addToExistPreset)));

            if (!AddToExistPreset)
            {
                EditorGUILayout.LabelField("New Preset Settings", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(assignNewPresetToScene)));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty(nameof(newPresetPath)));

                if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
                {
                    AssetDatabaseExtension.SelectProjectFolder(newPresetPath);
                }

                if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                {
                    AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new preset path", ref newPresetPath, newPresetPath);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty(nameof(newPresetName)));

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.presetSourceType)));

                switch (CurrentPresetSourceType)
                {
                    case PresetSourceType.Selected:
                        cacheContainer.DrawPresetSettings(cacheSo, EntityType, IsPlayerOwner);
                        break;
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(so.FindProperty(nameof(allowOverride)));

            switch (CurrentOwnerType)
            {
                case VehicleOwnerType.Traffic:
                    {
                        if (CloneHull)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficHullSavePath)));

                            if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
                            {
                                AssetDatabaseExtension.SelectProjectFolder(trafficHullSavePath);
                            }

                            if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                            {
                                AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new traffic hull save path", ref trafficHullSavePath, trafficHullSavePath);
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficSavePath)));

                        if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
                        {
                            AssetDatabaseExtension.SelectProjectFolder(trafficSavePath);
                        }

                        if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                        {
                            AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new traffic save path", ref trafficSavePath, trafficSavePath);
                        }

                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                case VehicleOwnerType.Player:
                    {
                        if (CloneHull)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(so.FindProperty(nameof(playerHullSavePath)));

                            if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
                            {
                                AssetDatabaseExtension.SelectProjectFolder(playerHullSavePath);
                            }

                            if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                            {
                                AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new traffic hull save path", ref playerHullSavePath, playerHullSavePath);
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.PropertyField(so.FindProperty(nameof(playerSavePath)));

                        if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
                        {
                            AssetDatabaseExtension.SelectProjectFolder(playerSavePath);
                        }

                        if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                        {
                            AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new player save path", ref playerSavePath, playerSavePath);
                        }

                        EditorGUILayout.EndHorizontal();

                        break;
                    }
            }

            cacheContainer.DrawTemplateSettings(cacheSo, EntityType, IsPlayerOwner, CloneHull);

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(so.FindProperty(nameof(collectionEditType)));

            GUILayout.EndVertical();


            GUILayout.BeginVertical("GroupBox");

            if (!UserController)
            {
                EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.materialType)));

                switch (CurrentMaterialType)
                {
                    case MaterialType.CustomAtlas:
                        {
                            DrawMaterialProperty("Custom Atlas Material");
                            break;
                        }
                    case MaterialType.NewUniqueMaterial:
                        {
                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.PropertyField(so.FindProperty(nameof(newCustomMaterialPath)));

                            if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
                            {
                                AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new material path", ref newCustomMaterialPath, newCustomMaterialPath);
                            }

                            EditorGUILayout.EndHorizontal();

                            DrawMaterialProperty("Template Material");
                            break;
                        }
                }

            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.EnumPopup("Material Type", MaterialType.Source);
                GUI.enabled = true;
            }

            GUILayout.EndVertical();
        }

        private void DrawCachedContainerProp()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(cacheContainer)));

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();

                if (cacheContainer)
                {
                    cacheSo = new SerializedObject(cacheContainer);
                }
            }
        }

        private void DrawMaterialProperty(string label)
        {
            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.CustomMaterial)), new GUIContent(label));
        }

        private void DrawTemplateSettings()
        {
            Action templateSettingsCallback = () =>
            {
                DrawTemplateSettingsContent();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Template Settings", templateSettingsCallback, ref templateSettingsFoldout);
        }

        private void DrawTemplateSettingsContent()
        {
            if (selectedUIIndex == 0)
            {
                templateScrollPosition = EditorGUILayout.BeginScrollView(templateScrollPosition, GUILayout.Height(ScrollTemplateHeight));
            }
            else
            {
                templateScrollPosition = EditorGUILayout.BeginScrollView(templateScrollPosition);
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(hullNameTemplates)), true);

            GUI.enabled = HasWheels;

            EditorGUILayout.PropertyField(so.FindProperty(nameof(showWheelTemplate)));

            if (showWheelTemplate)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(wheelNameTemplates)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(ignoreWords)));
            }

            GUI.enabled = true;

            if (!HasWheels)
            {
                EditorGUILayout.HelpBox("Wheel search disabled.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Preview Settings", () =>
            {
                DrawPreviewSettingsContent();

            }, ref previewSettingsFoldout);
        }

        private void DrawPreviewSettingsContent()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(showPreview)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(showAdditionalSettings)));

            EditorGUILayout.PropertyField(so.FindProperty(nameof(showCustomSettings)));

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                InitList();
                Repaint();
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(pingFolderAfterCreation)));
        }

        private void DrawAdditionalSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Additional Settings", () =>
            {
                DrawAdditionalSettingsContent();

            }, ref additionalSettingsFoldout);
        }

        private void DrawAdditionalSettingsContent()
        {
            var newTabIndex = GUILayout.Toolbar(additionalTabIndex, AdditionalHeaders);

            if (newTabIndex != additionalTabIndex)
            {
                additionalTabIndex = newTabIndex;
                InitList();
            }

            switch (additionalTabIndex)
            {
                case 0:
                    {
                        if (!UserCustomVehicle)
                        {
                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.wheelRadius)));

                            DrawApplyButton(() =>
                            {
                                ApplyWheelSettings(0);
                            });

                            if (EditorGUI.EndChangeCheck())
                            {
                                so.ApplyModifiedProperties();
                                ApplyWheelSettings(0);
                                Repaint();
                            }

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            cacheContainer.DrawWheelOffsetSettings(cacheSo, EntityType);

                            if (EditorGUI.EndChangeCheck())
                            {
                                so.ApplyModifiedProperties();
                                ApplyWheelSettings(1);
                                Repaint();
                            }

                            DrawApplyButton(() =>
                            {
                                ApplyWheelSettings(1);
                            });

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            GUI.enabled = CustomPhysics;

                            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.suspensionLength)));

                            DrawApplyButton(() =>
                            {
                                ApplyWheelSettings(2);
                            });

                            GUI.enabled = true;

                            if (EditorGUI.EndChangeCheck())
                            {
                                so.ApplyModifiedProperties();
                                ApplyWheelSettings(2);
                                Repaint();
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.additiveOffset)));

                            DrawApplyButton(() =>
                            {
                                ApplyWheelSettings(3);
                            });

                            if (EditorGUI.EndChangeCheck())
                            {
                                so.ApplyModifiedProperties();
                                ApplyWheelSettings(3);
                                Repaint();
                            }

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty(nameof(cacheContainer.maxSteeringAngle)));

                            DrawApplyButton(() =>
                            {
                                ApplyWheelSettings(4);
                            });

                            if (EditorGUI.EndChangeCheck())
                            {
                                so.ApplyModifiedProperties();
                                ApplyWheelSettings(4);
                                Repaint();
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Apply Settings"))
                        {
                            if (!HybridMono)
                            {
                                ApplyWheelSettings(0);
                                ApplyWheelSettings(1);
                                ApplyWheelSettings(2);
                            }
                            else
                            {
                                if (!UserController)
                                {
                                    ApplyWheelSettings(0);
                                    ApplyWheelSettings(1);
                                    ApplyWheelSettings(2);
                                }

                                ApplyWheelSettings(3);
                                ApplyWheelSettings(4);
                            }
                        }

                        break;
                    }
                case 1:
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(cacheSo.FindProperty("SizeOffset"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            cacheSo.ApplyModifiedProperties();
                            ApplyPhysicsSettings(0);
                        }

                        DrawApplyButton(() =>
                        {
                            ApplyPhysicsSettings(0);
                        });

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(cacheSo.FindProperty("CenterOffset"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            cacheSo.ApplyModifiedProperties();
                            ApplyPhysicsSettings(1);
                        }

                        DrawApplyButton(() =>
                        {
                            ApplyPhysicsSettings(1);
                        });

                        EditorGUILayout.EndHorizontal();

                        if (!HybridMono)
                        {
                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty("CenterOfMass"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                cacheSo.ApplyModifiedProperties();
                                ApplyPhysicsSettings(2);
                            }

                            DrawApplyButton(() =>
                            {
                                ApplyPhysicsSettings(2);
                            });

                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty("BevelRadius"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                cacheSo.ApplyModifiedProperties();
                                ApplyPhysicsSettings(3);
                            }

                            DrawApplyButton(() =>
                            {
                                ApplyPhysicsSettings(3);
                            });

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUI.BeginChangeCheck();

                            cacheContainer.DrawPhysicsSettings(cacheSo, EntityType);

                            if (EditorGUI.EndChangeCheck())
                            {
                                cacheSo.ApplyModifiedProperties();
                                ApplyPhysicsSettings(4);
                            }

                            DrawApplyButton(() =>
                            {
                                ApplyPhysicsSettings(4);
                            });

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("Apply Settings"))
                        {
                            ApplyPhysicsSettings(0);
                            ApplyPhysicsSettings(1);

                            if (!HybridMono)
                            {
                                ApplyPhysicsSettings(3);
                                ApplyPhysicsSettings(4);
                            }
                        }

                        break;
                    }
                case 2:
                    {
                        if (!UserCustomVehicle)
                        {
                            if (HasWheels)
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(cacheSo.FindProperty("WheelSourceType"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    cacheSo.ApplyModifiedProperties();
                                    InitWheelMesh();
                                    InitList();
                                }

                                if (SharedWheel && WheelSourceType == WheelMeshSourceType.SharedAll)
                                {
                                    EditorGUILayout.PropertyField(cacheSo.FindProperty("WheelRotationType"));
                                }

                                if (WheelSourceType == WheelMeshSourceType.SharedAll)
                                {
                                    EditorGUI.BeginChangeCheck();

                                    EditorGUILayout.PropertyField(cacheSo.FindProperty("SharedWheelMesh"));

                                    if (HasLods)
                                    {
                                        EditorGUILayout.PropertyField(cacheSo.FindProperty("SharedWheelMeshLOD1"));
                                        EditorGUILayout.PropertyField(cacheSo.FindProperty("SharedWheelMeshLOD2"));
                                    }

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        cacheSo.ApplyModifiedProperties();
                                        InitWheelMesh();
                                    }
                                }
                            }

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(cacheSo.FindProperty("HasLods"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                cacheSo.ApplyModifiedProperties();
                                InitLods();
                                InitList();
                            }

                            if (HasLods)
                            {
                                EditorGUILayout.PropertyField(cacheSo.FindProperty("Lod0ScreenSize"));
                                EditorGUILayout.PropertyField(cacheSo.FindProperty("Lod1ScreenSize"));
                                EditorGUILayout.PropertyField(cacheSo.FindProperty("Lod2ScreenSize"));
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(HybridMonoPrefabMessage, MessageType.Info);
                        }

                        break;
                    }
            }
        }

        private void DrawPrefabInfoSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Prefab Info", () =>
            {
                DrawPrefabInfoSettingsContent();

            }, ref prefabInfoFoldout);
        }

        private void DrawPrefabInfoSettingsContent()
        {
            prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition);

            EditorGUI.BeginChangeCheck();

            reorderableList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawButtons()
        {
            if (GUILayout.Button("Scan"))
            {
                Scan();
            }

            GUI.enabled = CreateAvailable;

            if (GUILayout.Button("Create"))
            {
                Create();
            }

            GUI.enabled = true;
        }

        private void DrawApplyButton(Action onClick)
        {
            var icon = EditorGUIUtility.IconContent("Animation.Play");

            if (GUILayout.Button(icon, GUILayout.Width(25f)))
            {
                onClick();
            }
        }

        #endregion

        #region Public methods

        public void Scan()
        {
            SaveCache();

            prefabsInfo.Clear();

            Prefabs = Prefabs.Distinct().ToList();

            for (int i = 0; i < Prefabs?.Count; i++)
            {
                string newEnumType = Prefabs[i].name;

                newEnumType = ValidateName(newEnumType);

                var trafficAuthoring = Prefabs[i].GetComponent<TrafficCarEntityAuthoring>();

                var mesh = TryToGetHull(Prefabs[i], true);
                var id = GetID(Prefabs[i]);

                List<MeshRenderer> wheels = null;
                List<MeshRenderer> extraWheels = null;

                if (HasWheels)
                {
                    FindWheels(Prefabs[i], out wheels, out extraWheels);
                }
                else
                {
                    wheels = new List<MeshRenderer>();
                    extraWheels = new List<MeshRenderer>();
                }

                if (UserCustomVehicle)
                {
                    var rb = Prefabs[i].GetComponentInChildren<Rigidbody>();

                    if (rb == null)
                    {
                        Debug.Log($"Prefab {Prefabs[i].name}. Rigidbody not found. Make sure you have added a prefab with custom monobehaviour vehicle controller or change player simulation type to Hybrid DOTS.");
                    }
                }

                var prefabInfo = new CarPrefabInfo()
                {
                    Prefab = Prefabs[i],
                    ID = id,
                    Name = newEnumType,
                    SourceMesh = mesh,
                    AllWheels = wheels,
                    AllWheelsMeshes = wheels.Where(a => a.GetComponent<MeshFilter>() != null).Select(a => a.GetComponent<MeshFilter>().sharedMesh).ToList(),
                    ExtraWheels = extraWheels,
                    AvailableWheels = wheels.Select(a => a.GetComponent<MeshFilter>().sharedMesh.name).ToArray(),
                };

                prefabInfo.UpdateInternalID();

                prefabsInfo.Add(prefabInfo);
            }

            LoadCache();
            LoadSettings();
            LoadAdditionalSettings();
            InitLods();
            InitWheelMesh();
            InitList();

            scanned = true;
        }

        public void Create()
        {
            bool created = false;

            if (!HybridMono)
            {
                created = CreateDefault();
            }
            else
            {
                created = CreateHybrid();
            }

            if (created)
            {
                PostCreation();
            }
        }

        public bool CreateDefault()
        {
            var templatePrefab = GetTemplate();

            if (templatePrefab == null)
            {
                Debug.Log($"Template {EntityType} doesn't exist");
                return false;
            }

            if (collectionEditType == CollectionEditType.Override)
            {
                vehicleDataCollection.ClearData(false);
            }

            for (int i = 0; i < prefabsInfo?.Count; i++)
            {
                var prefabInfoData = prefabsInfo[i];
                var sourcePrefabInfoSettings = TryToGetClonePrefab(prefabInfoData);

                var carScenePrefab = prefabInfoData.Prefab;

                var currentTrafficEntityAuthoring = carScenePrefab.GetComponent<TrafficCarEntityAuthoring>();
                var prefabHasComponents = currentTrafficEntityAuthoring != null;

                GameObject carTemplatePrefab = null;
                GameObject unpackedPrefab = null;

                MeshRenderer hullMesh = null;

                if (!prefabHasComponents)
                {
                    hullMesh = TryToGetHull(carScenePrefab, true);

                    if (!hullMesh)
                        continue;
                }

                if (!prefabHasComponents)
                {
                    carTemplatePrefab = GetCopyTemplatePrefab(prefabInfoData);
                    unpackedPrefab = Instantiate(carScenePrefab);

                    unpackedPrefab.name = carScenePrefab.name;
                    carTemplatePrefab.name = unpackedPrefab.name;
                    carTemplatePrefab.transform.SetAsLastSibling();

                    unpackedPrefab.transform.SetParent(carTemplatePrefab.transform);
                    unpackedPrefab.transform.localPosition = default;
                    unpackedPrefab.transform.localRotation = default;
                }
                else
                {
                    carTemplatePrefab = carScenePrefab;
                    unpackedPrefab = carTemplatePrefab;
                }

                var physicsShape = carTemplatePrefab.gameObject.GetComponentInChildren<PhysicsShapeAuthoring>();
                var physicsBody = carTemplatePrefab.gameObject.GetComponentInChildren<PhysicsBodyAuthoring>();

                hullMesh = TryToGetHull(unpackedPrefab);

                if (!hullMesh)
                    continue;

                if (physicsBody)
                {
                    physicsBody.Mass = sourcePrefabInfoSettings.Mass.Value;

                    physicsBody.CustomMassDistribution = new Unity.Physics.MassDistribution()
                    {
                        Transform = new RigidTransform(Quaternion.identity, sourcePrefabInfoSettings.CenterOfMass.Value),
                        InertiaTensor = new float3(1, 1, 1)
                    };
                }

                if (HasLods)
                {
                    if (hullLods == null || hullLods.Length != LodCount)
                    {
                        hullLods = new MeshRenderer[LodCount];
                    }

                    for (int j = 0; j < LodCount; j++)
                    {
                        hullLods[j] = null;
                    }

                    hullLods[0] = hullMesh;

                    for (int j = 1; j < LodCount; j++)
                    {
                        var newHullMeshRenderer = CreateNewMesh(prefabInfoData.HullMeshLODs[j].name, hullMesh.sharedMaterial, prefabInfoData.HullMeshLODs[j]);
                        newHullMeshRenderer.transform.SetParent(carTemplatePrefab.transform);

                        hullLods[j] = newHullMeshRenderer;
                    }
                }
                else
                {
                    if (hullLods == null)
                    {
                        hullLods = new MeshRenderer[0];
                    }
                }

                var collider = unpackedPrefab.GetComponentInChildren<Collider>();

                if (collider)
                {
                    DestroyImmediate(collider);
                }

                List<MeshRenderer> wheels = new List<MeshRenderer>();
                bool allWheelsFound = false;

                if (HasWheels)
                {
                    FindWheels(unpackedPrefab, out wheels, out var extraWheels);

                    UpdateDefaultLOD(prefabInfoData, carTemplatePrefab, hullMesh, wheels);

                    var extraWheelsCount = extraWheels?.Count ?? 0;
                    allWheelsFound = wheels.Count - extraWheelsCount == MandotaryTypes.Count;

                    if (allWheelsFound)
                    {
                        EntityType currentEntityType = GetEntityType(sourcePrefabInfoSettings);
                        var customPhysics = IsCustomPhysics(currentEntityType);

                        IVehicleAuthoring vehicleAuthoring = null;

                        if (!customPhysics)
                        {
                            vehicleAuthoring = carTemplatePrefab.GetOrCreateComponent<CarWheelAuthoring>() as IVehicleAuthoring;
                        }
                        else
                        {
                            vehicleAuthoring = carTemplatePrefab.GetComponent<IVehicleAuthoring>();
                        }

                        if (vehicleAuthoring != null)
                        {
                            if (HasLods)
                            {
                                wheelLods = new MeshRenderer[LodCount][];

                                for (int j = 0; j < wheelLods.GetLength(0); j++)
                                {
                                    wheelLods[j] = new MeshRenderer[wheels.Count];
                                }
                            }

                            for (int j = 0; j < wheels.Count; j++)
                            {
                                MeshRenderer wheel = wheels[j];

                                ApplyWheelOffset(sourcePrefabInfoSettings, wheel);
                                SetWheelMesh(prefabInfoData, carTemplatePrefab.transform, wheel);

                                if (HasLods)
                                {
                                    wheelLods[0][j] = wheel;

                                    if (SharedWheel)
                                    {
                                        for (int k = 1; k < LodCount; k++)
                                        {
                                            var wheelMesh = prefabInfoData.SharedWheelMesh[k];

                                            if (wheelMesh)
                                            {
                                                var newWheel = CreateNewMesh(wheel.name, wheel.sharedMaterial, wheelMesh);

                                                var lodWheelParent = hullLods[k]?.transform ?? carTemplatePrefab.transform;

                                                newWheel.transform.SetParent(lodWheelParent);
                                                newWheel.transform.position = wheel.transform.position;
                                                newWheel.transform.rotation = wheel.transform.rotation;

                                                wheelLods[k][j] = newWheel;
                                            }
                                        }
                                    }
                                }

                                var wheelParent = wheel.gameObject;

                                if (vehicleAuthoring != null)
                                {
                                    if (j <= 1)
                                    {
                                        vehicleAuthoring.AddSteeringWheel(wheelParent);
                                    }

                                    vehicleAuthoring.InsertWheel(wheelParent, j);
                                }
                            }

                            var customVehicleAuthoring = vehicleAuthoring as VehicleAuthoring;

                            if (customVehicleAuthoring)
                            {
                                var currentSettingsType = sourcePrefabInfoSettings.SettingsType;
                                var template = GetVehicleSettingsTemplate(sourcePrefabInfoSettings);

                                if (currentSettingsType == SettingsType.Template && !template)
                                {
                                    Debug.Log($"{prefabInfoData.ID} Template not found");
                                    currentSettingsType = SettingsType.New;
                                }

                                switch (currentSettingsType)
                                {
                                    case SettingsType.New:
                                        {
                                            float currentWheelRadius = GetWheelRadius(sourcePrefabInfoSettings);
                                            vehicleAuthoring.WheelRadius = currentWheelRadius;

                                            var currentSuspension = sourcePrefabInfoSettings.SuspensionLength.Value;

                                            customVehicleAuthoring.SuspensionLength = VehicleAuthoring.DefaultSuspensionLength;
                                            var offset = currentSuspension - VehicleAuthoring.DefaultSuspensionLength;
                                            customVehicleAuthoring.ChangeSuspension(offset, false);
                                            customVehicleAuthoring.MoveWheels(-offset, false);
                                            break;
                                        }
                                    case SettingsType.Template:
                                        {
                                            template.CopyFromTemplate(customVehicleAuthoring, customVehicleAuthoring.GetComponent<PhysicsBodyAuthoring>(), hullMesh?.transform ?? null, sourcePrefabInfoSettings.CopySettingsType);
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                float currentWheelRadius = GetWheelRadius(sourcePrefabInfoSettings);
                                vehicleAuthoring.WheelRadius = currentWheelRadius;
                            }

                            vehicleAuthoring.SetDirty();
                        }
                        else
                        {
                            Debug.LogError("IVehicleAuthoring not found");
                        }
                    }
                }
                else
                {
                    UpdateDefaultLOD(prefabInfoData, carTemplatePrefab, hullMesh, null);
                }

                ApplyOffsets(sourcePrefabInfoSettings, unpackedPrefab, hullMesh, wheels.Count > 0);

                if (HasLods)
                {
                    for (int j = 1; j < LodCount; j++)
                    {
                        if (!hullLods[j])
                            continue;

                        hullLods[j].transform.localPosition = hullMesh.transform.localPosition;
                        hullLods[j].transform.localRotation = hullMesh.transform.localRotation;
                    }
                }

                var bounds = hullMesh.bounds;

                if (physicsShape)
                {
                    var belongTags = new PhysicsCategoryTags();

                    switch (CurrentOwnerType)
                    {
                        case VehicleOwnerType.Traffic:
                            belongTags.Value = 1 << ProjectConstants.DEFAULT_TRAFFIC_LAYER_VALUE;
                            break;
                        case VehicleOwnerType.Player:
                            belongTags.Value = 1 << ProjectConstants.PLAYER_LAYER_VALUE;
                            break;
                    }

                    physicsShape.BelongsTo = belongTags;

                    physicsShape.transform.localPosition = Vector3.zero;

                    Vector3 shapeSize = default;
                    Vector3 shapeCenter = default;

                    if (FitPhysicsShapeToMesh)
                    {
                        if (IncludeWheels && allWheelsFound)
                        {
                            for (int j = 0; j < MandotaryTypes.Count; j++)
                            {
                                bounds.Encapsulate(wheels[j].bounds);
                            }

                            if (PhysicsShapeAtFloor)
                            {
                                var yOffset = bounds.extents.y - bounds.center.y;
                                bounds.center += new Vector3(0, yOffset / 2, 0);
                                bounds.size = new Vector3(bounds.size.x, bounds.size.y - yOffset, bounds.size.z);
                            }
                        }

                        shapeSize = bounds.size;
                        shapeCenter = bounds.center;
                    }
                    else
                    {
                        shapeSize = bounds.size;
                        shapeCenter = bounds.center;
                    }

                    SetShapeSize(physicsShape, sourcePrefabInfoSettings, shapeSize, shapeCenter);
                }

                AddNavmeshObstacle(carTemplatePrefab, bounds);

                if (OverrideEntityType(sourcePrefabInfoSettings) && sourcePrefabInfoSettings.EntityType != EntityType)
                {
                    var vehicleOverride = carTemplatePrefab.GetOrCreateComponent<VehicleOverrideTypeAuthoring>();
                    vehicleOverride.EntityType = sourcePrefabInfoSettings.EntityType;
                    EditorSaver.SetObjectDirty(vehicleOverride);
                }

                CarEntityAuthoringBase carEntityAuthoring = null;

                switch (CurrentOwnerType)
                {
                    case VehicleOwnerType.Traffic:
                        {
                            carEntityAuthoring = carTemplatePrefab.gameObject.GetOrCreateComponent<TrafficCarEntityAuthoring>();
                            carEntityAuthoring.FactionType = FactionType.City;
                            carEntityAuthoring.CarType = CarType.Traffic;

                            var trafficEntityAuthoring = carEntityAuthoring.GetComponent<TrafficCarEntityAuthoring>();
                            trafficEntityAuthoring.TrafficGroup = sourcePrefabInfoSettings.TrafficGroup;

                            if (sourcePrefabInfoSettings.PublicTransport)
                            {
                                var trafficPublicAuthoring = carTemplatePrefab.gameObject.GetOrCreateComponent<TrafficPublicEntityAuthoring>();
                                trafficPublicAuthoring.PredefinedRoad = sourcePrefabInfoSettings.PredefinedRoad;

                                var carCapacityAuthoring = carTemplatePrefab.gameObject.GetOrCreateComponent<CarCapacityAuthoring>();

                                carCapacityAuthoring.MaxCapacity = sourcePrefabInfoSettings.Capacity;
                                carCapacityAuthoring.CreateEntries(sourcePrefabInfoSettings.Entries);

                                EditorSaver.SetObjectDirty(trafficPublicAuthoring);
                                EditorSaver.SetObjectDirty(carCapacityAuthoring);
                            }

                            break;
                        }
                    case VehicleOwnerType.Player:
                        {
#if !DOTS_SIMULATION
                            carEntityAuthoring = carTemplatePrefab.gameObject.GetOrCreateComponent<PlayerCarEntityAuthoring>();
                            carEntityAuthoring.FactionType = FactionType.Player;
                            carEntityAuthoring.CarType = CarType.Player;
#endif
                            break;
                        }
                }

                carEntityAuthoring.HullMeshRenderer = hullMesh;
                carEntityAuthoring.ID = prefabInfoData.ID;

                var customId = prefabInfoData.CustomID || CurrentScanIDSourceType == ScanIDSourceType.PrefabName;
                carEntityAuthoring.CustomID = customId;

                if (physicsShape)
                {
                    carEntityAuthoring.PhysicsShape = physicsShape;
                }

                EditorSaver.SetObjectDirty(carEntityAuthoring);

                vehicleDataCollection.AddData(prefabInfoData.Name, prefabInfoData.ID);

                if (HasLods)
                {
                    var lodGroup = carTemplatePrefab.gameObject.GetOrCreateComponent<LODGroup>();

                    LOD[] lods = new LOD[LodCount];

                    for (int lodIndex = 0; lodIndex < LodCount; lodIndex++)
                    {
                        if (!hullLods[lodIndex])
                        {
                            Debug.Log($"CarPrefabCreatorWindow. LOD {lodIndex} for Hull not found");
                        }

                        List<Renderer> renders = new List<Renderer>();

                        if (hullLods[lodIndex])
                        {
                            renders.Add(hullLods[lodIndex]);

                            var currWheels = wheelLods[lodIndex];

                            if (currWheels?.Length > 0 && currWheels[0] != null)
                            {
                                renders.AddRange(currWheels);
                            }

                            AddOtherMeshLOD(prefabInfoData, carTemplatePrefab, lodIndex, renders);
                        }

                        var lodSize = GetLODSize(lodIndex);
                        lods[lodIndex] = new LOD(lodSize, renders.ToArray());
                    }

                    lodGroup.SetLODs(lods);
                    lodGroup.RecalculateBounds();
                }

                var sourceMaterial = hullMesh.sharedMaterial;
                var customMaterial = GetMaterial(prefabInfoData);

                if (customMaterial != null)
                {
                    foreach (var meshRenderer in carTemplatePrefab.GetComponentsInChildren<MeshRenderer>())
                    {
                        AssignMaterial(prefabInfoData.ID, meshRenderer, sourceMaterial, customMaterial);
                    }
                }

                SavePrefab(carTemplatePrefab, carScenePrefab, prefabInfoData, sourcePrefabInfoSettings, !prefabHasComponents);
            }

            return true;
        }

        public bool CreateHybrid()
        {
            var inputScript = cacheContainer.InputScript;

            if (!IsPlayerOwner && UserCustomVehicle)
            {
                if (inputScript == null || inputScript.GetClass().GetInterface(nameof(IVehicleInput)) == null)
                {
                    Debug.Log("Create & assign Input Script in the save tab that implements 'IVehicleInput' interface to link traffic input & your vehicle controller input");
                    return false;
                }
            }

            var template = GetControllerTemplatePrefab();
            var hullTemplatePrefab = template.HullPrefab;
            var entityTemplatePrefab = template.EntityPrefab;

            if (collectionEditType == CollectionEditType.Override)
            {
                vehicleDataCollection.ClearData(false);
            }

            for (int i = 0; i < prefabsInfo?.Count; i++)
            {
                var prefabInfoData = prefabsInfo[i];

                if (!prefabInfoData.Prefab)
                    continue;

                var newHullPrefabAsset = prefabInfoData.Prefab;
                var sourcePrefab = prefabInfoData.Prefab;

                var hullMesh = TryToGetHull(prefabInfoData.Prefab);

                if (!hullMesh)
                {
                    Debug.Log($"Prefab {prefabInfoData.Prefab.name} hull mesh not found.");
                    continue;
                }

                if (string.IsNullOrEmpty(prefabInfoData.ID))
                {
                    Debug.Log($"Prefab {prefabInfoData.Prefab.name} ID is not set up.");
                    continue;
                }

                var currentSavePath = GetSavePath(prefabInfoData.Prefab);
                var hullSavePath = GetSavePath(sourcePrefab, true);

                if (!CheckPrefabExist(currentSavePath))
                    continue;

                if (!CheckPrefabExist(hullSavePath))
                    continue;

                if (string.IsNullOrEmpty(currentSavePath))
                {
                    Debug.Log("Save path is empty");
                    continue;
                }

                if (string.IsNullOrEmpty(hullSavePath))
                {
                    Debug.Log("Hull save path is empty");
                    continue;
                }

                var tempNewEntityPrefab = (PrefabUtility.InstantiatePrefab(entityTemplatePrefab.gameObject) as GameObject).GetComponent<CarEntityAuthoringBase>();
                tempNewEntityPrefab.ID = prefabInfoData.ID;
                tempNewEntityPrefab.CustomID = true;
                tempNewEntityPrefab.HybridMono = true;
                tempNewEntityPrefab.SetCustomBounds(hullMesh.bounds);

                vehicleDataCollection.AddData(prefabInfoData.Name, prefabInfoData.ID);

                GameObject newPrefabAsset = default;

                if (UserCustomVehicle)
                {
                    if (PrefabUtility.IsPartOfPrefabAsset(sourcePrefab))
                    {
                        PrefabExtension.EditPrefab(sourcePrefab, (prefab) =>
                        {
                            AddHybridComponents(prefab);
                        });
                    }
                    else
                    {
                        Debug.Log($"{sourcePrefab.name} is not a prefab");
                    }
                }
                else
                {
                    sourcePrefab = PrefabUtility.InstantiatePrefab(hullTemplatePrefab) as GameObject;
                    CreateCarController(prefabInfoData, sourcePrefab);
                    AddHybridComponents(sourcePrefab);
                }

                AddNavmeshObstacle(tempNewEntityPrefab.gameObject, hullMesh.bounds);

                if (!IsPlayerOwner)
                {
                    var authoring = tempNewEntityPrefab.GetComponent<TrafficCarHybridMonoEntityAuthoring>();
                    authoring.MaxSteeringAngle = prefabInfoData.MaxSteeringAngle.Value;
                }

                try
                {
                    newPrefabAsset = PrefabUtility.SaveAsPrefabAsset(tempNewEntityPrefab.gameObject, currentSavePath);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    DestroyImmediate(tempNewEntityPrefab.gameObject);
                }

                if (!UserCustomVehicle)
                {
                    var sourceMaterial = hullMesh.sharedMaterial;
                    var customMaterial = GetMaterial(prefabInfoData);

                    if (customMaterial != null)
                    {
                        foreach (var meshRenderer in sourcePrefab.GetComponentsInChildren<MeshRenderer>())
                        {
                            AssignMaterial(prefabInfoData.ID, meshRenderer, sourceMaterial, customMaterial);
                        }
                    }
                }

                try
                {
                    if (!UserCustomVehicle)
                    {
                        newHullPrefabAsset = PrefabUtility.SaveAsPrefabAsset(sourcePrefab.gameObject, hullSavePath);
                    }
                    else
                    {
                        newHullPrefabAsset = sourcePrefab;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (!UserCustomVehicle && sourcePrefab != null)
                        DestroyImmediate(sourcePrefab.gameObject);
                }

                prefabInfoData.EntityPrefab = newPrefabAsset;
                prefabInfoData.HullPrefab = newHullPrefabAsset;
                prefabInfoData.ResultWeight = 1f;
            }

            return true;
        }

        private void CreateCarController(CarPrefabInfo prefabInfoData, GameObject sourcePrefab)
        {
            try
            {
                var sourceSubPrefab = Instantiate(prefabInfoData.Prefab, sourcePrefab.transform);
                sourceSubPrefab.transform.localPosition = default;
                sourceSubPrefab.transform.localRotation = Quaternion.identity;
                sourceSubPrefab.name = prefabInfoData.Prefab.name;

                var hullMesh = TryToGetHull(sourceSubPrefab);

                FindWheels(sourceSubPrefab, out var wheels, out var extraWheels);
                ApplyOffsets(prefabInfoData, sourceSubPrefab, hullMesh, wheels.Count > 0);

                for (int i = 0; i < wheels?.Count; i++)
                {
                    ApplyWheelOffset(prefabInfoData, wheels[i]);
                }

                for (int i = 0; i < extraWheels?.Count; i++)
                {
                    ApplyWheelOffset(prefabInfoData, extraWheels[i]);
                }

                switch (controllerType)
                {
                    case ControllerType.Arcade:
                        {
                            var arcadeVehicle = sourcePrefab.GetOrCreateComponent<ArcadeVehicleController>();

                            for (int j = 0; j < wheels.Count; j++)
                            {
                                arcadeVehicle.AddWheel(wheels[j].transform, j <= 1);

                                SetWheelMesh(prefabInfoData, arcadeVehicle.transform, wheels[j]);

                            }

                            for (int j = 0; j < extraWheels?.Count; j++)
                            {
                                arcadeVehicle.AddWheel(wheels[j].transform);

                                SetWheelMesh(prefabInfoData, arcadeVehicle.transform, wheels[j]);
                            }

                            var colliders = sourceSubPrefab.GetComponentsInChildren<Collider>();

                            foreach (var collider in colliders)
                            {
                                DestroyImmediate(collider);
                            }

                            if (hullMesh)
                            {
                                arcadeVehicle.InitializeCollider(hullMesh, cacheContainer.colliderType == ColliderType.Box);
                            }
                            else
                            {
                                arcadeVehicle.InitializeCollider();
                            }

                            arcadeVehicle.InitializeBody();

                            var boxCollider = arcadeVehicle.GetComponent<BoxCollider>();

                            if (boxCollider)
                            {
                                boxCollider.size += prefabInfoData.SizeOffset.Value;
                                boxCollider.center += prefabInfoData.CenterOffset.Value;
                                EditorSaver.SetObjectDirty(boxCollider);
                            }

                            arcadeVehicle.WheelRadius = prefabInfoData.WheelRadius.Value;
                            arcadeVehicle.SpringRestLength = prefabInfoData.SuspensionLength.Value;
                            EditorSaver.SetObjectDirty(arcadeVehicle);

                            break;
                        }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void AddHybridComponents(GameObject prefab)
        {
            if (AddPlayerComponents)
            {
                AddPlayerHybridComponents(prefab);
            }

            if (!IsPlayerOwner)
            {
                AddTrafficHybridComponents(prefab);
            }
        }

        private void AddPlayerHybridComponents(GameObject prefab)
        {
#if !DOTS_SIMULATION
            var playerActor = prefab.GetOrCreateComponent<PlayerActor>();
            playerActor.CurrentActorType = PlayerActorType.Car;

            var carSlots = prefab.GetOrCreateComponent<CarSlots>();

            if (carSlots.DriverSlot == null)
            {
                carSlots.GenerateSlot(true);
            }

            prefab.GetOrCreateComponent<PlayerNpcCarBehaviour>();
#endif
        }

        private void AddTrafficHybridComponents(GameObject prefab)
        {
            var scriptSwitcher = prefab.GetOrCreateComponent<ScriptSwitcher>();
            scriptSwitcher.Disable();

            var physicsSwitcher = prefab.GetOrCreateComponent<PhysicsSwitcher>();

            physicsSwitcher.Disable();

            if (UserCustomVehicle)
            {
                prefab.GetOrCreateComponent(cacheContainer.InputScript.GetClass());
            }

            prefab.GetOrCreateComponent<CarEntityAdapter>();
        }

        private void PostCreation()
        {
            UpdateCollection();
            TryToAddPrefabsToPool();
            SaveCache();
            UpdateCollectionHeaders();

            if (pingFolderAfterCreation)
            {
                AssetDatabaseExtension.SelectProjectFolder(SavePath, true);
            }
        }

        private void UpdateCollectionHeaders()
        {
            idBinding.Clear();
            int count = 1;

            if (vehicleDataCollection != null)
            {
                count += vehicleDataCollection.VehicleDataKeys.Count;
            }

            idHeaders = new string[count];
            idHeaders[0] = "None";

            if (!vehicleDataCollection)
                return;

            var vehicleDataKeys = vehicleDataCollection.VehicleDataKeys;

            for (int i = 0; i < vehicleDataKeys.Count; i++)
            {
                idBinding.Add(vehicleDataKeys[i], i);
                idHeaders[i + 1] = vehicleDataKeys[i];
            }
        }

        private void AddOtherMeshLOD(CarPrefabInfo prefabInfoData, GameObject carTemplatePrefab, int lodIndex, List<Renderer> renders)
        {
            switch (lodIndex)
            {
                case 0:
                    {
                        var lods = prefabInfoData.LOD0Meshes;

                        foreach (var lod in lods)
                        {
                            renders.Add(lod.MeshRenderer);
                        }

                        break;
                    }
                case 1:
                    {
                        AddUserOtherLOD(prefabInfoData.LOD1Meshes, carTemplatePrefab, lodIndex, renders);
                        break;
                    }
                case 2:
                    {
                        AddUserOtherLOD(prefabInfoData.LOD2Meshes, carTemplatePrefab, lodIndex, renders);
                        break;
                    }
            }
        }

        private void AddUserOtherLOD(List<LODMeshData> lodData, GameObject carTemplatePrefab, int lodIndex, List<Renderer> renders)
        {
            foreach (var lod in lodData)
            {
                var parent = hullLods[lodIndex];
                var newMesh = CreateNewMesh(lod.Mesh.name, parent.sharedMaterial, lod.Mesh);

                newMesh.transform.SetParent(parent.transform);
                newMesh.transform.localPosition = carTemplatePrefab.transform.TransformPoint(lod.LocalPosition);
                newMesh.transform.localRotation = carTemplatePrefab.transform.rotation * lod.LocalRotation;

                renders.Add(newMesh);
            }
        }

        private void UpdateDefaultLOD(CarPrefabInfo carPrefabInfo, GameObject carTemplatePrefab, MeshRenderer hullMesh, List<MeshRenderer> wheels)
        {
            if (!HasLods)
                return;

            carPrefabInfo.LOD0Meshes.Clear();
            List<MeshRenderer> otherMeshes = null;

            if (wheels != null)
            {
                otherMeshes = carTemplatePrefab.GetComponentsInChildren<MeshRenderer>().Where(a => a != hullMesh && !wheels.Contains(a) && !hullLods.Contains(a)).ToList();
            }
            else
            {
                otherMeshes = carTemplatePrefab.GetComponentsInChildren<MeshRenderer>().Where(a => a != hullMesh && !hullLods.Contains(a)).ToList();
            }

            for (int i = 0; i < otherMeshes.Count; i++)
            {
                MeshRenderer otherMesh = otherMeshes[i];

                carPrefabInfo.LOD0Meshes.Add(new LODMeshData(carTemplatePrefab.transform, otherMesh));
            }
        }

        private MeshRenderer CreateNewMesh(string name, Material material, Mesh mesh)
        {
            var newHull = new GameObject(name);
            var newHullMeshRenderer = newHull.AddComponent<MeshRenderer>();
            var newHullMeshFilter = newHull.AddComponent<MeshFilter>();

            newHullMeshRenderer.sharedMaterial = material;
            newHullMeshFilter.sharedMesh = mesh;

            return newHullMeshRenderer;
        }

        public static CacheContainer LoadCacheContainer()
        {
            var newCollection = AssetDatabase.LoadAssetAtPath($"{CityEditorBookmarks.TRAFFIC_PRESET_PATH}{CACHE_DATA_FILE_NAME}.asset", typeof(ScriptableObject));

            if (newCollection)
            {
                return newCollection as CacheContainer;
            }

            return null;
        }

        #endregion

        #region Private methods

        private EntityType GetEntityType(CarPrefabInfo carPrefabInfo) => OverrideEntityType(carPrefabInfo) ? carPrefabInfo.EntityType : EntityType;

        private bool OverrideEntityType(CarPrefabInfo carPrefabInfo) => !HybridMono ? carPrefabInfo.OverrideEntityType : false;

        private float GetWheelRadius(CarPrefabInfo carPrefabInfo) => carPrefabInfo.WheelRadius.Value;

        private void FindWheels(GameObject sourcePrefab, out List<MeshRenderer> wheels, out List<MeshRenderer> extraWheels)
        {
            wheels = new List<MeshRenderer>();
            extraWheels = null;

            foreach (var type in MandotaryTypes)
            {
                var wheel = TryToGetWheel(sourcePrefab.transform, wheelNameTemplates[type].WheelTemplates, type);

                if (wheel != null)
                {
                    wheels.Add(wheel);
                }

                CheckForExist(wheel, $"{sourcePrefab.name} wheel {type}");
            }

            if (WheelSearchType.HasFlag(WheelSearchType.ByTextPattern))
            {
                foreach (var type in OptionalTypes)
                {
                    if (!wheelNameTemplates.ContainsKey(type))
                        continue;

                    var foundExtraWheels = FindAllMeshesByText(sourcePrefab.transform, wheelNameTemplates[type].WheelTemplates);

                    if (foundExtraWheels?.Count > 0)
                    {
                        foreach (var extraWheel in foundExtraWheels)
                        {
                            if (!ValidWheel(extraWheel.bounds))
                                continue;

                            wheels.TryToAdd(extraWheel);

                            if (extraWheels == null)
                            {
                                extraWheels = new List<MeshRenderer>();
                            }

                            extraWheels.TryToAdd(extraWheel);
                        }
                    }
                }
            }
        }

        private Material GetMaterial(CarPrefabInfo carPrefabInfo)
        {
            switch (CurrentMaterialType)
            {
                case MaterialType.CustomAtlas:
                    {
                        return CustomAtlasMaterial;
                    }
                case MaterialType.NewUniqueMaterial:
                    {
                        if (CustomAtlasMaterial && carPrefabInfo.CustomMaterial == null)
                        {
                            if (carPrefabInfo.SourceMesh && carPrefabInfo.SourceMesh.sharedMaterial)
                            {
                                var materialName = carPrefabInfo.SourceMesh.sharedMaterial.name;
                                var savePath = $"{newCustomMaterialPath}{materialName}.mat";

                                Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(savePath);

                                if (!newMaterial)
                                {
                                    newMaterial = Instantiate(CustomAtlasMaterial);
                                    newMaterial.name = materialName;
                                    newMaterial.mainTexture = carPrefabInfo.SourceMesh.sharedMaterial.mainTexture;
                                    AssetDatabase.CreateAsset(newMaterial, savePath);
                                    AssetDatabase.SaveAssets();
                                }

                                carPrefabInfo.CustomMaterial = newMaterial;
                            }
                            else
                            {
                                if (!carPrefabInfo.SourceMesh)
                                {
                                    Debug.Log($"{carPrefabInfo.Name} SourceMesh is null");
                                }
                            }
                        }

                        return carPrefabInfo.CustomMaterial;
                    }
            }

            return null;
        }

        private float GetLODSize(int index)
        {
            float value = 0;

            switch (index)
            {
                case 0:
                    {
                        value = Lod0ScreenSize;
                        break;
                    }
                case 1:
                    {
                        value = Lod1ScreenSize;
                        break;
                    }
                case 2:
                    {
                        value = Lod2ScreenSize;
                        break;
                    }
            }

            return value / 100f;
        }

        private void SetWheelMesh(CarPrefabInfo carPrefabInfo, Transform parent, MeshRenderer wheelMesh)
        {
            if (WheelSourceType == WheelMeshSourceType.ModelUnique)
                return;

            var localPos = parent.InverseTransformPoint(wheelMesh.transform.position);
            var leftWheel = localPos.x < 0;

            var currentWheelRotationType = CurrentWheelRotationType;
            var allWheels = carPrefabInfo.AllWheels;

            if (WheelSourceType == WheelMeshSourceType.SharedFromModel)
            {
                if (allWheels.Count > carPrefabInfo.SelectedWheelMeshIndex && allWheels[carPrefabInfo.SelectedWheelMeshIndex] != null)
                {
                    var sharedWheel = carPrefabInfo.AllWheels[carPrefabInfo.SelectedWheelMeshIndex];
                    var localPos2 = parent.InverseTransformPoint(sharedWheel.transform.position);
                    var leftWheel2 = localPos2.x < 0;

                    currentWheelRotationType = leftWheel2 ? WheelRotationType.FlipRightRow : WheelRotationType.FlipLeftRow;
                }
                else
                {
                    currentWheelRotationType = WheelRotationType.Source;
                }
            }

            switch (currentWheelRotationType)
            {
                case WheelRotationType.FlipLeftRow:
                    {
                        if (leftWheel)
                        {
                            wheelMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }

                        break;
                    }
                case WheelRotationType.FlipRightRow:
                    {
                        if (!leftWheel)
                        {
                            wheelMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        }

                        break;
                    }
            }

            switch (WheelSourceType)
            {
                case WheelMeshSourceType.SharedFromModel:
                    wheelMesh.GetComponent<MeshFilter>().sharedMesh = carPrefabInfo.SharedWheelMesh[0];
                    break;
                case WheelMeshSourceType.SharedAll:
                    wheelMesh.GetComponent<MeshFilter>().sharedMesh = carPrefabInfo.SharedWheelMesh[0];
                    break;
            }
        }

        private void ApplyOffsets(CarPrefabInfo sourcePrefabInfoSettings, GameObject unpackedPrefab, MeshRenderer hullMesh, bool vehicleHasWheels)
        {
            var sourcePos = hullMesh.transform.localPosition;

            if (AddOffset)
            {
                if (FixPivot)
                {
                    var yOffset = hullMesh.bounds.extents.y - hullMesh.bounds.center.y;
                    hullMesh.transform.localPosition += new Vector3(0, yOffset) + new Vector3(0, unpackedPrefab.transform.position.y);
                }

                if (AddWheelOffset && HasWheels && vehicleHasWheels)
                {
                    float currentWheelRadius = GetWheelRadius(sourcePrefabInfoSettings);
                    hullMesh.transform.localPosition += new Vector3(0, currentWheelRadius);
                }
            }

            hullMesh.transform.localPosition += new Vector3(0, LocalOffset);

            var offset = hullMesh.transform.localPosition - sourcePos;

            var parentTransform = hullMesh.transform.parent;

            for (int i = 0; i < parentTransform.childCount; i++)
            {
                var child = parentTransform.GetChild(i);

                if (child == hullMesh.transform) continue;

                child.localPosition += offset;
            }
        }

        private void ApplyWheelOffset(CarPrefabInfo sourcePrefabInfoSettings, MeshRenderer wheel)
        {
            var currentWheelOffset = sourcePrefabInfoSettings.WheelOffset.Value;

            wheel.transform.localPosition += new Vector3(0, currentWheelOffset);
        }

        private void AddNavmeshObstacle(GameObject prefab, Bounds bounds)
        {
            NavMeshObstacleAuthoring navMeshObstacleAuthoring = prefab.GetComponent<NavMeshObstacleAuthoring>();

            if (hasNavmeshObstacle)
            {
                if (!navMeshObstacleAuthoring)
                {
                    navMeshObstacleAuthoring = prefab.gameObject.AddComponent<NavMeshObstacleAuthoring>();
                }

                navMeshObstacleAuthoring.Carve = true;
                navMeshObstacleAuthoring.MoveThreshold = moveThreshold;
                navMeshObstacleAuthoring.TimeToStationary = carvingTimeToStationary;
                navMeshObstacleAuthoring.CarveOnlyStationary = carveStationary;
            }

            if (navMeshObstacleAuthoring)
            {
                navMeshObstacleAuthoring.Bounds = bounds;
                EditorSaver.SetObjectDirty(navMeshObstacleAuthoring);
            }
        }

        private float GetWheelBase(MeshRenderer sourceWheel)
        {
            if (sourceWheel)
            {
                return (float)Math.Round(sourceWheel.bounds.extents.z, 2);
            }
            else
            {
                return WheelRadius;
            }
        }

        private void AssignMaterial(string id, MeshRenderer targetMeshRenderer, Material sourceMaterial, Material newMaterial)
        {
            if (sourceMaterial == null || targetMeshRenderer.sharedMaterial == sourceMaterial)
            {
                targetMeshRenderer.sharedMaterial = newMaterial;
                EditorSaver.SetObjectDirty(targetMeshRenderer);
            }
            else
            {
                Debug.Log($"AssignMaterial. ID '{id}' MeshRenderer {targetMeshRenderer.name} material '{targetMeshRenderer.sharedMaterial.name}' is ignored.");
            }

            if (targetMeshRenderer.sharedMaterials?.Length > 1)
            {
                Debug.Log($"AssignMaterial. ID '{id}' MeshRenderer {targetMeshRenderer.name} multiple materials found.");
                targetMeshRenderer.sharedMaterials = new Material[1] { targetMeshRenderer.sharedMaterials[0] };
            }
        }

        private GameObject AssignNewParentWheel(Transform sourceWheel, Transform newParent = null)
        {
            var wheelParent = new GameObject(sourceWheel.name + "Parent");
            wheelParent.transform.parent = sourceWheel.parent;
            wheelParent.transform.localPosition = sourceWheel.localPosition;
            wheelParent.transform.localRotation = sourceWheel.localRotation;
            sourceWheel.transform.parent = wheelParent.transform;
            sourceWheel.transform.localPosition = Vector3.zero;
            sourceWheel.transform.localRotation = Quaternion.identity;

            if (newParent != null)
            {
                wheelParent.transform.parent = newParent;
            }

            return wheelParent;
        }

        private void TryToLoadConfig()
        {
            if (string.IsNullOrEmpty(newPresetPath))
            {
                newPresetPath = CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Level/Presets/Traffic";
            }

            if (string.IsNullOrEmpty(trafficHullSavePath))
            {
                trafficHullSavePath = CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Cars/Traffic Mono/Hull/";
            }

            if (string.IsNullOrEmpty(playerHullSavePath))
            {
                playerHullSavePath = CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Cars/Player Mono/Hull/";
            }

            if (string.IsNullOrEmpty(trafficSavePath))
            {
                trafficSavePath = CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Cars/Traffic/";
            }

            if (string.IsNullOrEmpty(playerSavePath))
            {
                playerSavePath = CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Cars/Player/";
            }

            CacheContainer sceneCache = null;

            var vehicleHolder = ObjectUtils.FindObjectOfType<VehicleDataHolder>();

            if (vehicleHolder)
            {
                sceneCache = vehicleHolder.CacheContainer as CacheContainer;
            }

            if (sceneCache)
            {
                cacheContainer = sceneCache;
                vehicleDataCollection = vehicleHolder.VehicleDataCollection;
            }
            else if (!cacheContainer)
            {
                cacheContainer = LoadCacheContainer();
            }

            if (!vehicleDataCollection)
            {
                var newCollection = AssetDatabase.LoadAssetAtPath($"{CityEditorBookmarks.TRAFFIC_PRESET_PATH}{VEHICLE_DATA_FILE_NAME}.asset", typeof(ScriptableObject));

                if (newCollection)
                {
                    vehicleDataCollection = newCollection as VehicleDataCollection;
                }
            }

            if (!trafficConvertTemplate)
            {
                var carConverter = AssetDatabase.LoadAssetAtPath(GetConfigPath(TRAFFIC_CONVERTER_TEMPLATE_PATH), typeof(ScriptableObject));

                if (carConverter)
                {
                    trafficConvertTemplate = carConverter as CarConvertTemplate;
                }
            }

            if (!playerCarConvertTemplate)
            {
                var carConverter = AssetDatabase.LoadAssetAtPath(GetConfigPath(PLAYERCAR_CONVERTER_TEMPLATE_PATH), typeof(ScriptableObject));

                if (carConverter)
                {
                    playerCarConvertTemplate = carConverter as CarConvertTemplate;
                }
            }

            InitWordList(ignoreWords, DefaultIgnoreWords);
            InitWordList(WheelType.WheelFR, DefaultWheelFRWords);
            InitWordList(WheelType.WheelFL, DefaultWheelFLWords);
            InitWordList(WheelType.WheelBL, DefaultWheelBLWords);
            InitWordList(WheelType.WheelBR, DefaultWheelBRWords);
            InitWordList(WheelType.WheelMiddle, DefaultWheelMWords);
        }

        private void InitWordList(WheelType wheelType, string[] sourceWords)
        {
            if (!wheelNameTemplates.ContainsKey(wheelType))
            {
                wheelNameTemplates.Add(wheelType, new WheelTemplateHolder()
                {
                    WheelTemplates = new List<string>()
                });
            }

            InitWordList(wheelNameTemplates[wheelType].WheelTemplates, sourceWords);
        }

        private void InitWordList(List<string> wordList, string[] sourceWords)
        {
            foreach (var word in sourceWords)
            {
                wordList.TryToAdd(word);
            }
        }

        private string GetConfigPath(string relativePath) => $"{CityEditorBookmarks.CITY_EDITOR_CONFIGS_PATH}{relativePath}";

        private string ValidateName(string newEnumType)
        {
            if (newEnumType.Contains('.'))
            {
                newEnumType = newEnumType.Replace('.', '_');
            }

            return newEnumType;
        }

        private void CheckForExist(MeshRenderer meshRenderer, string name)
        {
            if (!meshRenderer)
            {
                Debug.Log($"{name} mesh not found. Make sure the wheel name matches the wheel template in the Template tab, or disable wheels in the Common tab.");
            }
        }

        private MeshRenderer FindMeshByText(Transform sourceParent, List<string> templates)
        {
            MeshRenderer[] meshes = null;
            return FindMeshByText(sourceParent, templates, ref meshes);
        }

        private MeshRenderer FindMeshByText(Transform sourceParent, List<string> templates, ref MeshRenderer[] meshes)
        {
            var lowerTemplates = templates.ConvertAll(str => str.ToLower());

            meshes = sourceParent.GetComponentsInChildren<MeshRenderer>().Where(a => a.transform != sourceParent).ToArray();

            for (int i = 0; i < meshes.Length; i++)
            {
                var meshName = meshes[i].gameObject.name.ToLower();

                for (int j = 0; j < lowerTemplates.Count; j++)
                {
                    var index = meshName.IndexOf(lowerTemplates[j]);

                    if (index >= 0 && !ignoreWords.Any(a => meshName.Contains(a, StringComparison.OrdinalIgnoreCase)))
                    {
                        bool add = true;

                        if (lowerTemplates[j].Length == 2 && meshName.Length > 2)
                        {
                            add = false;
                            var leftIndex = index - 1;

                            if (leftIndex >= 0 &&
                                Symbols.Contains(meshName[leftIndex]))
                            {
                                add = true;
                            }

                            if (add)
                            {
                                var rightIndex = index + lowerTemplates[j].Length;

                                if (meshName.Length > rightIndex)
                                {
                                    add = Symbols.Contains(meshName[rightIndex]);
                                }
                            }
                        }

                        if (add)
                        {
                            add = ValidWheel(meshes[i].bounds);
                        }

                        if (add)
                            return meshes[i];
                    }
                }
            }

            return null;
        }

        private MeshRenderer TryToGetWheel(Transform sourceParent, List<string> templates, WheelType wheelType)
        {
            MeshRenderer[] meshes = null;

            if (WheelSearchType.HasFlag(WheelSearchType.ByTextPattern))
            {
                var mesh = FindMeshByText(sourceParent, templates, ref meshes);

                if (mesh)
                {
                    return mesh;
                }

                for (int i = 0; i < meshes?.Length; i++)
                {
                    var meshName = meshes[i].gameObject.name.ToLower();

                    if (!meshName.Contains("wheel", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    bool hasTemplate = false;

                    switch (wheelType)
                    {
                        case WheelType.WheelFL:
                            hasTemplate = meshName.Contains("fl");
                            break;
                        case WheelType.WheelFR:
                            hasTemplate = meshName.Contains("fr");
                            break;
                        case WheelType.WheelBR:
                            hasTemplate = meshName.Contains("br");
                            break;
                        case WheelType.WheelBL:
                            hasTemplate = meshName.Contains("bl");
                            break;
                    }

                    if (hasTemplate)
                    {
                        return meshes[i];
                    }
                }
            }

            if (WheelSearchType.HasFlag(WheelSearchType.ByPosition))
            {
                MeshRenderer mesh = null;

                if (meshes == null)
                {
                    meshes = sourceParent.GetComponentsInChildren<MeshRenderer>().Where(a => a.transform != sourceParent).ToArray();
                }

                float yPos = float.MaxValue;

                sourceParent.transform.rotation = Quaternion.identity;

                for (int i = 0; i < meshes?.Length; i++)
                {
                    var bounds = meshes[i].bounds;

                    if (!ValidWheel(bounds))
                        continue;

                    var localPos = sourceParent.InverseTransformPoint(meshes[i].transform.position);

                    bool isWheel = false;

                    switch (wheelType)
                    {
                        case WheelType.WheelFL:
                            isWheel = localPos.x < 0 && localPos.z > 0;
                            break;
                        case WheelType.WheelFR:
                            isWheel = localPos.x > 0 && localPos.z > 0;
                            break;
                        case WheelType.WheelBR:
                            isWheel = localPos.x > 0 && localPos.z < 0;
                            break;
                        case WheelType.WheelBL:
                            isWheel = localPos.x < 0 && localPos.z < 0;
                            break;
                    }

                    if (isWheel)
                    {
                        if (yPos > localPos.y && (mesh == null || !TransformExtensions.IsChild(mesh.gameObject, meshes[i].gameObject)))
                        {
                            yPos = localPos.y;
                            mesh = meshes[i];
                        }
                    }
                }

                return mesh;
            }

            return null;
        }

        private bool ValidWheel(Bounds bounds)
        {
            var rateSize = bounds.size.y / bounds.size.z;

            if (rateSize < WheelSizeRate.x || rateSize > WheelSizeRate.y)
                return false;

            return true;
        }

        private List<MeshRenderer> FindAllMeshesByText(Transform sourceParent, List<string> templates)
        {
            List<MeshRenderer> addedMeshes = new List<MeshRenderer>();
            var lowerTemplates = templates.ConvertAll(str => str.ToLower());

            var meshes = sourceParent.GetComponentsInChildren<MeshRenderer>().Where(a => a.transform != sourceParent).ToArray();

            for (int i = 0; i < meshes.Length; i++)
            {
                var meshName = meshes[i].gameObject.name.ToLower();

                for (int j = 0; j < lowerTemplates.Count; j++)
                {
                    if (meshName.Contains(lowerTemplates[j]) && !ignoreWords.Any(a => meshName.Contains(a, StringComparison.OrdinalIgnoreCase)))
                    {
                        addedMeshes.TryToAdd(meshes[i]);
                    }
                }
            }

            return addedMeshes;
        }

        private GameObject GetCopyTemplatePrefab(CarPrefabInfo carScenePrefabInfo)
        {
            var template = GetTemplate(carScenePrefabInfo);
            GameObject templatePrefab = template.EntityPrefab;

            GameObject objSource = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;

            return objSource;
        }

        private CarPrefabPair GetTemplate(CarPrefabInfo carScenePrefabInfo = null)
        {
            var currentEntityType = EntityType;

            if (carScenePrefabInfo != null && OverrideEntityType(carScenePrefabInfo))
            {
                currentEntityType = carScenePrefabInfo.EntityType;
            }

            CarPrefabPair templatePrefab = null;

            switch (CurrentOwnerType)
            {
                case VehicleOwnerType.Traffic:
                    {
                        if (trafficConvertTemplate == null)
                        {
                            Debug.Log("Template file not found");
                            return null;
                        }

                        trafficConvertTemplate.CarTemplates.TryGetValue(currentEntityType, out templatePrefab);
                        break;
                    }
                case VehicleOwnerType.Player:
                    {
                        if (playerCarConvertTemplate == null)
                        {
                            Debug.Log("Template file not found");
                            return null;
                        }

                        playerCarConvertTemplate.CarTemplates.TryGetValue(currentEntityType, out templatePrefab);
                        break;
                    }
            }

            return templatePrefab;
        }

        private CarPrefabPair GetControllerTemplatePrefab()
        {
            CarConvertTemplate.ControllerTemplateDataDictionary data = null;

            switch (CurrentOwnerType)
            {
                case VehicleOwnerType.Traffic:
                    data = trafficConvertTemplate.ControllerData;
                    break;
                case VehicleOwnerType.Player:
                    data = playerCarConvertTemplate.ControllerData;
                    break;
            }

            if (data != null && data.ContainsKey(controllerType))
            {
                return data[controllerType];
            }

            return null;
        }

        private void SavePrefab(GameObject newCarPrefab, GameObject carScenePrefab, CarPrefabInfo prefabInfo, CarPrefabInfo sourcePrefabInfo, bool newPrefab)
        {
            var template = GetTemplate(sourcePrefabInfo);

            string currentSavePath = GetSavePath(carScenePrefab);

            if (string.IsNullOrEmpty(currentSavePath))
            {
                if (newPrefab)
                {
                    DestroyImmediate(newCarPrefab);
                }

                Debug.Log("Save path is empty");
                return;
            }

            if (!CheckPrefabExist(newCarPrefab, newPrefab, currentSavePath))
                return;

            string currentSaveHullPath = string.Empty;

            if (CloneHull)
            {
                currentSaveHullPath = GetSavePath(template.HullPrefab, true);

                if (!CheckPrefabExist(newCarPrefab, newPrefab, currentSaveHullPath))
                    return;
            }

            GameObject entityPrefab = null;

            if (newPrefab)
            {
                GameObject prefabVariant = PrefabUtility.SaveAsPrefabAsset(newCarPrefab, currentSavePath);
                entityPrefab = prefabVariant;
                DestroyImmediate(newCarPrefab);
            }
            else
            {
                EditorSaver.SetObjectDirty(newCarPrefab.GetComponent<TrafficCarEntityAuthoring>());
                PrefabUtility.SavePrefabAsset(newCarPrefab);
                entityPrefab = newCarPrefab;
            }

            var hybridSkinPrefab = template.HullPrefab;

            if (CloneHull)
            {
                hybridSkinPrefab = PrefabUtility.SaveAsPrefabAsset(hybridSkinPrefab, currentSaveHullPath);
            }

            prefabInfo.EntityPrefab = entityPrefab;
            prefabInfo.HullPrefab = hybridSkinPrefab;
            prefabInfo.ResultWeight = sourcePrefabInfo.Weight;
        }

        private bool CheckPrefabExist(GameObject newCarPrefab, bool newPrefab, string currentSavePath)
        {
            var existPrefab = AssetDatabase.LoadAssetAtPath(currentSavePath, typeof(Transform));

            if (!allowOverride)
            {
                if (existPrefab != null)
                {
                    if (newPrefab)
                    {
                        DestroyImmediate(newCarPrefab);
                    }

                    Debug.Log($"Prefab '{currentSavePath}' already exist");
                    return false;
                }
            }

            return true;
        }

        private bool CheckPrefabExist(string currentSavePath)
        {
            var existPrefab = AssetDatabase.LoadAssetAtPath(currentSavePath, typeof(Transform));

            if (!allowOverride)
            {
                if (existPrefab != null)
                {
                    Debug.Log($"Prefab '{currentSavePath}' already exist");
                    return false;
                }
            }

            return true;
        }

        private string GetSavePath(GameObject carScenePrefab, bool hull = false)
        {
            string carName = carScenePrefab.name;

            string newTextTemplate = cacheContainer.GetTemplateName(EntityType, IsPlayerOwner, hull);

            var currentSavePath = !hull ? SavePath : SaveHullPath;

            if (currentSavePath[currentSavePath.Length - 1] != '/')
            {
                currentSavePath += "/";
            }

            currentSavePath += carName + $"{newTextTemplate}.prefab";

            return currentSavePath;
        }

        private void UpdateCollection()
        {
            foreach (var prefab in prefabsInfo)
            {
                if (prefab.SettingsType != SettingsType.CloneModel)
                {
                    vehicleDataCollection.DiscardClone(prefab.ID);
                }
                else
                {
                    vehicleDataCollection.SetClone(prefab.ID, prefab.CloneID);
                }
            }
        }

        private void TryToAddPrefabsToPool()
        {
            TrafficCarPoolPreset carPoolPreset = null;

            if (AddToExistPreset)
            {
                switch (CurrentPresetSourceType)
                {
                    case PresetSourceType.Scene:
                        {
                            GetPool(out carPoolPreset);
                            break;
                        }
                    case PresetSourceType.Selected:
                        {
                            carPoolPreset = SelectedPreset;
                            break;
                        }
                }

                if (carPoolPreset == null)
                {
                    switch (CurrentPresetSourceType)
                    {
                        case PresetSourceType.Scene:
                            {
                                Debug.Log($"TrafficCarEntityPoolBakerRef on scene doesn't have a preset with <b>{EntityType}</b> entityType. Create a new preset with the selected type & assign it to <b>TrafficCarEntityPoolBakerRef</b> or change the entity type.");
                                break;
                            }
                        case PresetSourceType.Selected:
                            {
                                Debug.Log($"Selected preset in the <b>Save tab</b> is null. Select preset & try again");
                                break;
                            }
                    }

                    return;
                }
            }
            else
            {
                carPoolPreset = AssetDatabaseExtension.CreatePersistScriptableObject<TrafficCarPoolPreset>(newPresetPath, newPresetName);

                if (carPoolPreset == null)
                {
                    Debug.Log($"Can't create new preset {newPresetName}");
                    return;
                }

                if (assignNewPresetToScene)
                {
                    TryToAssignPresetToScene(carPoolPreset);
                }
            }

            if (carPoolPreset)
            {
                carPoolPreset.TrafficEntityType = EntityType;
                carPoolPreset.ClearNulls(false);

                for (int i = 0; i < prefabsInfo?.Count; i++)
                {
                    var prefabsInfoData = prefabsInfo[i];
                    var id = prefabsInfoData.ID;

                    if (string.IsNullOrEmpty(id) || !prefabsInfoData.EntityPrefab)
                    {
                        continue;
                    }

                    var carPrefabPair = new CarPrefabPair()
                    {
                        EntityPrefab = prefabsInfoData.EntityPrefab,
                        HullPrefab = prefabsInfoData.HullPrefab,
                        Weight = prefabsInfoData.ResultWeight,
                    };

                    carPoolPreset.AddEntry(id, carPrefabPair, true);
                }

                EditorSaver.SetObjectDirty(carPoolPreset);
            }
        }

        private List<CarPrefabPair> GetPool(out TrafficCarPoolPreset carPoolPreset)
        {
            List<CarPrefabPair> entityDictionary = null;
            carPoolPreset = null;

            switch (CurrentOwnerType)
            {
                case VehicleOwnerType.Traffic:
                    {
                        var trafficPoolGlobal = ObjectUtils.FindObjectOfType<TrafficCarPoolGlobal>();

                        if (trafficPoolGlobal)
                        {
                            entityDictionary = trafficPoolGlobal.GetPoolData(EntityType);
                            carPoolPreset = trafficPoolGlobal.GetPreset(EntityType);
                        }

                        break;
                    }
                case VehicleOwnerType.Player:
                    {
#if !DOTS_SIMULATION
                        var playerCarPool = ObjectUtils.FindObjectOfType<PlayerCarPool>();

                        if (playerCarPool)
                        {
                            entityDictionary = playerCarPool.CarPrefabs;
                            carPoolPreset = playerCarPool.CarPoolPreset;
                        }
#endif

                        break;
                    }
            }

            return entityDictionary;
        }

        private void TryToAssignPresetToScene(TrafficCarPoolPreset carPoolPreset)
        {
            bool set = false;

            switch (CurrentOwnerType)
            {
                case VehicleOwnerType.Traffic:
                    {
                        var trafficPoolGlobal = ObjectUtils.FindObjectOfType<TrafficCarPoolGlobal>();

                        if (trafficPoolGlobal && trafficPoolGlobal.SetPreset(carPoolPreset, EntityType))
                        {
                            set = true;
                        }

                        break;
                    }
                case VehicleOwnerType.Player:
                    {
#if !DOTS_SIMULATION
                        var playerCarPool = ObjectUtils.FindObjectOfType<PlayerCarPool>();

                        if (playerCarPool)
                        {
                            playerCarPool.CarPoolPreset = carPoolPreset;
                            EditorSaver.SetObjectDirty(playerCarPool);
                            set = true;
                        }
#endif

                        break;
                    }
            }

            if (!set)
            {
                Debug.LogError($"Preset not assigned! Pool with type '{EntityType} not found.");
            }
        }

        private void InitList()
        {
            SetIds();

            var lineHeight = EditorGUIUtility.singleLineHeight + 2;
            var fieldHeight = lineHeight - 2;

            reorderableList = new ReorderableList(prefabsInfo, typeof(CarPrefabInfo), true, false, true, true);

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var prefabsInfoProp = so.FindProperty(nameof(prefabsInfo));
                SerializedProperty element = prefabsInfoProp.GetArrayElementAtIndex(index);

                var sourcePrefab = prefabsInfo[index];

                var r1 = rect;
                r1.height = fieldHeight;

                var prefabProp = element.FindPropertyRelative("Prefab");
                EditorGUI.PropertyField(r1, prefabProp);

                if (showPreview && prefabProp.objectReferenceValue != null)
                {
                    var lastRect = GUILayoutUtility.GetLastRect();

                    var x = r1.x;
                    var previousHeight = r1.height;
                    r1.y += lineHeight;
                    r1.height = PreviewSize;
                    r1.x = EditorGUIUtility.labelWidth + 20f;

                    var texture = AssetPreview.GetAssetPreview(prefabProp.objectReferenceValue);

                    GUI.Label(r1, texture);

                    r1.height = previousHeight;
                    r1.y += PreviewSize;
                    r1.y -= lineHeight;
                    r1.x = x;
                }

                var settingsType = prefabsInfo[index].SettingsType;

                switch (additionalTabIndex)
                {
                    case 0:
                        {
                            DrawIdSettings(index, lineHeight, element, sourcePrefab, ref r1);
                            DrawSettings(index, lineHeight, prefabsInfoProp, element, ref r1);
                            break;
                        }
                    case 1:
                        {
                            TryToGetClonedProp(index, prefabsInfoProp, element, out var sourceProp, out var sourcePropIndex);

                            var currentElement = sourceProp ?? element;

                            GUI.enabled = sourceProp == null;

                            r1.y += lineHeight;
                            EditorGUI.PropertyField(r1, currentElement.FindPropertyRelative("SizeOffset"));

                            if (settingsType == SettingsType.CloneModel)
                            {
                                sourcePrefab = TryToGetClonePrefab(sourcePrefab);
                            }

                            var template = GetVehicleSettingsTemplate(sourcePrefab);

                            r1.y += lineHeight;
                            EditorGUI.PropertyField(r1, currentElement.FindPropertyRelative("CenterOffset"));

                            if (!HybridMono)
                            {
                                r1.y += lineHeight;

                                if (template == null || !sourcePrefab.CopySettingsType.HasFlag(VehicleCustomTemplate.CopySettingsType.CenterOfMass))
                                {
                                    EditorGUI.PropertyField(r1, currentElement.FindPropertyRelative("CenterOfMass"));
                                }
                                else
                                {
                                    var labelWidth = EditorGUIUtility.labelWidth;

                                    GUI.enabled = false;
                                    EditorGUI.LabelField(r1, "Center Of Mass [template]");

                                    var tempR = r1;
                                    const float offset = 152f;
                                    tempR.width -= offset + 20f;
                                    tempR.x += offset;

                                    EditorGUIUtility.labelWidth = 0;
                                    EditorGUI.Vector3Field(tempR, string.Empty, template.CenterOfMass);
                                    GUI.enabled = true;

                                    EditorGUIUtility.labelWidth = labelWidth;
                                }

                                r1.y += lineHeight;

                                EditorGUI.PropertyField(r1, currentElement.FindPropertyRelative("BevelRadius"));

                                r1.y += lineHeight;

                                if (template == null || !sourcePrefab.CopySettingsType.HasFlag(VehicleCustomTemplate.CopySettingsType.PhysicsSettings))
                                {
                                    EditorGUI.PropertyField(r1, currentElement.FindPropertyRelative("Mass"));
                                }
                                else
                                {
                                    GUI.enabled = false;

                                    var tempR = r1;
                                    tempR.width -= 20f;

                                    EditorGUI.Slider(tempR, "Mass [template]", template.VehicleMass, 1f, 5000f);
                                    GUI.enabled = true;
                                }

                                GUI.enabled = true;
                            }

                            break;
                        }
                    case 2:
                        {
                            if (!UserCustomVehicle)
                            {
                                if (!HasLods)
                                {
                                    r1.y += lineHeight;
                                    prefabsInfo[index].HullMeshLODs[0] = (Mesh)EditorGUI.ObjectField(r1, $"Hull Mesh", prefabsInfo[index].HullMeshLODs[0], typeof(Mesh), false);
                                }
                                else
                                {
                                    r1.y += lineHeight;

                                    DrawHelpBox(r1, prefabsInfo[index].HullMeshLODs.Length + 1, lineHeight);

                                    GUI.Label(r1, "Hull Mesh", EditorStyles.boldLabel);

                                    for (int i = 0; i < prefabsInfo[index].HullMeshLODs.Length; i++)
                                    {
                                        r1.y += lineHeight;
                                        prefabsInfo[index].HullMeshLODs[i] = (Mesh)EditorGUI.ObjectField(r1, $"LOD {i}", prefabsInfo[index].HullMeshLODs[i], typeof(Mesh), false);
                                    }

                                    r1.y += HelpboxSpacing;
                                }

                                if (SharedWheel)
                                {
                                    r1.y += lineHeight;

                                    int toolbarHeader = 0;

                                    if (WheelSourceType == WheelMeshSourceType.SharedFromModel)
                                    {
                                        toolbarHeader = 1;
                                    }

                                    var wheelCount = 1;

                                    if (HasLods)
                                    {
                                        wheelCount = prefabsInfo[index].SharedWheelMesh.Length;
                                    }

                                    DrawHelpBox(r1, wheelCount + 1 + toolbarHeader, lineHeight);

                                    GUI.Label(r1, "Source Shared Wheel", EditorStyles.boldLabel);
                                }

                                if (WheelSourceType == WheelMeshSourceType.SharedFromModel)
                                {
                                    r1.y += lineHeight;
                                    var newIndex = GUI.Toolbar(r1, prefabsInfo[index].SelectedWheelMeshIndex, prefabsInfo[index].AvailableWheels);

                                    if (prefabsInfo[index].SelectedWheelMeshIndex != newIndex)
                                    {
                                        prefabsInfo[index].SelectedWheelMeshIndex = newIndex;
                                        InitWheelMesh();
                                    }
                                }

                                if (SharedWheel)
                                {
                                    if (!HasLods)
                                    {
                                        r1.y += lineHeight;
                                        prefabsInfo[index].SharedWheelMesh[0] = (Mesh)EditorGUI.ObjectField(r1, $"Shared Wheel Mesh", prefabsInfo[index].SharedWheelMesh[0], typeof(Mesh), false);
                                    }
                                    else
                                    {
                                        for (int i = 0; i < prefabsInfo[index].SharedWheelMesh.Length; i++)
                                        {
                                            r1.y += lineHeight;
                                            prefabsInfo[index].SharedWheelMesh[i] = (Mesh)EditorGUI.ObjectField(r1, $"LOD {i}", prefabsInfo[index].SharedWheelMesh[i], typeof(Mesh), false);
                                        }
                                    }

                                    r1.y += HelpboxSpacing;
                                }

                                if (HasLods)
                                {
                                    r1.y += lineHeight;
                                    int fieldCount = (prefabsInfo[index].LOD0Meshes.Count + 1) + (prefabsInfo[index].LOD1Meshes.Count + 1) + (prefabsInfo[index].LOD2Meshes.Count + 1);
                                    DrawHelpBox(r1, fieldCount + 1, lineHeight);

                                    GUI.Label(r1, "Other Meshes", EditorStyles.boldLabel);

                                    DrawLODList(ref r1, index, lineHeight, prefabsInfo[index].LOD0Meshes, "LOD 0", true);
                                    DrawLODList(ref r1, index, lineHeight, prefabsInfo[index].LOD1Meshes, "LOD 1", true);
                                    DrawLODList(ref r1, index, lineHeight, prefabsInfo[index].LOD2Meshes, "LOD 2", true);
                                }
                            }

                            break;
                        }
                }

                so.ApplyModifiedProperties();
            };

            reorderableList.elementHeightCallback = (index) =>
            {
                SerializedProperty element = so.FindProperty(nameof(prefabsInfo)).GetArrayElementAtIndex(index);

                int fieldCount = 1;
                float additional = 0;

                switch (additionalTabIndex)
                {
                    case 0:
                        {
                            fieldCount += 2;
                            GetMainTabFieldCount(index, element, ref fieldCount);
                            break;
                        }
                    case 1:
                        {
                            fieldCount += 2;

                            if (!HybridMono)
                            {
                                fieldCount += 3;
                            }

                            break;
                        }
                    case 2:
                        {
                            GetMeshFieldCount(index, ref fieldCount, ref additional);

                            if (HybridMono)
                            {
                                fieldCount += 1;
                            }

                            break;
                        }
                }

                var prefabProp = element.FindPropertyRelative("Prefab");
                float currentPreviewSize = 0;

                if (showPreview && prefabProp.objectReferenceValue != null)
                {
                    currentPreviewSize = PreviewSize;
                }

                return fieldCount * lineHeight + EntrySpacing + currentPreviewSize + additional;
            };

            reorderableList.onRemoveCallback = (index) =>
            {
                SetIds();
            };
        }

        private void GetMainTabFieldCount(int index, SerializedProperty element, ref int fieldCount)
        {
            if (!IsPlayerOwner)
            {
                fieldCount++;
            }

            GetIdFieldCount(index, ref fieldCount);

            var settingsType = SettingsType.New;
            var showSettings = showCustomSettings && !UserCustomVehicle;

            bool hasCloneSettings = false;
            var settingsIndex = -1;

            if (showSettings)
            {
                settingsType = prefabsInfo[index].SettingsType;

                if (settingsType == SettingsType.CloneModel)
                {
                    settingsIndex = TryToGetClonePrefabIndex(prefabsInfo[index]);

                    if (settingsIndex >= 0)
                    {
                        hasCloneSettings = true;
                    }
                }
            }

            if (showAdditionalSettings && !IsPlayerOwner && !hasCloneSettings)
            {
                fieldCount += 2;

                if (!HybridMono)
                {
                    fieldCount += 1;

                    var overrideEntityType = element.FindPropertyRelative("OverrideEntityType");

                    if (overrideEntityType.boolValue)
                    {
                        fieldCount += 1;
                    }
                }

                var publicTransportProp = element.FindPropertyRelative("PublicTransport");

                if (publicTransportProp.boolValue)
                {
                    fieldCount += 3;
                }
            }

            if (UserCustomVehicle && !IsPlayerOwner)
            {
                fieldCount += 2;
            }

            if (showSettings)
            {
                if (!HasTemplateSettings)
                {
                    if (settingsType == SettingsType.Template)
                        settingsType = SettingsType.New;
                }

                fieldCount++;

                switch (settingsType)
                {
                    case SettingsType.New:
                        {
                            fieldCount += 3;
                            break;
                        }
                    case SettingsType.Template:
                        {
                            fieldCount += 1;

                            if (prefabsInfo[index].LocalTemplateIndex > 0)
                            {
                                fieldCount += 2;
                            }

                            break;
                        }
                    case SettingsType.CloneModel:
                        {
                            if (hasCloneSettings)
                            {
                                SerializedProperty targetElement = so.FindProperty(nameof(prefabsInfo)).GetArrayElementAtIndex(settingsIndex);

                                GetMainTabFieldCount(settingsIndex, targetElement, ref fieldCount);

                                if (!IsPlayerOwner)
                                {
                                    fieldCount -= 1;
                                }
                            }

                            if (!hasCloneSettings)
                            {
                                fieldCount += 4;
                            }

                            break;
                        }
                }
            }
        }

        private void GetIdFieldCount(int index, ref int fieldCount)
        {
            if (CustomId(prefabsInfo[index]))
            {
                fieldCount++;

                if (prefabsInfo[index].IDSourceType == IDSourceType.Collection)
                {
                    fieldCount++;
                }
            }
        }

        private void GetMeshFieldCount(int index, ref int fieldCount, ref float additional)
        {
            fieldCount += 1;

            if (UserCustomVehicle)
                return;

            if (HasLods)
            {
                fieldCount += 4;
                additional += HelpboxSpacing * 2;
            }

            switch (WheelSourceType)
            {
                case WheelMeshSourceType.SharedFromModel:
                    {
                        fieldCount += 3;

                        if (HasLods)
                        {
                            fieldCount += 2;
                        }

                        break;
                    }
                case WheelMeshSourceType.SharedAll:
                    {
                        fieldCount++;

                        if (HasLods)
                        {
                            fieldCount += 2;
                        }

                        break;
                    }
            }

            fieldCount++;

            if (HasLods)
            {
                fieldCount += 3;
                fieldCount += prefabsInfo[index].LOD0Meshes.Count;
                fieldCount += prefabsInfo[index].LOD1Meshes.Count;
                fieldCount += prefabsInfo[index].LOD2Meshes.Count;
            }
        }

        private void DrawIdSettings(int index, float lineHeight, SerializedProperty element, CarPrefabInfo sourcePrefab, ref Rect r1)
        {
            r1.y += lineHeight;

            EditorGUI.PropertyField(r1, element.FindPropertyRelative("Name"));

            var idProp = element.FindPropertyRelative("ID");
            var customIdProp = element.FindPropertyRelative("CustomID");

            if (CustomId(sourcePrefab))
            {
                r1.y += lineHeight;

                EditorGUI.BeginChangeCheck();

                EditorGUI.PropertyField(r1, element.FindPropertyRelative("IDSourceType"));

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    OnIdSourceChanged(index);
                }

                var idSourceType = sourcePrefab.IDSourceType;

                switch (idSourceType)
                {
                    case IDSourceType.Custom:
                        {
                            break;
                        }
                    case IDSourceType.Collection:
                        {
                            var idIndex = 0;

                            string sourceId = idProp.stringValue;

                            if (idBinding.ContainsKey(sourceId))
                            {
                                idIndex = idBinding[sourceId] + 1;
                            }

                            r1.y += lineHeight;
                            var newIndex = EditorGUI.Popup(r1, "Collection ID", idIndex, idHeaders);

                            var newId = string.Empty;

                            if (newIndex > 0)
                            {
                                newId = idHeaders[newIndex];
                            }

                            if (sourceId != newId)
                            {
                                idProp.stringValue = newId;
                            }

                            break;
                        }
                }

                r1.y += lineHeight;

                bool idEnabled = idSourceType == IDSourceType.Custom;

                if (HybridMono)
                {
                    GUI.enabled = idEnabled;

                    EditorGUI.PropertyField(r1, idProp);

                    GUI.enabled = true;
                }
                else
                {
                    DrawIdField(index, r1, idProp, customIdProp, idEnabled);
                }
            }
            else
            {
                r1.y += lineHeight;

                DrawIdField(index, r1, idProp, customIdProp, HybridMono);
            }
        }

        private void DrawIdField(int index, Rect r1, SerializedProperty idProp, SerializedProperty customIdProp, bool enabled)
        {
            GUI.enabled = enabled;

            var r2 = r1;

            r2.width -= 20;

            var r3 = r1;

            r3.x += r2.width + 5f;
            r3.width = 20f;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(r2, idProp);

            ShowErrorIfDuplicate(prefabsInfo[index], r2);

            if (EditorGUI.EndChangeCheck())
            {
                var previousId = prefabsInfo[index].ID;
                idProp.serializedObject.ApplyModifiedProperties();
                ChangeId(index, previousId, idProp.stringValue);
            }

            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(r3, customIdProp, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                customIdProp.serializedObject.ApplyModifiedProperties();

                if (!customIdProp.boolValue)
                {
                    var prefabInfo = prefabsInfo[index];
                    var previousId = prefabInfo.ID;
                    prefabInfo.ID = GetID(prefabInfo.Prefab);
                    ChangeId(prefabInfo, previousId, prefabInfo.ID);
                }
                else
                {
                    OnIdSourceChanged(index);
                }
            }
        }

        private void ShowErrorIfDuplicate(CarPrefabInfo sourcePrefab, Rect rect)
        {
            if (duplicateIds.Contains(sourcePrefab.Prefab))
            {
                rect.width = 100f;
                rect.x += 20;
                EditorGUI.HelpBox(rect, "Duplicate", MessageType.Error);
            }
        }

        private void DrawSettings(int index, float lineHeight, SerializedProperty prefabsInfoProp, SerializedProperty element, ref Rect r1, bool enabled = true, SerializedProperty sourceElement = null)
        {
            SerializedProperty currentElement = null;

            bool settingsOverriden = false;

            if (sourceElement != null)
            {
                currentElement = sourceElement;
                settingsOverriden = true;
            }
            else
            {
                currentElement = element;
            }

            var settingsType = prefabsInfo[index].SettingsType;

            TryToGetClonedProp(index, prefabsInfoProp, element, out var sourceProp, out var sourcePropIndex);

            if (sourceProp != null)
            {
                DrawSettings(sourcePropIndex, lineHeight, prefabsInfoProp, sourceProp, ref r1, false, element);
                return;
            }

            if (!IsPlayerOwner)
            {
                GUI.enabled = enabled;

                r1.y += lineHeight;

                EditorGUI.PropertyField(r1, element.FindPropertyRelative("TrafficGroup"));

                if (showAdditionalSettings)
                {
                    r1.y += lineHeight;

                    EditorGUI.PropertyField(r1, element.FindPropertyRelative("Weight"));

                    if (!HybridMono)
                    {
                        r1.y += lineHeight;

                        var overrideEntityTypeProp = element.FindPropertyRelative("OverrideEntityType");
                        EditorGUI.PropertyField(r1, overrideEntityTypeProp);

                        if (overrideEntityTypeProp.boolValue)
                        {
                            EditorGUI.indentLevel++;

                            r1.y += lineHeight;
                            EditorGUI.PropertyField(r1, element.FindPropertyRelative("EntityType"));

                            EditorGUI.indentLevel--;
                        }
                    }

                    r1.y += lineHeight;

                    var publicTransportProp = element.FindPropertyRelative("PublicTransport");

                    EditorGUI.PropertyField(r1, publicTransportProp);

                    if (publicTransportProp.boolValue)
                    {
                        EditorGUI.indentLevel++;

                        r1.y += lineHeight;
                        EditorGUI.PropertyField(r1, element.FindPropertyRelative("PredefinedRoad"));

                        r1.y += lineHeight;
                        EditorGUI.PropertyField(r1, element.FindPropertyRelative("Capacity"));

                        r1.y += lineHeight;
                        EditorGUI.PropertyField(r1, element.FindPropertyRelative("Entries"));

                        EditorGUI.indentLevel--;
                    }
                }
            }

            if (!UserCustomVehicle)
            {
                if (showCustomSettings)
                {
                    r1.y += lineHeight;

                    Func<Enum, bool> settingsTypeValidation = (value) =>
                    {
                        var enumValue = (SettingsType)value;

                        if (enumValue != SettingsType.Template)
                        {
                            return true;
                        }

                        return HasTemplateSettings;
                    };

                    GUI.enabled = true;

                    var settingsTypeProp = currentElement.FindPropertyRelative("SettingsType");

                    var labelContent = InspectorExtension.GetPropertyLabel(settingsTypeProp, "Settings Type");

                    settingsType = (SettingsType)EditorGUI.EnumPopup(r1, labelContent, (SettingsType)settingsTypeProp.enumValueIndex, checkEnabled: settingsTypeValidation);

                    if (settingsTypeProp.enumValueIndex != (int)settingsType)
                    {
                        settingsTypeProp.enumValueIndex = (int)settingsType;
                    }

                    GUI.enabled = enabled;
                }

                if (!HasTemplateSettings && settingsType == SettingsType.Template)
                {
                    settingsType = SettingsType.New;
                }

                DrawAdditionalSettings(index, lineHeight, element, ref r1, enabled, currentElement, settingsOverriden, settingsType);

                if (settingsOverriden)
                {
                    var cloneSettingsTypeProp = element.FindPropertyRelative("SettingsType");
                    var currentSettingsType = (SettingsType)cloneSettingsTypeProp.enumValueIndex;

                    if (!HasTemplateSettings && currentSettingsType == SettingsType.Template)
                    {
                        currentSettingsType = SettingsType.New;
                    }

                    DrawAdditionalSettings(index, lineHeight, element, ref r1, false, element, settingsOverriden, currentSettingsType);
                }

                GUI.enabled = true;
            }
            else
            {
                if (!IsPlayerOwner)
                {
                    r1.y += lineHeight;
                    EditorGUI.PropertyField(r1, element.FindPropertyRelative("AdditiveOffset"));

                    r1.y += lineHeight;
                    EditorGUI.PropertyField(r1, element.FindPropertyRelative("MaxSteeringAngle"));
                }
            }
        }

        private SerializedProperty TryToGetClonedProp(int index, SerializedProperty prefabsInfoProp, SerializedProperty element, out SerializedProperty sourceProp, out int sourceIndex)
        {
            sourceProp = null;
            sourceIndex = -1;

            var cloneIdProp = element.FindPropertyRelative("CloneID");

            var settingsType = prefabsInfo[index].SettingsType;

            if (settingsType == SettingsType.CloneModel)
            {
                bool isEmptyID = IsEmptyID(cloneIdProp.stringValue);

                if (!isEmptyID)
                {
                    if (idToSettings.ContainsKey(cloneIdProp.stringValue) && idToSettings[cloneIdProp.stringValue].SettingsType != SettingsType.CloneModel)
                    {
                        var settings = idToSettings[cloneIdProp.stringValue];
                        sourceIndex = prefabsInfo.IndexOf(settings);

                        if (sourceIndex >= 0)
                        {
                            sourceProp = prefabsInfoProp.GetArrayElementAtIndex(sourceIndex);
                        }
                    }
                }
            }

            return null;
        }

        private void DrawAdditionalSettings(int index, float lineHeight, SerializedProperty element, ref Rect r1, bool enabled, SerializedProperty currentElement, bool settingsOverriden, SettingsType currentSettingsType)
        {
            switch (currentSettingsType)
            {
                case SettingsType.New:
                    {
                        DrawNewSettings(lineHeight, element, ref r1);

                        break;
                    }
                case SettingsType.Template:
                    {
                        r1.y += lineHeight;

                        EditorGUI.BeginChangeCheck();

                        var sourceIndex = prefabsInfo[index].LocalTemplateIndex;

                        if (templateHeaders.Length <= sourceIndex)
                        {
                            AssignTemplate(prefabsInfo[index]);
                        }

                        var newIndex = EditorGUI.Popup(r1, "Selected Template", sourceIndex, templateHeaders);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (sourceIndex != newIndex)
                            {
                                prefabsInfo[index].LocalTemplateIndex = newIndex;

                                if (newIndex == 0 || sourceIndex == 0)
                                {
                                    InitList();
                                }
                            }
                        }

                        var template = GetVehicleSettingsTemplate(prefabsInfo[index]);

                        if (template)
                        {
                            r1.y += lineHeight;
                            EditorGUI.ObjectField(r1, "Template", template, typeof(VehicleCustomTemplate), false);

                            r1.y += lineHeight;
                            EditorGUI.PropertyField(r1, element.FindPropertyRelative("CopySettingsType"));
                        }

                        break;
                    }
                case SettingsType.CloneModel:
                    {
                        GUI.enabled = true;

                        var currentCloneIdProp = currentElement.FindPropertyRelative("CloneID");
                        var cloneId = currentCloneIdProp.stringValue;

                        var cloneIDIndex = Array.IndexOf(availableIds, cloneId);

                        if (cloneIDIndex < 0)
                        {
                            cloneIDIndex = 0;
                        }

                        r1.y += lineHeight;

                        EditorGUI.BeginChangeCheck();

                        var newIndex = EditorGUI.Popup(r1, "Source Model", cloneIDIndex, availableIds);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (cloneIDIndex != newIndex)
                            {
                                var newID = availableIds[newIndex];

                                if (idToSettings.ContainsKey(newID) && idToSettings[newID].SettingsType != SettingsType.CloneModel)
                                {
                                    currentCloneIdProp.stringValue = newID;
                                }
                                else
                                {
                                    currentCloneIdProp.stringValue = EmptyID;
                                }
                            }
                        }

                        GUI.enabled = enabled;

                        if (!settingsOverriden)
                        {
                            DrawNewSettings(lineHeight, element, ref r1, !settingsOverriden);
                        }

                        break;
                    }
            }
        }

        private void DrawNewSettings(float lineHeight, SerializedProperty element, ref Rect r1, bool enabled = true)
        {
            GUI.enabled = enabled;

            r1.y += lineHeight;
            EditorGUI.PropertyField(r1, element.FindPropertyRelative("WheelRadius"));

            r1.y += lineHeight;
            EditorGUI.PropertyField(r1, element.FindPropertyRelative("WheelOffset"));

            GUI.enabled = true;

            GUI.enabled = CustomPhysics && enabled;

            r1.y += lineHeight;
            EditorGUI.PropertyField(r1, element.FindPropertyRelative("SuspensionLength"));

            GUI.enabled = true;
        }

        private void DrawHelpBox(Rect r1, int fieldCount, float lineHeight)
        {
            var helpBox = r1;

            helpBox.height = fieldCount * lineHeight;
            helpBox.x -= 4;
            helpBox.width += 8;

            EditorGUI.HelpBox(helpBox, string.Empty, MessageType.None);
        }

        private void SetShapeSize(PhysicsShapeAuthoring physicsShape, CarPrefabInfo prefabInfo, Vector3 shapeSize, Vector3 shapeCenter)
        {
            physicsShape.SetBox(new Unity.Physics.BoxGeometry()
            {
                Size = shapeSize + prefabInfo.SizeOffset.Value,
                Center = shapeCenter + prefabInfo.CenterOffset.Value,
                Orientation = Quaternion.identity,
                BevelRadius = prefabInfo.BevelRadius.Value,
            });
        }

        private void DrawLODList(ref Rect r1, int index, float lineHeight, List<LODMeshData> lodMeshes, string header, bool fbxOnly = false)
        {
            var prevR = r1;

            r1.y += lineHeight;

            var content = new GUIContent(header);
            var size = GUI.skin.label.CalcSize(content).x + 10;

            EditorGUI.PrefixLabel(r1, content);

            r1.x += size;
            r1.width -= size;

            var objs = InspectorExtension.DrawDropAreaGUI(r1, "Drag & drop FBX file");

            for (int i = 0; i < objs?.Length; i++)
            {
                if (objs[i] == null)
                {
                    continue;
                }

                if (!fbxOnly && objs[i] is Mesh)
                {
                    var newMesh = (Mesh)objs[i];
                    lodMeshes.TryToAdd(new LODMeshData(newMesh));
                }

                if (objs[i] is GameObject)
                {
                    var obj = objs[i] as GameObject;

                    var filters = obj.GetComponentsInChildren<MeshFilter>();

                    for (int j = 0; j < filters?.Length; j++)
                    {
                        MeshFilter filter = filters[j];

                        if (filter.sharedMesh != null)
                        {
                            lodMeshes.TryToAdd(new LODMeshData(filter));
                        }
                    }
                }
            }

            r1.x = prevR.x;

            int removeIndex = -1;

            for (int i = 0; i < lodMeshes.Count; i++)
            {
                Mesh mesh = lodMeshes[i].Mesh;

                r1.x = prevR.x;
                r1.y += lineHeight;
                r1.width = prevR.width - 30;

                EditorGUI.ObjectField(r1, mesh, typeof(Mesh), false);

                r1.x += r1.width + 5;
                r1.width = 25f;

                if (GUI.Button(r1, "x"))
                {
                    removeIndex = i;
                }
            }

            if (removeIndex != -1 && lodMeshes.Count > removeIndex)
            {
                lodMeshes.RemoveAt(removeIndex);
            }

            r1.x = prevR.x;
            r1.width = prevR.width;
            r1.height = prevR.height;
        }

        private void InitWheelMesh()
        {
            if (UserCustomVehicle)
                return;

            switch (WheelSourceType)
            {
                case WheelMeshSourceType.SharedFromModel:
                    {
                        for (int i = 0; i < prefabsInfo.Count; i++)
                        {
                            var allwheels = prefabsInfo[i].AllWheels;

                            if (allwheels.Count > 0)
                            {
                                int index = prefabsInfo[i].SelectedWheelMeshIndex;

                                if (index >= allwheels.Count)
                                {
                                    index = 0;
                                    prefabsInfo[i].SelectedWheelMeshIndex = 0;
                                }

                                prefabsInfo[i].SharedWheelMesh[0] = allwheels[index].GetComponent<MeshFilter>().sharedMesh;
                            }
                            else
                            {
                                prefabsInfo[i].SharedWheelMesh[0] = null;
                            }
                        }

                        break;
                    }
                case WheelMeshSourceType.SharedAll:
                    {
                        for (int i = 0; i < prefabsInfo.Count; i++)
                        {
                            prefabsInfo[i].SharedWheelMesh[0] = SharedWheelMesh;

                            if (HasLods)
                            {
                                prefabsInfo[i].SharedWheelMesh[1] = SharedWheelMeshLOD1;
                                prefabsInfo[i].SharedWheelMesh[2] = SharedWheelMeshLOD2;
                            }
                        }

                        break;
                    }
            }
        }

        private void InitLods()
        {
            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                if (prefabsInfo[i].HullMeshLODs == null || prefabsInfo[i].HullMeshLODs.Length != LodCount)
                {
                    prefabsInfo[i].HullMeshLODs = new Mesh[LodCount];
                }

                if (prefabsInfo[i].SourceMesh)
                {
                    prefabsInfo[i].HullMeshLODs[0] = prefabsInfo[i].SourceMesh.GetComponent<MeshFilter>().sharedMesh;
                }

                if (prefabsInfo[i].SharedWheelMesh == null || prefabsInfo[i].SharedWheelMesh.Length != LodCount)
                {
                    prefabsInfo[i].SharedWheelMesh = new Mesh[LodCount];
                }
            }
        }

        private string GetID(GameObject prefab)
        {
            switch (CurrentScanIDSourceType)
            {
                case ScanIDSourceType.BodyMesh:
                    var mesh = TryToGetHull(prefab, true);
                    return GetID(mesh);
                case ScanIDSourceType.PrefabName:
                    return prefab?.name ?? string.Empty;
            }

            return string.Empty;
        }

        private string GetID(MeshRenderer meshRenderer)
        {
            if (meshRenderer)
            {
                var filter = meshRenderer.GetComponent<MeshFilter>();
                return filter.sharedMesh?.name ?? string.Empty;
            }

            return string.Empty;
        }

        private void SaveCache()
        {
            if (!cacheContainer)
                return;

            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                if (HasTemplateSettings)
                {
                    string templateId = string.Empty;

                    if (prefabsInfo[i].LocalTemplateIndex > 0)
                    {
                        var template = templates[prefabsInfo[i].LocalTemplateIndex - 1];

                        if (template != null)
                        {
                            templateId = template.GUID;
                        }
                    }

                    prefabsInfo[i].TemplateID = templateId;
                }

                cacheContainer.AddCache(prefabsInfo[i]);
            }
        }

        private void LoadCache()
        {
            if (!cacheContainer)
                return;

            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                var cachedData = cacheContainer.GetCache(prefabsInfo[i]);

                if (cachedData != null)
                {
                    prefabsInfo[i].CloneCache(cachedData);
                    AssignTemplate(prefabsInfo[i]);
                }

                UpdateSettingsForPrefab(i);
            }
        }

        private void AssignTemplate(CarPrefabInfo carPrefabInfo)
        {
            VehicleCustomTemplate template = null;

            if (!string.IsNullOrEmpty(carPrefabInfo.TemplateID))
            {
                template = templates.Where(a => a.GUID == carPrefabInfo.TemplateID).FirstOrDefault();
            }

            if (template != null)
            {
                var index = Array.IndexOf(templates, template) + 1;
                carPrefabInfo.LocalTemplateIndex = index;
                carPrefabInfo.TemplateID = template.GUID;
            }
            else
            {
                carPrefabInfo.LocalTemplateIndex = 0;
                carPrefabInfo.TemplateID = string.Empty;
            }
        }

        private void LoadSettings()
        {
            if (prefabsInfo.Count > 0 && prefabsInfo[0].Prefab != null)
            {
                if (WheelRadius == 0 && prefabsInfo[0].AllWheels?.Count > 0)
                {
                    var wheel = prefabsInfo[0].AllWheels[0];
                    var currentWheelRadius = GetWheelBase(wheel);
                    WheelRadius = currentWheelRadius;
                }
            }
        }

        private MeshRenderer TryToGetHull(GameObject prefab, bool checkForExist = false)
        {
            MeshRenderer hullMesh;

            hullMesh = prefab.GetComponent<MeshRenderer>();

            if (!hullMesh)
            {
                hullMesh = FindMeshByText(prefab.transform, hullNameTemplates);
            }

            if (!hullMesh)
            {
                float vertexCount = 0;

                var meshes = prefab.GetComponentsInChildren<MeshFilter>();

                for (int i = 0; i < meshes?.Length; i++)
                {
                    var mesh = meshes[i].sharedMesh;

                    if (mesh != null && mesh.vertexCount > vertexCount)
                    {
                        vertexCount = mesh.vertexCount;
                        hullMesh = meshes[i].GetComponent<MeshRenderer>();
                    }
                }
            }

            if (checkForExist)
            {
                CheckForExist(hullMesh, prefab.name + " hull");
            }

            return hullMesh;
        }

        private void LoadAdditionalSettings()
        {
            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                var prefabInfo = prefabsInfo[i];

                MeshRenderer wheel = null;

                if (prefabInfo.AllWheels?.Count > 0)
                {
                    wheel = prefabInfo.AllWheels[0];
                }

                if (!prefabInfo.WheelRadius.Enabled)
                {
                    var wheelRadius = GetWheelBase(wheel);
                    prefabInfo.WheelRadius.Value = wheelRadius;
                }

                if (!prefabInfo.WheelOffset.Enabled)
                {
                    prefabInfo.WheelOffset.Value = WheelOffset;
                }

                if (!prefabInfo.SuspensionLength.Enabled)
                {
                    prefabInfo.SuspensionLength.Value = SuspensionLength;
                }

                if (!prefabInfo.AdditiveOffset.Enabled)
                {
                    prefabInfo.AdditiveOffset.Value = AdditiveOffset;
                }

                if (!prefabInfo.MaxSteeringAngle.Enabled)
                {
                    prefabInfo.MaxSteeringAngle.Value = MaxSteeringAngle;
                }

                if (!prefabInfo.SizeOffset.Enabled)
                {
                    prefabInfo.SizeOffset.Value = SizeOffset;
                }

                if (!prefabInfo.CenterOffset.Enabled)
                {
                    prefabInfo.CenterOffset.Value = CenterOffset;
                }

                if (!prefabInfo.BevelRadius.Enabled)
                {
                    prefabInfo.BevelRadius.Value = BevelRadius;
                }

                if (!prefabInfo.Mass.Enabled)
                {
                    prefabInfo.Mass.Value = Mass;
                }
            }
        }

        private void ApplyWheelSettings(int paramIndex)
        {
            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                switch (paramIndex)
                {
                    case 0:
                        {
                            if (!prefabsInfo[i].WheelRadius.Enabled)
                            {
                                prefabsInfo[i].WheelRadius.Value = WheelRadius;
                            }

                            break;
                        }
                    case 1:
                        {
                            if (!prefabsInfo[i].WheelOffset.Enabled)
                            {
                                prefabsInfo[i].WheelOffset.Value = WheelOffset;
                            }

                            break;
                        }
                    case 2:
                        {
                            if (!prefabsInfo[i].SuspensionLength.Enabled)
                            {
                                prefabsInfo[i].SuspensionLength.Value = SuspensionLength;
                            }

                            break;
                        }
                    case 3:
                        {
                            if (!prefabsInfo[i].AdditiveOffset.Enabled)
                            {
                                prefabsInfo[i].AdditiveOffset.Value = AdditiveOffset;
                            }
                            break;
                        }
                    case 4:
                        {
                            if (!prefabsInfo[i].MaxSteeringAngle.Enabled)
                            {
                                prefabsInfo[i].MaxSteeringAngle.Value = MaxSteeringAngle;
                            }

                            break;
                        }
                }
            }
        }

        private void ApplyPhysicsSettings(int paramIndex)
        {
            for (int i = 0; i < prefabsInfo.Count; i++)
            {
                switch (paramIndex)
                {
                    case 0:
                        {
                            if (!prefabsInfo[i].SizeOffset.Enabled)
                            {
                                prefabsInfo[i].SizeOffset.Value = SizeOffset;
                            }

                            break;
                        }
                    case 1:
                        {
                            if (!prefabsInfo[i].CenterOffset.Enabled)
                            {
                                prefabsInfo[i].CenterOffset.Value = CenterOffset;
                            }

                            break;
                        }
                    case 2:
                        {
                            if (!prefabsInfo[i].CenterOfMass.Enabled)
                            {
                                prefabsInfo[i].CenterOfMass.Value = CenterOfMass;
                            }

                            break;
                        }
                    case 3:
                        {
                            if (!prefabsInfo[i].BevelRadius.Enabled)
                            {
                                prefabsInfo[i].BevelRadius.Value = BevelRadius;
                            }

                            break;
                        }
                    case 4:
                        {
                            if (!prefabsInfo[i].Mass.Enabled)
                            {
                                prefabsInfo[i].Mass.Value = Mass;
                            }

                            break;
                        }
                }
            }
        }

        private VehicleCustomTemplate GetVehicleSettingsTemplate(CarPrefabInfo carPrefabInfo)
        {
            if (HasTemplateSettings && carPrefabInfo.SettingsType == SettingsType.Template && carPrefabInfo.LocalTemplateIndex > 0 && templates.Length > carPrefabInfo.LocalTemplateIndex - 1)
            {
                return templates[carPrefabInfo.LocalTemplateIndex - 1];
            }

            return null;
        }

        private void Clear()
        {
            Prefabs.Clear();
            prefabsInfo.Clear();
            SaveData();
        }

        private void SetIds()
        {
            List<string> ids = new List<string>() { EmptyID };
            idToSettings.Clear();

            foreach (var prefab in prefabsInfo)
            {
                if (RegisterId(prefab.ID, prefab))
                {
                    ids.Add(prefab.ID);
                }
            }

            availableIds = ids.ToArray();
        }

        private bool RegisterId(string id, CarPrefabInfo carPreab)
        {
            if (string.IsNullOrEmpty(id) || carPreab.Prefab == null)
            {
                return false;
            }

            if (!idToSettings.ContainsKey(id))
            {
                idToSettings.Add(id, carPreab);

                if (duplicateIds.Contains(carPreab.Prefab))
                {
                    duplicateIds.Remove(carPreab.Prefab);
                }

                return true;
            }
            else
            {
                if (!duplicateIds.Contains(carPreab.Prefab))
                {
                    duplicateIds.Add(carPreab.Prefab);
                }

                Debug.Log($"ID duplication found. SourcePrefab '{carPreab.Prefab.name}'. Duplicate ID '{id}'. Set the Scan ID source type to 'Prefab Name' in the Common tab, or set the ID for duplicates manually.");
            }

            return false;
        }

        private void ChangeId(int index, string previousId, string newId)
        {
            var prefabInfo = prefabsInfo[index];
            ChangeId(prefabInfo, previousId, newId);
        }

        private void ChangeId(CarPrefabInfo prefabInfo, string previousId, string newId)
        {
            if (newId == previousId)
                return;

            var availableIdsList = availableIds.ToList();

            if (idToSettings.ContainsKey(previousId) && (idToSettings[previousId] == null || idToSettings[previousId] == prefabInfo))
            {
                idToSettings.Remove(previousId);
                availableIdsList.Remove(previousId);
            }

            if (RegisterId(newId, prefabInfo))
            {
                availableIdsList.Add(newId);
            }

            availableIds = availableIdsList.ToArray();
        }

        private int TryToGetClonePrefabIndex(CarPrefabInfo carPrefabInfo)
        {
            if (carPrefabInfo.SettingsType == SettingsType.CloneModel && idToSettings.ContainsKey(carPrefabInfo.CloneID))
            {
                return prefabsInfo.IndexOf(idToSettings[carPrefabInfo.CloneID]);
            }

            return -1;
        }

        private CarPrefabInfo TryToGetClonePrefab(CarPrefabInfo carPrefabInfo)
        {
            var index = TryToGetClonePrefabIndex(carPrefabInfo);

            if (index >= 0)
            {
                return prefabsInfo[index];
            }

            return carPrefabInfo;
        }

        private bool IsEmptyID(string id) => string.IsNullOrEmpty(id) || id.Equals(EmptyID);

        private bool CustomId(CarPrefabInfo carPrefabInfo) => HybridMono || carPrefabInfo.CustomID;

        private void UpdateSettingsForPrefab(int index)
        {
            OnIdSourceChanged(index, false);
        }

        private void OnIdSourceChanged(int index, bool registerId = true)
        {
            var prefabInfo = prefabsInfo[index];

            switch (prefabInfo.IDSourceType)
            {
                case IDSourceType.Mesh:
                    {
                        prefabInfo.CustomID = false;
                        var mesh = TryToGetHull(prefabInfo.Prefab, true);
                        var previousId = prefabInfo.ID;
                        prefabInfo.ID = GetID(mesh);

                        if (registerId)
                            ChangeId(prefabInfo, previousId, prefabInfo.ID);

                        break;
                    }
                case IDSourceType.PrefabName:
                    {
                        prefabInfo.CustomID = false;
                        var previousId = prefabInfo.ID;
                        prefabInfo.ID = prefabInfo.Prefab.name;

                        if (registerId)
                            ChangeId(prefabInfo, previousId, prefabInfo.ID);
                        break;
                    }
                case IDSourceType.Custom:
                    {
                        prefabInfo.CustomID = true;

                        if (string.IsNullOrEmpty(prefabInfo.ID))
                        {
                            prefabInfo.ID = GetID(prefabInfo.Prefab);
                            SetIds();
                        }

                        break;
                    }
                default:
                    {
                        SetIds();
                        break;
                    }
            }
        }

        #endregion
    }
}
