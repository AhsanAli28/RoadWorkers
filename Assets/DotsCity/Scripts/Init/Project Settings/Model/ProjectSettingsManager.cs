using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spirit604.PackageManagerExtension
{
    //[CreateAssetMenu(fileName = "ProjectSettingsManager")]
    public class ProjectSettingsManager : ScriptableObject
    {
        private const string NavMeshAreasAsset = "NavMeshAreas";
        private const string TagManagerAsset = "TagManager";
        private const string DynamicsManagerAsset = "DynamicsManager";
        private const string GroupTip = "GroupTip";
        private const string PackagePath = "Packages/Upgrades/";
        private const int DefaultProjectSceneCount = 17;

        private readonly HashSet<int> DefaultLayers = new HashSet<int>() { 0, 1, 2, 4, 5 };

#if UNITY_6000_0_OR_NEWER
        private readonly HashSet<string> DefaultPipelineListURP = new HashSet<string> { "Mobile_RPAsset", "PC_RPAsset" };
#else
        private readonly HashSet<string> DefaultPipelineListURP = new HashSet<string> { "URP-Performant", "URP-Balanced", "URP-HighFidelity" };
#endif

        private readonly HashSet<string> DefaultPipelineListHDRP = new HashSet<string> { "HDRP High Fidelity", "HDRP Balanced", "HDRP Performant" };

        [Serializable]
        public class GroupData
        {
            public string Name;
            public bool Enabled = true;
        }

        [Serializable]
        public class LayerData
        {
            public int Layer;
            public string Name;
        }

        public List<GroupData> Groups = new List<GroupData>()
        {
            new GroupData()
            {
                Name = "Traffic Simulation", //0
                Enabled = true,
            },
            new GroupData()
            {
                Name = "NavMesh Layers", //1
                Enabled = true,
            },
            new GroupData()
            {
                Name = "Physics", //2
                Enabled = true,
            },
            new GroupData()
            {
                Name = "Demo Scene", //3
                Enabled = true,
            },
            new GroupData()
            {
                Name = "Demo Mono Scene", //4
                Enabled = true,
            },
        };

        [SerializeField]
        private MonoScript constansScript;

        [SerializeField]
        private bool importAllProjectLayers = true;

        [SerializeField]
        private List<ProjectLayerData> layers = new List<ProjectLayerData>();

        [SerializeField]
        private List<string> navMeshLayers = new List<string>()
        {
            "Humanoid",
            "Car",
        };

        [SerializeField]
        private List<SceneAsset> projectScenes = new List<SceneAsset>();

        [SerializeField]
        private bool cleanInstall;

        [SerializeField]
        private bool opened;

        private List<RenderPipelineAsset> assets = new List<RenderPipelineAsset>();

        private Dictionary<int, string> projectLayers = new Dictionary<int, string>();
        private Dictionary<string, int> nameToLayers = new Dictionary<string, int>();
        private Dictionary<int, ProjectLayerData> sourceToData = new Dictionary<int, ProjectLayerData>();
        private List<RenderPipelineAsset> projectAssets = new List<RenderPipelineAsset>();

        public bool Available { get; private set; }
        public bool Required { get; private set; }
        public bool CleanProject => projectLayers.Count == 0;
        public bool CleanPipelineTemplate { get; set; }

        public bool CleanInstall
        {
            get => cleanInstall;
            set
            {
                cleanInstall = value;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public bool Opened
        {
            get => opened;
            set
            {
                opened = value;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void ValidatePipelineSettings()
        {
            var listSource = DefaultPipelineListURP;

#if UNITY_HDRP
            listSource = DefaultPipelineListHDRP;
#endif

            bool defaultTemplateFlag = true;
            bool allNulls = true;

            for (int i = 0; i < QualitySettings.count; i++)
            {
                var q = QualitySettings.GetRenderPipelineAssetAt(i);

                if (q != null)
                {
                    allNulls = false;

                    if (!listSource.Contains(q.name))
                    {
                        defaultTemplateFlag = false;
                        break;
                    }
                }
                else
                {
                    defaultTemplateFlag = false;
                }
            }

            assets = AssetDatabaseExtension.TryGetUnityObjectsOfTypeFromPath<RenderPipelineAsset>("Assets/DotsCity/Configurations/Graphics");

            CleanPipelineTemplate = defaultTemplateFlag || allNulls;
        }

        public void DrawPipeline(SerializedObject so)
        {
            projectAssets.Clear();

            InspectorExtension.DrawDefaultInspectorGroupBlock("Project Quality Levels", () =>
            {
                for (int i = 0; i < QualitySettings.count; i++)
                {
                    var q = QualitySettings.GetRenderPipelineAssetAt(i);
                    var qualityName = q != null ? q.name : "NaN";
                    GUILayout.Label($"{i + 1}) {qualityName}");

                    if (q)
                        projectAssets.Add(q);
                }
            });

            InspectorExtension.DrawDefaultInspectorGroupBlock("New Quality Levels", () =>
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    var asset = assets[i];
                    GUILayout.Label($"{i + 1}) {asset.name}");
                }
            });

            bool allExist = true;

            for (int i = 0; i < assets.Count; i++)
            {
                if (!projectAssets.Any(a => a.name == assets[i].name))
                {
                    allExist = false;
                    break;
                }
            }

            if (allExist)
            {
                EditorGUILayout.HelpBox("All default pipelines added.", MessageType.Info);
            }

            if (GUILayout.Button("Import Graphics"))
            {
                InstallPipeline();
            }
        }

        public void InstallPipeline()
        {
            string pipeline = string.Empty;

#if UNITY_URP
            pipeline = "URP";
#endif
#if UNITY_HDRP
            pipeline = "HDRP";
#endif

            InstallPipeline(pipeline);
        }

        public void InstallPipeline(string pipeline)
        {
            CloneAsset("GraphicsSettings", $"{pipeline}");
            CloneAsset("QualitySettings", $"{pipeline}");

            ConsoleExtension.ClearErrorsAndWarnings();
            Debug.Log($"{pipeline} GraphicsSettings imported.");
        }

        public void InstallScenes(bool overwrite = false)
        {
            var list = projectScenes.Select(a => AssetDatabase.GetAssetPath(a));

            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();

            if (!overwrite)
            {
                editorBuildSettingsScenes.AddRange(EditorBuildSettings.scenes);
            }

            int count = 0;

            foreach (var scenePath in list)
            {
                var any = editorBuildSettingsScenes.Any(a => a.path == scenePath);

                if (!any)
                {
                    editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    count++;
                }
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();

            Debug.Log($"Added {count} scenes to EditorBuildSettings");
        }

        public void DrawProjectSettings(SerializedObject so)
        {
            GUILayout.Label("Project scenes:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(projectScenes)));

            if (EditorBuildSettings.scenes.Count() == DefaultProjectSceneCount)
            {
                EditorGUILayout.HelpBox("All project scenes added.", MessageType.Info);
            }

            if (GUILayout.Button("Import Android Gradle"))
            {
                ImportProjectSettings(true);
            }

            if (GUILayout.Button("Add All Scenes To Build"))
            {
                InstallScenes();
            }
        }

        public void Draw(SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty(nameof(importAllProjectLayers)));

            GUI.enabled = !importAllProjectLayers;

            GUILayout.BeginVertical("HelpBox");

            GUILayout.Label("Layer Groups", EditorStyles.boldLabel);

            for (int i = 0; i < Groups.Count; i++)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(Groups[i].Name);

                using (new EditorGUI.DisabledScope(i == 0))
                {
                    EditorGUI.BeginChangeCheck();

                    var flag = EditorGUILayout.Toggle(Groups[i].Enabled, GUILayout.MaxWidth(25f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        Groups[i].Enabled = flag;
                        Validate();
                        EditorSaver.SetObjectDirty(this);
                    }
                }

                GUILayout.EndHorizontal();
            }

            if (!importAllProjectLayers)
            {
                EditorTipExtension.TryToShowInspectorTip(GroupTip, "Select layer groups according to the features you want to use.");
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("HelpBox");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("HelpBox");

            GUILayout.Label("Current Project Layers", EditorStyles.boldLabel);

            foreach (var item in projectLayers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.Value);
                EditorGUILayout.LabelField($"[{item.Key}]", EditorStyles.boldLabel, GUILayout.MaxWidth(35));
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("HelpBox");

            GUILayout.Label("New Layers", EditorStyles.boldLabel);

            foreach (var layerData in layers)
            {
                if (!Groups[layerData.Group].Enabled)
                    continue;

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();

                var newLayer = EditorGUILayout.IntField(layerData.CurrentLayer, GUILayout.MaxWidth(35));

                if (EditorGUI.EndChangeCheck())
                {
                    layerData.CurrentLayer = Mathf.Clamp(newLayer, 0, 31);
                    Validate(layerData);
                    EditorSaver.SetObjectDirty(this);
                }

                EditorGUILayout.LabelField(layerData.Name, GUILayout.MaxWidth(150));

                switch (layerData.CurrentStatus)
                {
                    case ProjectLayerData.Status.NewLayer:
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_AS Badge New"));
                        break;
                    case ProjectLayerData.Status.Exist:
                        break;
                    case ProjectLayerData.Status.Collision:
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.erroricon@2x"));
                        break;
                    case ProjectLayerData.Status.ExistWithOtherIndex:
                        EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.warnicon@2x"));
                        break;
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (Groups[1].Enabled)
            {
                GUILayout.BeginVertical("HelpBox");

                GUILayout.Label("NavMesh Layers", EditorStyles.boldLabel);

                for (int i = 0; i < navMeshLayers.Count; i++)
                {
                    EditorGUILayout.LabelField(navMeshLayers[i]);
                }

                GUILayout.EndVertical();
            }

            GUI.enabled = true;

            if (!Required && Available)
            {
                EditorGUILayout.HelpBox("Project layers are already imported.", MessageType.Info);
            }

            if (!importAllProjectLayers)
            {
                if (GUILayout.Button("Import Collision Matrix"))
                {
                    CloneCollisionMatrix(true);
                }
            }

            if (GUILayout.Button("Apply"))
            {
                Apply();
            }
        }

        public void Refresh(bool includeValidation = true)
        {
            ValidatePipelineSettings();

            projectLayers.Clear();
            nameToLayers.Clear();

            var editorLayers = UnityEditorInternal.InternalEditorUtility.layers;

            foreach (var layer in editorLayers)
            {
                var layerIndex = LayerMask.NameToLayer(layer);

                if (!DefaultLayers.Contains(layerIndex))
                {
                    projectLayers.Add(layerIndex, layer);
                    nameToLayers.Add(layer, layerIndex);
                }
            }

            sourceToData.Clear();

            foreach (var layerData in layers)
            {
                sourceToData.Add(layerData.SourceLayer, layerData);
            }

            if (includeValidation)
            {
                Validate();
            }
        }

        public void ImportProjectSettings(bool force = false)
        {
            var projectSo = GetAsset(GetProjectAssetPath("ProjectSettings"));

            if (projectSo != null)
            {
                projectSo.Update();

                var companyNameProp = ProjectSettingsUtility.FindProperty(projectSo, "companyName");

                if (companyNameProp.stringValue == "DefaultCompany" || force)
                {
                    companyNameProp.stringValue = "Spirit604";
                    var productNameProp = ProjectSettingsUtility.FindProperty(projectSo, "productName");
                    productNameProp.stringValue = "DotsCity";

                    var prop1 = ProjectSettingsUtility.FindProperty(projectSo, "useCustomMainGradleTemplate");
                    var prop2 = ProjectSettingsUtility.FindProperty(projectSo, "useCustomLauncherGradleManifest");
                    var prop3 = ProjectSettingsUtility.FindProperty(projectSo, "useCustomBaseGradleTemplate");
                    var prop4 = ProjectSettingsUtility.FindProperty(projectSo, "useCustomGradlePropertiesTemplate");

                    if (prop1 != null && prop2 != null && prop3 != null && prop4 != null)
                    {
                        prop1.boolValue = prop2.boolValue = prop3.boolValue = prop4.boolValue = true;
                    }
                    else
                    {
                        Debug.Log("ImportProjectSettings. Some of Android manifest properties not found.");
                    }

                    try
                    {
                        var overrideDefaultApplicationIdentifierProp = ProjectSettingsUtility.FindProperty(projectSo, "overrideDefaultApplicationIdentifier");
                        overrideDefaultApplicationIdentifierProp.boolValue = false;
                    }
                    catch { }

                    projectSo.ApplyModifiedProperties();
                    AssetDatabase.Refresh();

                    AssetDatabase.ImportPackage($"{GetAssetPath(PackagePath)}AndroidManifestPackage.unitypackage", false);

#if UNITY_6000_0_OR_NEWER
                    AssetDatabase.ImportPackage($"{GetAssetPath(PackagePath)}Unity6_Gradle.unitypackage", false);
#endif

                    Debug.Log("Project player settings imported.");
                }
            }
            else
            {
                Debug.Log("ImportProjectSettings. Project settings not found.");
            }
        }

        public void Apply()
        {
            if (importAllProjectLayers)
            {
                CloneLayers();
                CloneCollisionMatrix();
                CloneNavMeshAreas();
                Refresh();
                Debug.Log("Project layer settings successfully cloned.");
                return;
            }

            Validate();

            if (Available && !Required)
            {
                Debug.Log("Project already contain all required layers.");
            }

            if (Available && Required)
            {
                var strings = ProjectSettingsUtility.GetStrings(constansScript);
                bool layerChanged = false;

                foreach (var item in layers)
                {
                    if (item.UpdateLayer(ref strings))
                    {
                        layerChanged = true;
                    }
                }

                if (layerChanged)
                {
                    EditorSaver.SetObjectDirty(this);
                    var assetPath = AssetDatabase.GetAssetPath(constansScript);
                    var scriptPath = Application.dataPath + assetPath.Substring(assetPath.IndexOf("/"));

                    try
                    {
                        using (FileStream stream = File.Open(scriptPath, FileMode.Open, FileAccess.Write))
                        {
                            stream.SetLength(0);

                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                StringBuilder builder = new StringBuilder();

                                for (int i = 0; i < strings.Length; i++)
                                {
                                    string line = strings[i];
                                    builder.AppendLine(line);
                                }

                                writer.Write(builder.ToString());
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    AssetDatabase.Refresh();
                }

                Refresh();
            }

            AddNavMesh();
        }

        private static string GetAssetPath(string relativePath) => CityEditorBookmarks.GetPath(relativePath);

        private void CloneNavMeshAreas(bool interactive = false)
        {
            ImportPackage(NavMeshAreasAsset, interactive);
        }

        private void CloneCollisionMatrix(bool interactive = false)
        {
            ImportPackage(DynamicsManagerAsset, interactive);
        }

        private void CloneLayers(bool interactive = false)
        {
            ImportPackage(TagManagerAsset, interactive);
        }

        private void ImportPackage(string assetName, bool interactive = false, string cloneAsset = "")
        {
            try
            {
                var packagePath = GetClonePackagePath($"{assetName}{cloneAsset}");
                AssetDatabase.ImportPackage(packagePath, interactive);
                Debug.Log($"{packagePath} imported.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void CloneAsset(string assetName, string cloneAsset = "")
        {
            var projectAssetPath = GetProjectAssetPath(assetName);
            var projectSo = GetAsset(projectAssetPath);
            var cloneSo = GetAsset(GetCloneAssetPath($"{assetName}{cloneAsset}"));

            SerializedProperty sourceIterator = cloneSo.GetIterator();

            while (sourceIterator.NextVisible(true))
            {
                var prop = projectSo.FindProperty(sourceIterator.propertyPath);

                try
                {
                    if (!prop.isArray)
                    {
                        var boxedValue = sourceIterator.boxedValue;

                        if (boxedValue != null)
                        {
                            prop.boxedValue = boxedValue;
                        }
                    }
                    else
                    {
                        prop.arraySize = sourceIterator.arraySize;

                        for (int i = 0; i < sourceIterator.arraySize; i++)
                        {
                            prop.GetArrayElementAtIndex(i).boxedValue = sourceIterator.GetArrayElementAtIndex(i).boxedValue;
                        }
                    }
                }
                catch { }
            }

            projectSo.ApplyModifiedProperties();

            Debug.Log($"{projectAssetPath} imported.");
        }

        private void ExportPackage(string assetName)
        {
            AssetDatabase.ExportPackage(GetProjectAssetPath(assetName), assetName + "Clone.unitypackage", ExportPackageOptions.Default);
        }

        private SerializedObject GetAsset(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            if (assets.Length > 0)
            {
                var asset = assets[0];

                if (asset != null)
                    return new SerializedObject(asset);
            }

            return null;
        }

        private void AddNavMesh()
        {
            if (Groups[1].Enabled)
            {
                var asset = GetAsset(GetProjectAssetPath(NavMeshAreasAsset));
                var prop = ProjectSettingsUtility.FindProperty(asset, "m_SettingNames");
                prop.arraySize = 0;

                asset.ApplyModifiedProperties();
                asset.Update();

                prop.arraySize = navMeshLayers.Count;

                for (int i = 0; i < navMeshLayers.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).stringValue = navMeshLayers[i];
                }

                asset.ApplyModifiedProperties();
            }
        }

        private bool Validate()
        {
            Validate(out var available, out var required);
            Available = available;
            Required = required;
            return required;
        }

        private void Validate(out bool available, out bool required)
        {
            available = true;
            required = false;

            foreach (var item in layers)
            {
                if (!Groups[item.Group].Enabled)
                    continue;

                Validate(item, ref available, ref required);
            }
        }

        private void Validate(ProjectLayerData item)
        {
            bool available = true;
            bool required = false;
            Validate(item, ref available, ref required);
        }

        private void Validate(ProjectLayerData item, ref bool available, ref bool required)
        {
            if (DefaultLayers.Contains(item.CurrentLayer))
            {
                item.CurrentStatus = ProjectLayerData.Status.Collision;
                available = false;
            }

            if (projectLayers.ContainsKey(item.CurrentLayer))
            {
                if (projectLayers[item.CurrentLayer] != item.Name)
                {
                    if (!nameToLayers.ContainsKey(item.Name))
                    {
                        available = false;
                        item.CurrentStatus = ProjectLayerData.Status.Collision;
                    }
                    else
                    {
                        item.ExistProjectLayer = nameToLayers[item.Name];
                        item.CurrentStatus = ProjectLayerData.Status.ExistWithOtherIndex;
                        required = true;
                    }
                }
                else
                {
                    item.CurrentStatus = ProjectLayerData.Status.Exist;
                }
            }
            else
            {
                if (!nameToLayers.ContainsKey(item.Name))
                {
                    item.CurrentStatus = ProjectLayerData.Status.NewLayer;
                }
                else
                {
                    item.ExistProjectLayer = nameToLayers[item.Name];
                    item.CurrentStatus = ProjectLayerData.Status.ExistWithOtherIndex;
                }

                required = true;
            }
        }

        private string GetCloneAssetPath(string assetName) => CityEditorBookmarks.GetPath($"Prefabs/Init/{assetName}Clone.asset");
        private string GetClonePackagePath(string assetName) => CityEditorBookmarks.GetPath($"Prefabs/Init/{assetName}Clone.unitypackage");
        private string GetProjectAssetPath(string assetName) => $"ProjectSettings/{assetName}.asset";
    }
}
