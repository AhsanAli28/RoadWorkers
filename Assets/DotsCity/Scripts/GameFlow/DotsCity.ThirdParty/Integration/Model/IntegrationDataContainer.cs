using Spirit604.DotsCity.EditorTools;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_URP
using UnityEditor.Rendering.Universal;
#endif
#if UNITY_HDRP
using UnityEditor.Rendering.HighDefinition;
using System.Reflection;
#endif

namespace Spirit604.DotsCity.ThirdParty.Integration
{
    public class IntegrationDataContainer : ScriptableObject
    {
        private const string AssetsParentName = "Assets";

        public const string EntitySubSceneName = "EntitySubScene";
        public static readonly string[] TargetSceneNames = new[] { "CityDebugger", "Hub", EntitySubSceneName };

        public bool SettingsFlag = true;
        public bool GeneratedCacheFlag;

        public bool AddOffset = true;

        public Vector3 GlobalOffset;

        public bool OverrideMaterial;
        public string subsceneRelativeAssetPath;
        public string subsceneRelativeFolderPath;
        public bool reparentAssets;
        public bool reparentVehicles;
        public bool replaceLight;
        public bool convertMaterials;
        public string cleanVehiclePhrase;
        public int qualityIndex = -1;

        public string cacheMaterialName;
        public CacheContainer cacheContainer;
        public List<SkinMeshData> SkinDatas = new List<SkinMeshData>();
        public List<MaterialData> Materials = new List<MaterialData>();
        public List<Material> ClonedMaterials = new List<Material>();
        public List<GameObject> Prefabs = new List<GameObject>();
        public List<ScriptableObject> Presets = new List<ScriptableObject>();

        public List<SkinnedCharacterData> LegacySkins = new List<SkinnedCharacterData>();
        public List<SkinnedCharacterData> Ragdolls = new List<SkinnedCharacterData>();
        public List<string> CacheContainerPrefabs = new List<string>();

        public SceneAsset ContentScene;
        public SceneAsset Subscene;

        private string tempScenePath;
        private Scene activeScene;
        private Dictionary<string, Material> cachedMaterials = new Dictionary<string, Material>();

