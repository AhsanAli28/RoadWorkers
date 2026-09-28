#if UNITY_EDITOR
using Spirit604.CityEditor;
using Spirit604.Doc;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

#if DOTS_CITY && !DOTS_SIMULATION && !CINEMACHINE_V3
using Spirit604.DotsCity.Installer;
#endif

namespace Spirit604.PackageManagerExtension
{
    public class PackageDownloader : PackageWindowBase
    {
        private const string FirstLaunchKey = "DotsInitializeWindowKey";
        private const string ProjectVersion = "1.1.0";
        private const string EntitiesVersion = "1.2.0";
        private const string EntitiesVersionFull = "1.3.14";
        private const string SampleName = "com.unity.physics";
        private const string ToolBarKey = "ToolBarKey";
        private const string LegacyPackagePath = "Packages/Legacy/";
        private const string PackagePath = "Packages/Upgrades/";
        private const string URP_package = "com.unity.render-pipelines.universal";
        private const string HDRP_package = "com.unity.render-pipelines.high-definition";
        private readonly string[] ToolBarHeaders = new string[] { "Packages", "Pipeline", "Layer Settings", "Project Settings" };

        private GUIStyle headerGUIStyle;

#pragma warning disable 0414

        private bool allLoaded;

#pragma warning restore 0414

        private bool userDisable = true;
        public ProjectSettingsManager projectSettingsManager;
        public ProjectUnpacker projectUnpacker;

        // [InitializeOnLoadMethod] // Not reliable on Mac OS, EditorPrefs can be empty, replaced by PackageDownloaderPostprocessor.
        public static void OnLoadMethod()
        {
            bool launched = GetLaunchState();

#if !DOTS_CITY || UNITY_ISSUE_1
            launched = false;
#endif

            if (!launched)
            {
                EditorApplication.delayCall -= EditorApplication_delayCall;
                EditorApplication.delayCall += EditorApplication_delayCall;
            }
        }

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Package Initialization", priority = 100)]
        public static PackageDownloader ShowWindow()
        {
            if (EditorWindow.HasOpenInstances<PackageDownloader>())
            {
                var oldWindow = EditorWindow.GetWindow<PackageDownloader>();

                if (oldWindow != null)
                {
                    oldWindow.Close();
                }
            }

            var window = (PackageDownloader)GetWindow(typeof(PackageDownloader));
            window.titleContent = new GUIContent("Package Initialization");

            return window;
        }

        protected override Vector2 GetDefaultWindowSize() => new Vector2(500, 760);
        private string ProjectSettingsManagerPath => CityEditorBookmarks.GetPath("Prefabs/Init/ProjectSettingsManager.asset");
        private bool SettingsImported => projectSettingsManager != null && !projectSettingsManager.Required;

        private int ToolBarIndex
        {
#if DOTS_CITY
            get => EditorPrefs.GetInt(ToolBarKey, 0);
#else
            get => 0;
#endif
            set => EditorPrefs.SetInt(ToolBarKey, value);
        }

        protected override void OnEnable()
        {
            CityEditorStartup.ForceUpdateRoot();
            base.OnEnable();
            headerGUIStyle = new GUIStyle();
            headerGUIStyle.fontSize = 32;
            headerGUIStyle.normal.textColor = Color.white;
            headerGUIStyle.alignment = TextAnchor.UpperCenter;
            Init();
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (userDisable)
            {
                SaveLaunch();
            }

            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_beforeAssemblyReload;
        }

        protected override void OnGUI()
        {
            EditorGUILayout.LabelField("Welcome to DOTS Traffic City!", headerGUIStyle);
            EditorGUILayout.Space(30f);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(6));

            const string label = "First of all, you need to install the required packages.";
            float labelWidth = EditorStyles.label.CalcSize(new GUIContent(label)).x;

            var width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorGUIUtility.labelWidth = width;

            var r = GUILayoutUtility.GetLastRect();

            r.x += 330;
            r.width = 100;

#if UNITY_2023_1_OR_NEWER
            r.x += 20;
#endif

            if (EditorGUI.LinkButton(r, "(Documentation)"))
            {
                Application.OpenURL(DotsDocumentationReference.DocUrlGettingStarted);
            }