        public void Integrate()
        {
            ClearTemp();

            if (AddOffset && GlobalOffset != Vector3.zero)
            {
                if (!EditorUtility.DisplayDialog("Warning", "Integration package has a global offset, make sure that you have made a backup", "Continue", "Cancel"))
                {
                    return;
                }
            }

            if (convertMaterials)
            {
#if UNITY_URP
                Converters.RunInBatchMode(ConverterContainerId.BuiltInToURP, new List<ConverterId>() { ConverterId.Material }, ConverterFilter.Inclusive);
#endif

#if UNITY_HDRP
                try
                {
                    MethodInfo method = null;

                    var type = TypeHelper.ByName("UpgradeStandardShaderMaterials");

                    if (type != null)
                        method = type.GetMethod("UpgradeMaterialsProject", BindingFlags.Static | BindingFlags.NonPublic);

                    if (method != null)
                    {
                        method.Invoke(null, null);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"HDRP material convert failed. UpgradeStandardShaderMaterials.UpgradeMaterialsProject method not found.");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
#endif
            }

            if (qualityIndex >= 0)
            {
                QualitySettings.SetQualityLevel(qualityIndex);
            }

            activeScene = IntegrationExtension.GetActiveScene();

            var hub = ObjectUtils.FindObjectOfType<EntityRootSubsceneGenerator>();

            if (hub)
            {
                if (!EditorUtility.DisplayDialog("Warning", "The scene already contains city components", "Replace", "Cancel"))
                {
                    return;
                }

                var roots = activeScene.GetRootGameObjects();

                foreach (var root in roots)
                {
                    if (TargetSceneNames.Contains(root.name))
                    {
                        DestroyImmediate(root.gameObject);
                    }
                }
            }

            PreprocessScene();

            var sourceContentPath = AssetDatabase.GetAssetPath(ContentScene);
            tempScenePath = sourceContentPath;
            var index = tempScenePath.LastIndexOf(".");

            tempScenePath = tempScenePath.Insert(index, "_temp");

            if (AssetDatabase.CopyAsset(sourceContentPath, tempScenePath))
            {
                EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
                EditorSceneManager.OpenScene(tempScenePath, OpenSceneMode.Additive);
            }
        }

        public void Clear()
        {
            ClearInternal();
        }

        private void ClearTemp()
        {
            cachedMaterials.Clear();
        }

        private void PreprocessScene()
        {
            var light = ObjectUtils.FindObjectOfType<Light>();

            if (light != null)
            {
                if (!replaceLight)
                {
                    light.transform.SetParent(null);
                }
                else
                {
                    DestroyImmediate(light.gameObject);
                }
            }
        }

        private void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;

            var currentRoots = activeScene.GetRootGameObjects();

            if (AddOffset && GlobalOffset != Vector3.zero)
            {
                foreach (var root in currentRoots)
                {
                    root.transform.position += GlobalOffset;
                }
            }

            var cam = currentRoots.Where(a => a.GetComponentInChildren<Camera>() != null).FirstOrDefault();

            if (cam != null)
            {
                DestroyImmediate(cam.gameObject);
            }

            if (reparentAssets)
            {
                Transform assetsParent = null;

                var assetParentTemp = GameObject.Find(AssetsParentName);

                if (assetParentTemp)
                {
                    assetsParent = assetParentTemp.transform;
                }
                else
                {
                    assetsParent = new GameObject(AssetsParentName).transform;
                }

                for (int i = 0; i < currentRoots.Length; i++)
                {
                    if (!currentRoots[i])
                    {
                        continue;
                    }

                    var componentCount = 0;

                    var objs = currentRoots[i].GetComponents<Component>();

                    if (objs?.Length > 0)
                    {
                        componentCount = objs.Length;
                    }

                    if (componentCount > 1)
                    {
                        currentRoots[i].transform.SetParent(assetsParent.transform);
                    }
                }
            }

            if (!string.IsNullOrEmpty(cleanVehiclePhrase))
            {
                Transform vehiclesParent = null;

                if (reparentVehicles)
                {
                    vehiclesParent = new GameObject("Cars").transform;
                }

                var vehicles = ObjectUtils.FindObjectsOfType<Transform>().Where(a => a.name.Contains(cleanVehiclePhrase) && PrefabUtility.GetCorrespondingObjectFromSource(a) != null);

                if (vehicles != null)
                {
                    foreach (var vehicle in vehicles)
                    {
                        vehicle.gameObject.SetActive(false);

                        if (vehiclesParent)
                        {
                            vehicle.transform.SetParent(vehiclesParent);
                        }
                    }
                }
            }

            EditorSceneManager.MergeScenes(scene, activeScene);

            var roots = activeScene.GetRootGameObjects();

            EntityRootSubsceneGenerator hub = null;

            try
            {
                hub = roots.Where(a => a.GetComponent<EntityRootSubsceneGenerator>()).FirstOrDefault().GetComponent<EntityRootSubsceneGenerator>();
            }
            catch
            {
                UnityEngine.Debug.LogError($"Integration. Hub root object not found");
            }

            var sourceSubscenePath = AssetDatabase.GetAssetPath(Subscene);

            var newSubsceneFolder = activeScene.path;
            newSubsceneFolder = newSubsceneFolder.Replace(".unity", "/");

            AssetDatabaseExtension.CheckForFolderExist(newSubsceneFolder);

            var newSubscenePath = $"{newSubsceneFolder}{Subscene.name}.unity";

            AssetDatabaseExtension.CheckForExistAsset(newSubscenePath, true);

            if (AssetDatabase.CopyAsset(sourceSubscenePath, newSubscenePath))
            {
                var newSubscene = AssetDatabase.LoadAssetAtPath<SceneAsset>(newSubscenePath);

                var subScene = IntegrationExtension.GetObjectByName<SubScene>(activeScene, EntitySubSceneName);

                if (!subScene)
                {
                    subScene = new GameObject(EntitySubSceneName).AddComponent<SubScene>();
                    EditorSceneManager.MoveGameObjectToScene(subScene.gameObject, activeScene);
                    UnityEngine.Debug.Log($"Integration. SubScene auto created");
                }

                if (subScene)
                {
                    subScene.SceneAsset = newSubscene;
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Integration. Copy subscene failed. Source {sourceSubscenePath} New folder {newSubsceneFolder} New asset path {newSubscenePath}");
            }

            UnpackInternal();

            if (hub)
            {
                UnpackPedestrianData(hub.gameObject);
            }

            try
            {
                AssetDatabase.DeleteAsset(tempScenePath);
            }
            catch { }

            UnityEngine.Debug.Log($"Template successfully unpacked");
        }

        private void UnpackInternal()
        {
            UnpackMaterial();
            UnpackVehicles();
            UnpackOther();
        }

        private void UnpackMaterial()
        {
            var materials = Materials;

            foreach (var materialData in materials)
            {
                if (!materialData.Material)
                {
                    continue;
                }

                Texture2D texture = FindTexture(materialData.TextureName);

                if (texture != null)
                {
                    materialData.Material.mainTexture = texture;
                    EditorSaver.SetObjectDirty(materialData.Material);
                }
            }
        }

        private void UnpackVehicles()
        {
            var vehicles = Prefabs;

            foreach (var vehicle in vehicles)
            {
                if (!vehicle)
                {
                    continue;
                }

                var vehiclePrefabPath = AssetDatabase.GetAssetPath(vehicle);
                var vehiclePrefab = PrefabUtility.LoadPrefabContents(vehiclePrefabPath);
                var meshResolvers = vehiclePrefab.GetComponentsInChildren<MeshResolveRef>();

                foreach (var meshResolver in meshResolvers)
                {
                    var mesh = IntegrationExtension.FindSubAsset<Mesh>(meshResolver.MainAssetName, meshResolver.MeshName);

                    if (!mesh)
                    {
                        UnityEngine.Debug.LogError($"{vehiclePrefab.name} Mesh not found {meshResolver.MainAssetName} {meshResolver.MeshName}");
                        continue;
                    }

                    var meshFilter = meshResolver.GetComponent<MeshFilter>();
                    var meshRenderer = meshResolver.GetComponent<MeshRenderer>();

                    meshFilter.sharedMesh = mesh;

                    if (!OverrideMaterial || meshRenderer.sharedMaterial == null)
                    {
                        var material = FindMaterial(meshResolver.MaterialName);

                        if (material != null)
                        {
                            meshRenderer.sharedMaterial = material;
                            EditorSaver.SetObjectDirty(meshRenderer);
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"MeshResolver. Prefab {vehiclePrefab.name} Material {meshResolver.MaterialName} not found");
                        }
                    }

                    DestroyImmediate(meshResolver);
                }

                PrefabUtility.SaveAsPrefabAsset(vehiclePrefab, vehiclePrefabPath);
                PrefabUtility.UnloadPrefabContents(vehiclePrefab);
            }
        }

        private void UnpackOther()
        {
            if (cacheContainer)
            {
                if (!string.IsNullOrEmpty(cacheMaterialName))
                {
                    var mat = FindMaterial(cacheMaterialName);
                    cacheContainer.CustomMaterial = mat;
                }

                foreach (var prefabName in CacheContainerPrefabs)
                {
                    var prefab = FindPrefab(prefabName);

                    if (prefab != null)
                    {
                        cacheContainer.Prefabs.Add(prefab);
                    }
                }

                EditorSaver.SetObjectDirty(cacheContainer);
            }
        }

        private Material FindMaterial(string materialName)
        {
            if (cachedMaterials.ContainsKey(materialName))
            {
                return cachedMaterials[materialName];
            }

            var materialGuids = AssetDatabase.FindAssets($"{materialName} t:Material");

            if (materialGuids == null || materialGuids.Length == 0)
            {
                UnityEngine.Debug.Log($"MeshResolver. Material {materialName} not found");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuids[0]));

            if (material)
            {
                cachedMaterials.Add(materialName, material);
            }

            return material;
        }

        private Texture2D FindTexture(string textureName) => FindAsset<Texture2D>(textureName, "Texture2D");

        private GameObject FindPrefab(string prefabName) => FindAsset<GameObject>(prefabName, "Prefab");