            EditorGUILayout.EndHorizontal();

#if UNITY_ISSUE_1

            EditorGUILayout.HelpBox($"Warning! Unity LTS versions 2022.3.40, 2022.3.41, 2022.3.42, 2022.3.43, 2022.3.44, 2022.3.45, 2022.3.46, 2022.3.47, 2022.3.48, 2022.3.49, 2022.3.50 are currently broken, plz install Unity 2022.3.51+ or Unity 6.0.23+", MessageType.Error);

            EditorGUILayout.BeginHorizontal();

            if (EditorGUILayout.LinkButton("Unity forum issue"))
            {
                Application.OpenURL("https://discussions.unity.com/t/missing-prefab-references-when-baking-a-subscene/1502057");
            }

            if (EditorGUILayout.LinkButton("Unity issue tracker"))
            {
                Application.OpenURL("https://issuetracker.unity3d.com/issues/missing-prefab-asset-error-in-a-subscene-after-domain-reload");
            }

            EditorGUILayout.EndHorizontal();
#endif

            EditorGUILayout.Space(10f);

            if (projectSettingsManager == null)
            {
                var soCurrent = new SerializedObject(this);
                soCurrent.Update();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(soCurrent.FindProperty(nameof(projectSettingsManager)));

                if (EditorGUI.EndChangeCheck())
                {
                    soCurrent.ApplyModifiedProperties();

                    if (projectSettingsManager)
                        projectSettingsManager.Refresh();
                }
            }

            EditorGUI.BeginChangeCheck();

#if !DOTS_CITY
            GUI.enabled = false;
#endif

            var newToolbarIndex = GUILayout.Toolbar(ToolBarIndex, ToolBarHeaders);

            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                ToolBarIndex = newToolbarIndex;

                if (newToolbarIndex == 1)
                {
                    projectSettingsManager.Refresh();
                }
            }