        private T FindAsset<T>(string name, string type) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"{name} t:{type}");

            string assetPath = string.Empty;

            if (guids?.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    var index = path.IndexOf($"{name}.");

                    if (index > 0)
                    {
                        assetPath = path;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }

            UnityEngine.Debug.Log($"Asset searching. Asset {name} Type {type} not found");

            return null;
        }

        private void UnpackPedestrianData(GameObject hub)
        {
            UnpackGPUPedestrian(hub);
            UnpackLegacyPedestrian(hub);
        }

        private void UnpackLegacyPedestrian(GameObject hub)
        {
            var pedestrianSkinFactory = hub.GetComponentInChildren<PedestrianSkinFactory>();

            if (!pedestrianSkinFactory)
            {
                UnityEngine.Debug.LogError($"Parent '{hub.name}' PedestrianCrowdSkinFactory not found");
                return;
            }

            if (LegacySkins.Count == 0)
            {
                return;
            }

            var skinPreset = pedestrianSkinFactory.PedestrianSkinFactoryData;

            if (!skinPreset)
            {
                return;
            }

            var skinCount = pedestrianSkinFactory.SkinCount;

            for (int i = 0; i < skinCount; i++)
            {
                if (LegacySkins.Count <= i)
                {
                    break;
                }

                var skinData = skinPreset.GetEntry(i);
                var savedSkinData = LegacySkins[i];

                if (skinData.Skin)
                {
                    var animator = skinData.Skin.GetComponent<Animator>();

                    if (animator && (!string.IsNullOrEmpty(savedSkinData.AvatarMainAsset) || !string.IsNullOrEmpty(savedSkinData.AvatarName)))
                    {
                        var avatar = IntegrationExtension.FindSubAsset<Avatar>(savedSkinData.AvatarMainAsset, savedSkinData.AvatarName, savedSkinData.AvatarExtension);
                        animator.avatar = avatar;
                    }

                    UnpackSkin(skinData.Skin, savedSkinData);
                }

                if (skinData.Ragdoll && i < Ragdolls.Count)
                {
                    var savedRagdollData = Ragdolls[i];

                    UnpackSkin(skinData.Ragdoll.gameObject, savedRagdollData);
                }
            }
        }

        private void UnpackSkin(GameObject skin, SkinnedCharacterData savedSkinData)
        {
            SkinnedMeshRenderer[] meshes = skin.GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int j = 0; j < meshes.Length; j++)
            {
                SkinnedMeshRenderer skinnedMesh = meshes[j];
                var savedSkinnedMesh = savedSkinData.MeshList.Meshes[j];

                var material = IntegrationExtension.FindSubAsset<Material>(savedSkinnedMesh.MaterialMainAssetName, savedSkinnedMesh.MaterialName, savedSkinnedMesh.MaterialExtension);
                var mesh = IntegrationExtension.FindSubAsset<Mesh>(savedSkinnedMesh.MeshMainAssetName, savedSkinnedMesh.MeshName);

                skinnedMesh.sharedMaterial = material;
                skinnedMesh.sharedMesh = mesh;

                EditorSaver.SetObjectDirty(skinnedMesh);
            }
        }

        private void UnpackGPUPedestrian(GameObject hub)
        {
            var pedestrianCrowdSkinFactory = hub.GetComponentInChildren<PedestrianCrowdSkinFactory>();

            if (!pedestrianCrowdSkinFactory)
            {
                UnityEngine.Debug.LogError($"Parent '{hub.name}' PedestrianCrowdSkinFactory not found");
                return;
            }

            if (SkinDatas.Count == 0)
            {
                return;
            }

            var characterAnimationContainer = pedestrianCrowdSkinFactory.CharacterAnimationContainer;

            for (int i = 0; i < characterAnimationContainer.Count; i++)
            {
                if (SkinDatas.Count <= i)
                {
                    break;
                }

                var skinData = characterAnimationContainer.GetSkinData(i);

                var gpuData = SkinDatas[i];

                var mesh = IntegrationExtension.FindSubAsset<Mesh>(gpuData.MeshMainAssetName, gpuData.MeshName);

                if (mesh)
                {
                    skinData.SetMesh(mesh);
                }

                var texture = FindTexture(gpuData.TextureName);

                if (texture)
                {
                    var material = skinData.GetMaterial();
                    material.SetTexture(AnimationBaker.Constans.MainTexture, texture);
                }
                else
                {
                    UnityEngine.Debug.LogError($"SkinIndex {i} texture {gpuData.TextureName} not found");
                }

                if (skinData.Ragdoll)
                {
                    UnpackSkin(skinData.Ragdoll.gameObject, Ragdolls[i]);
                }
            }

            EditorSaver.SetObjectDirty(characterAnimationContainer);
        }

        private void ClearInternal()
        {
            if (ContentScene)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(ContentScene));
            }

            foreach (var skinData in SkinDatas)
            {
                if (skinData.Material)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(skinData.Material));
                }
            }

            foreach (var material in Materials)
            {
                if (material.Material)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(material.Material));
                }
            }

            foreach (var material in ClonedMaterials)
            {
                if (material)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(material));
                }
            }

            foreach (var prefab in Prefabs)
            {
                if (prefab)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prefab));
                }
            }

            foreach (var preset in Presets)
            {
                if (preset)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(preset));
                }
            }

            foreach (var legacySkin in LegacySkins)
            {
                if (legacySkin.Prefab)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(legacySkin.Prefab));
                }
            }

            foreach (var ragdoll in Ragdolls)
            {
                if (ragdoll.Prefab)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(ragdoll.Prefab));
                }
            }

            if (!string.IsNullOrEmpty(subsceneRelativeFolderPath))
            {
                var sourcePath = AssetDatabase.GetAssetPath(this);
                var folderPath = $"{sourcePath}{subsceneRelativeFolderPath}";

                try
                {
                    AssetDatabase.DeleteAsset(folderPath);
                }
                catch { }
            }

            SkinDatas.Clear();
            Prefabs.Clear();
            Presets.Clear();
            Materials.Clear();
            LegacySkins.Clear();
            Ragdolls.Clear();
            CacheContainerPrefabs.Clear();
        }
    }
}