            switch (newToolbarIndex)
            {
                case 0:
                    {
                        ShowPackageGUI();
                        break;
                    }
                case 1:
                    {
                        if (projectSettingsManager != null)
                        {
                            var so = new SerializedObject(projectSettingsManager);
                            so.Update();

                            projectSettingsManager.DrawPipeline(so);
                            so.ApplyModifiedProperties();
                        }

                        break;
                    }
                case 2:
                    {
                        if (projectSettingsManager != null)
                        {
                            var so = new SerializedObject(projectSettingsManager);
                            so.Update();

                            projectSettingsManager.Draw(so);
                            so.ApplyModifiedProperties();
                        }

                        break;
                    }
                case 3:
                    {
                        if (projectSettingsManager != null)
                        {
                            var so = new SerializedObject(projectSettingsManager);
                            so.Update();

                            projectSettingsManager.DrawProjectSettings(so);
                            so.ApplyModifiedProperties();
                        }

                        break;
                    }
            }
        }

        protected override Dictionary<string, PackageData> LoadRequirePackageData()
        {
            var hdrp = new PackageData()
            {
                PackageName = HDRP_package,
                PackageLoadPath = HDRP_package,
#if !UNITY_URP && !UNITY_HDRP
                PackageVariant = true,
#endif
            };

            hdrp.OnProjectInstall = () =>
            {
                AssetDatabase.ImportPackage($"{GetAssetPath(PackagePath)}HDRP.unitypackage", false);
                hdrp.InstallStatus = InstallStatus.Installed;
                return true;
            };

            var requirePackages = new Dictionary<string, PackageData>()
            {
                { "com.unity.entities", new PackageData()
                {
                    PackageName = "com.unity.entities",
                    PackageLoadPath = $"com.unity.entities@{EntitiesVersionFull}",
                    MinVersion = EntitiesVersion,
#if UNITY_6000_1_OR_NEWER && !DOTS_SIMULATION && DOTS_CITY
                    OnProjectInstall = () =>
                    {
                        var camera = AssetDatabase.LoadAssetAtPath<GameObject>(CityEditorBookmarks.PREFAB_GAMEFLOW_ROOT_PATH + "Level/Common/Main Camera Base.prefab");

                        PrefabExtension.EditPrefab(camera, (prefabRoot) =>
                        {
                            var oldInput = prefabRoot.GetComponentInChildren<UnityEngine.EventSystems.StandaloneInputModule>();

                            if (oldInput != null)
                            {
                                oldInput.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                                DestroyImmediate(oldInput, true);
                            }
                        });

                        var runtimeUI = AssetDatabase.LoadAssetAtPath<GameObject>(CityEditorBookmarks.GetPath("Prefabs/RuntimeTileDemo/Scene/UI.prefab"));

                        PrefabExtension.EditPrefab(runtimeUI, (prefabRoot) =>
                        {
                            var oldInput = prefabRoot.GetComponentInChildren<UnityEngine.EventSystems.StandaloneInputModule>();

                            if (oldInput != null)
                            {
                                oldInput.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                                DestroyImmediate(oldInput, true);
                            }
                        });

                        return true;
                    }
#endif
                }},
                { "com.unity.entities.graphics", new PackageData()
                {
                    PackageName = "com.unity.entities.graphics",
                    PackageLoadPath = $"com.unity.entities.graphics@1.4.12",
                    MinVersion = EntitiesVersion
                }},
                { "com.unity.physics", new PackageData()
                {
                    PackageName = "com.unity.physics",
                    PackageLoadPath = $"com.unity.physics@{EntitiesVersionFull}",
                    MinVersion = EntitiesVersion
                }},
#if BURST_1_8_15
                { "com.unity.burst", new PackageData()
                {
                    PackageName = "com.unity.burst",
                    PackageLoadPath = $"com.unity.burst@1.8.16",
                    MinVersion = "1.8.16"
                }},
#endif
                { "com.unity.ai.navigation", new PackageData()
                {
                    PackageName = "com.unity.ai.navigation",
                    PackageLoadPath = "com.unity.ai.navigation",
                }},
#if !TEXT_MESH_PRO && !UNITY_2023_2_OR_NEWER
                { "com.unity.textmeshpro", new PackageData()
                {
                    PackageName = "com.unity.textmeshpro",
                    PackageLoadPath = "com.unity.textmeshpro@3.0.9",
                }},
#endif

#if !UNITY_URP && !UNITY_HDRP
                { URP_package, new PackageData()
                {
                    PackageName = URP_package,
                    PackageLoadPath = URP_package,
                    DefaultIndex = 0,
                    Variants = new List<PackageDataBase>()
                    {
                        new PackageData()
                        {
                            PackageName = URP_package,
                            PackageLoadPath = URP_package,
                        },
                        hdrp
                    }
                }},
                { HDRP_package, hdrp},
#elif UNITY_URP
                { URP_package, new PackageData()
                {
                    PackageName = URP_package,
                    PackageLoadPath = URP_package,
                }},
#else
                { HDRP_package, hdrp},
#endif
                { "com.unity.cinemachine", new PackageData()
                {
                    PackageName = "com.unity.cinemachine",
                    PackageLoadPath = "com.unity.cinemachine@3.1.3",
                    MinVersion = "3.0.0",
                    DefaultIndex = 0,
#if !CINEMACHINE_V3
                    VariantVersions = true,
                    Variants = new List<PackageDataBase>()
                    {
                        new PackageData()
                        {
                            PackageName = "com.unity.cinemachine 3.1.3",
                            PackageLoadPath = "com.unity.cinemachine@3.1.3",
                            MinVersion = "3.0.0",
                        },
                        new PackageData()
                        {
                            PackageName = "com.unity.cinemachine 2.10.3",
                            PackageLoadPath = "com.unity.cinemachine@2.10.3",
                            MinVersion = "2.0.0",
                            OnPackageInstall = () =>
                            {
                                AssetDatabase.ImportPackage($"{GetAssetPath(LegacyPackagePath)}Main Camera City CM_v2_legacy.unitypackage", false);
                            },
                            OnProjectInstall = () =>
                            {
#if !DOTS_SIMULATION && DOTS_CITY
                                var hub = AssetDatabase.LoadAssetAtPath<GameObject>(CityEditorBookmarks.CITY_BASE_PATH + "Hub.prefab");

                                PrefabExtension.EditPrefab(hub, (prefab) =>
                                {
                                    var camera = prefab.GetComponentInChildren<Camera>().gameObject;
                                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(camera);
                                    var uiInstaller = prefab.GetComponentInChildren<GameplayUIInstaller>();
                                    uiInstaller.MainCameraBase = camera;
                                });
#endif

                                return true;
                            }
                        },
                    }
#endif
                }
                },
                {
                    "com.svermeulen.extenject", new PackageData()
                    {
                        PackageName = "com.svermeulen.extenject@9.2.0-stcf3",
                        Description = "Library for injecting dependencies.",
                        ScopeName = "com.svermeulen.extenject",
                        PackageLoadPath = "https://package.openupm.com",
                        DescriptionUrl = "https://openupm.com/packages/com.svermeulen.extenject/",
                        Scope = true,
                        TargetFileToFind = "zenject",
                        Optional = true
                    }},
            };

            return requirePackages;
        }

        protected override Dictionary<string, PackageData> LoadOptionalPackageData()
        {
            var optionalPackages = new Dictionary<string, PackageData>()
            {
                { "com.reese.path", new PackageData()
                {
                    PackageName = "com.reese.path",
                    PackageLoadPath = "https://github.com/tawi1/ReeseUnityDemos.git?path=/Packages/com.reese.path",
                    CustomScriptDefine = "REESE_PATH",
                    Description = "Reese navigation package for DOTS navmesh navigation",
                    DescriptionUrl = "https://github.com/tawi1/ReeseUnityDemos",
                }},
            };

            return optionalPackages;
        }

        protected override Dictionary<string, AssetStorePackage> LoadAssetStorePackageData()
        {
            var assetStorePackages = new Dictionary<string, AssetStorePackage>()
            {
                { "FMODUnity", new AssetStorePackage()
                {
                    TargetFileToFind = "FMODUnity",
                    PackageName = "FMODUnity [optional]",
                    CustomScriptDefine = "FMOD",

                    AssetDownloadDatas = new List<AssetDownloadData>()
                    {
                        new AssetDownloadData()
                        {
                            DownloadUrl = "https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-161631",
                            Description = "FMOD plugin asset for Unity for playing sounds in the game."
                        },
                        new AssetDownloadData()
                        {
                            DownloadUrl = "https://www.fmod.com/download",
                            Description = "FMOD studio for editing sounds."
                        },
                    }
                }},
            };

            return assetStorePackages;
        }

        protected override List<string> LoadRequireScriptDefines()
        {
            return new List<string>() { "DOTS_CITY", "UNITY_PHYSICS_CUSTOM" };
        }

        protected override void PostProcessLoadedPackages(bool isLoaded)
        {
            if (!isLoaded)
            {
                RemoveKey();
                return;
            }

            UpdateManager.SetOrCreateVersion();

            projectUnpacker.TryToImportPackages();
            ConsoleExtension.ClearErrorsAndWarnings();

            var sample = GetCustomSample();

            if (!sample.isImported)
            {
                if (!sample.Equals(default) && !string.IsNullOrEmpty(sample.importPath))
                {
                    Debug.Log($"Importing sample {sample.importPath}");
                    sample.Import(Sample.ImportOptions.OverridePreviousImports);
                }
                else
                {
                    Debug.Log($"Importing sample {SampleName} failed");
                }
            }
            else
            {
                allLoaded = true;

                CheckInstallation();

                if (projectSettingsManager != null)
                {
                    OpenDemoScene();

                    projectSettingsManager.ValidatePipelineSettings();

                    if (projectSettingsManager.Required && projectSettingsManager.CleanProject && projectSettingsManager.CleanPipelineTemplate)
                    {
                        projectSettingsManager.CleanInstall = true;
                        projectSettingsManager.InstallScenes(true);
                        OpenDemoScene();
                        projectSettingsManager.ImportProjectSettings();
                    }

                    if (projectSettingsManager.Required && projectSettingsManager.CleanProject)
                    {
                        projectSettingsManager.Apply();
                    }

                    if (projectSettingsManager.CleanPipelineTemplate)
                    {
                        projectSettingsManager.InstallPipeline();
                    }
                }
            }
        }

        private void OpenDemoScene()
        {
            if (projectSettingsManager.CleanInstall && !projectSettingsManager.Opened)
            {
                projectSettingsManager.Opened = true;
                var demoPath = CityEditorBookmarks.GetPath("Scenes/Gameplay/Demo.unity");

                try
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(demoPath);
                }
                catch { }
            }
        }

        private void SaveLaunch()
        {
#if DOTS_CITY

            if (!allLoaded)
            {
                foreach (var item in requirePackages)
                {
                    if (item.Value.InstallStatus != InstallStatus.Installed && !item.Value.HasVariants && !item.Value.Optional)
                    {
                        Debug.Log($"PackageDownloader. Installation window closed, but the package '{item.Key}' not installed or outdated");
                    }
                }
            }

            if (!SettingsImported)
            {
                var nodeLayerIndex = LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME);

                if (nodeLayerIndex == -1)
                {
                    Debug.Log($"PackageDownloader. Installation window closed, but required layer '{ProjectConstants.TRAFFIC_NODE_LAYER_NAME}' not installed. In the Unity toolbar, open the 'Spirit604/CityEditor/Window/Package initialization' & open layer tab to install the '{ProjectConstants.TRAFFIC_NODE_LAYER_NAME}' layer");
                }

                var pedestrianNodeLayerIndex = LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME);

                if (pedestrianNodeLayerIndex == -1)
                {
                    Debug.Log($"PackageDownloader. Installation window closed, but required layer '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' not installed. In the Unity toolbar, open the 'Spirit604/CityEditor/Window/Package initialization' & open the layer tab to install the '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer");
                }
            }

#else
            Debug.Log($"PackageDownloader. 'DOTS_CITY' scripting define not found & the installation window closed. In the Unity toolbar, open the 'Spirit604/CityEditor/Window/Package initialization' window to continue the process");
#endif

            string key = GetLaunchKey();
            EditorPrefs.SetBool(key, true);
        }

        private Sample GetCustomSample()
        {
            var samples = Sample.FindByPackage(SampleName, string.Empty);

            if (samples != null && samples.Count() > 0)
            {
                foreach (var localSample in samples)
                {
                    if (localSample.importPath.Contains("Custom Physics Authoring"))
                    {
                        return localSample;
                    }
                }
            }

            return default;
        }

        private void RemoveKey()
        {
            var key = GetLaunchKey();

            if (EditorPrefs.HasKey(key))
            {
                EditorPrefs.DeleteKey(key);
            }
        }

        private static bool GetLaunchState()
        {
            string key = GetLaunchKey();
            var firstLaunch = EditorPrefs.GetBool(key, false);
            return firstLaunch;
        }

        private static string GetLaunchKey() => $"{EditorExtension.GetUniquePrefsKey(FirstLaunchKey)}_{ProjectVersion}";

        private static string GetAssetPath(string path) => CityEditorBookmarks.GetPath(path);

        private void Init()
        {
            if (projectSettingsManager == null)
            {
                projectSettingsManager = AssetDatabase.LoadAssetAtPath<ProjectSettingsManager>(ProjectSettingsManagerPath);
            }

            if (projectUnpacker == null)
            {
                projectUnpacker = ProjectUnpacker.GetUnpacker();
            }

            if (projectSettingsManager)
            {
                projectSettingsManager.Refresh();

                if (projectSettingsManager.Required)
                {
                    RemoveKey();
                }
            }

            ConsoleExtension.ClearErrorsAndWarnings();
        }

        private void CheckInstallation()
        {
            ForEach(currentPackage =>
            {
                currentPackage.RaiseProjectInstall();
            });
        }

        private static void WaitForUpdate()
        {
            if (EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= WaitForUpdate;
            ShowWindow();
        }

        private void AssemblyReloadEvents_beforeAssemblyReload()
        {
            userDisable = false;
            Close();
        }

        private static void EditorApplication_delayCall()
        {
            EditorApplication.delayCall -= EditorApplication_delayCall;

            if (EditorApplication.isUpdating)
            {
                EditorApplication.update -= WaitForUpdate;
                EditorApplication.update += WaitForUpdate;
            }
            else
            {
                ShowWindow();
            }
        }
    }
}
#endif