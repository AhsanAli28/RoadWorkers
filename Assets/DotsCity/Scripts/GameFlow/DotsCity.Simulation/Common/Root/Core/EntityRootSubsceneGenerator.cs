using Spirit604.Attributes;
using Spirit604.CityEditor.Pedestrian;
using Spirit604.CityEditor.Road;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    public class EntityRootSubsceneGenerator : MonoBehaviour, IVersionProvider
    {
        public enum SearchType { ByTag, ByLayer }

#pragma warning disable 0414

        [SerializeField] private Transform trafficLightRoot;

        [SerializeField] private SubSceneChunkCreator subsceneCreator;

        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [SerializeField] private PedestrianNodeTransferService pedestrianNodeTransferService;

        [SerializeField] private PhysicsShapeTransferService physicsShapeTransferService;

        [SerializeField] private EntityRoadRoot entityRoadRootPrefab;

        [SerializeField] private string entitySubSceneSavePath;

        [SerializeField] private bool moveTools = true;

        [SerializeField] private bool moveLights = true;

        [SerializeField] private bool moveProps = true;

        [SerializeField] private SearchType propsSearchType = SearchType.ByLayer;

        [Layer]
        [SerializeField] private string propsLayer;

        [Tag]
        [SerializeField] private string propsTag;

        [SerializeField] private bool moveSurface;

        [SerializeField] private SearchType searchType = SearchType.ByLayer;

        [Tag]
        [SerializeField] private string groundTag;

        [Layer]
        [SerializeField] private string groundLayer;

        [SerializeField] private bool copyPhysicsShapes;

        [SerializeField] private bool physicsShapesIsCopied;

        [SerializeField] private SubScene entitySubScene;

        [SerializeField] private EntityRoadRoot createdEntityRoadRoot;

        [SerializeField] private string entitySubSceneName = "EntitySubScene";

        [SerializeField] private bool settingsFlag = true;

        [SerializeField] private bool refsFlag = true;

        [SerializeField] private bool configFlag;

        [SerializeField] private string version;

#pragma warning restore 0414

        private ConfigRoot configRootSource;
        private RoadParent roadParent;
        private List<Transform> customPedestianNodes = new List<Transform>();
        private Action onDelayedCallback;
        private Action onOpenedCallback;
        private float delayedTimeStamp;
        private bool saveScene;
        private bool operationInProgress;

        public string Version => version;

        public PhysicsShapeTransferService PhysicsShapeTransferService => physicsShapeTransferService;

        public bool OperationInProgress { get => operationInProgress; set => operationInProgress = value; }

#if UNITY_EDITOR
        public bool EntitySceneCreated => entitySubScene && entitySubScene.SceneAsset;
#else
        public bool EntitySceneCreated => false;
#endif
        public bool CanMoveBack => EntitySceneCreated && !OperationInProgress && createdEntityRoadRoot == null;

        public SearchType PropsSearchType => propsSearchType;

        public SearchType CurrentSearchType => searchType;

        public bool PhysicsShapesIsCopied
        {
            get => physicsShapesIsCopied;
            set
            {
                if (physicsShapesIsCopied != value)
                {
                    physicsShapesIsCopied = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public bool MoveSurfaceAvailable => !CopyPhysicsShapes;

        public bool DOTSSimulation => citySettingsInitializer ? citySettingsInitializer.DOTSSimulation : true;

        public bool MoveLights { get => moveLights && DOTSSimulation; set => moveLights = value; }

        public bool MoveProps { get => moveProps && DOTSSimulation; set => moveProps = value; }

        public bool CopyPhysicsShapes { get => copyPhysicsShapes && DOTSSimulation; set => copyPhysicsShapes = value; }

        private bool SubsceneChunksHasPhysics => subsceneCreator && subsceneCreator.PhysicsShapesIsCopied;

        private bool CanSearchSurface => searchType == SearchType.ByLayer && !string.IsNullOrEmpty(groundLayer) || searchType == SearchType.ByTag && !string.IsNullOrEmpty(groundTag);

        private bool MoveSurface => moveSurface && MoveSurfaceAvailable && CanSearchSurface && DOTSSimulation;

#if UNITY_EDITOR
        private Scene RoadSubscene => entitySubScene.EditingScene;

        private void Start()
        {
            if (entitySubScene == null)
            {
                var subscene = GameObject.Find("EntitySubScene");

                if (subscene == null)
                {
                    var subsceneComp = ObjectUtils.FindObjectsOfType<SubScene>();

                    if (subsceneComp == null)
                    {
                        Debug.Log($"EntityRootSubsceneGenerator. Subscene not generated, make sure you have generated a subscene in the <b>{name}</b> object. Read more about <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/roadInstallation.html#roadentitysubscene\">Hub</a>");
                        EditorGUIUtility.PingObject(gameObject);
                    }
                }
            }

            var roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

            if (roadParent)
            {
                if (!roadParent.gameObject.scene.isSubScene)
                    Debug.Log("Road parent should be on subscene");
            }
        }

        public void Generate()
        {
            if (subsceneCreator == null)
            {
                subsceneCreator = ObjectUtils.FindObjectOfType<SubSceneChunkCreator>();
            }

            if (entitySubScene == null)
            {
                entitySubScene = SubSceneUtils.CreateSubScene(entitySubSceneSavePath, entitySubSceneName, autoCreatePath: true, autoLoadScene: true);
            }

            if (entitySubScene == null)
            {
                return;
            }

            if (!CheckAvailableStatus())
            {
                return;
            }

            var hasDuplicates = !pedestrianNodeTransferService.LoadSceneNodes();

            if (hasDuplicates)
            {
                UnityEngine.Debug.LogError($"PedestrianNodeTransferService has {pedestrianNodeTransferService.DuplicateCount} duplicated pedestrian nodes at the same position. View duplicates in the service.");
                EditorGUIUtility.PingObject(pedestrianNodeTransferService);
                return;
            }

            configRootSource = GetComponentInChildren<ConfigRoot>();
            roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

            customPedestianNodes.Clear();

            pedestrianNodeTransferService.ConvertNodes(false);
            var pedestrianNodes = FindObjects<PedestrianNode>();

            foreach (var pedestrianNode in pedestrianNodes)
            {
                var partOfRoad = TransformExtensions.IsChild(roadParent.gameObject, pedestrianNode.gameObject);

                if (!partOfRoad)
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(pedestrianNode.gameObject);
                    var parent = root?.transform ?? pedestrianNode.transform;

                    customPedestianNodes.TryToAdd(parent);
                }
            }

            configRootSource.SyncConfigs();

            PreProcessGeneration();

            ProcessSubScene(() =>
            {
                PostProcessGeneration();
            }, true);
        }

        public void MoveBack()
        {
            if (!CheckAvailableStatus())
            {
                return;
            }

            ProcessSubScene(() =>
            {
                PostProcessMoveback();
            }, true);
        }

        public void CopyToSubscene()
        {
            CopySceneConfig(true);
        }

        public void CopyFromSubscene()
        {
            CopySceneConfig(false);
        }

        public void RestorePhysics(Action onComplete = null)
        {
            ProcessSubScene(() =>
            {
                physicsShapeTransferService.Restore();
                onComplete?.Invoke();
            }, true);
        }

        private void CopySceneConfig(bool copyToSubscene)
        {
            ProcessSubScene(() =>
            {
                CopyConfig(copyToSubscene);
            });
        }

        private void ProcessSubScene(Action onOpened, bool saveScene = false)
        {
            if (!entitySubScene || !entitySubScene.SceneAsset || OperationInProgress)
            {
                return;
            }

            if (RoadSubscene.isLoaded)
            {
                onOpened?.Invoke();
            }
            else
            {
                LoadScene(onOpened, saveScene);
            }
        }

        private void LoadScene(Action onOpened, bool saveScene = false)
        {
            EditorSceneManager.sceneOpened += EditorSceneManager_SubSceneOpened;

            this.saveScene = saveScene;
            OperationInProgress = true;
            var path = AssetDatabase.GetAssetPath(entitySubScene.SceneAsset);

            onOpenedCallback = onOpened;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private void StartDelayedAction(Action onDelayComplete, float delayDuration = 1f)
        {
            if (delayedTimeStamp == 0)
            {
                EditorApplication.update += EditorApplication_update_DelayedCallback;
            }

            delayedTimeStamp = (float)EditorApplication.timeSinceStartup + delayDuration;
            onDelayedCallback = onDelayComplete;
        }

        private void CopyConfig(bool copyToSubscene)
        {
            var objs = RoadSubscene.GetRootGameObjects();

            var subSceneConfigObj = objs.Where(a => a.GetComponent<ConfigRoot>() != null).FirstOrDefault();

            if (subSceneConfigObj != null)
            {
                var sourceConfig = GetComponentInChildren<ConfigRoot>();

                if (copyToSubscene)
                {
                    PrefabExtension.CopyPrefabOverridenProperties(sourceConfig.gameObject, subSceneConfigObj);
                    CopyConfigInternal(sourceConfig.gameObject, subSceneConfigObj);

                    EditorSceneManager.SaveScene(RoadSubscene);
                }
                else
                {
                    PrefabExtension.CopyPrefabOverridenProperties(subSceneConfigObj, sourceConfig.gameObject);
                    CopyConfigInternal(subSceneConfigObj.gameObject, sourceConfig.gameObject);
                }

                var configs = sourceConfig.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(a => a is ISyncableConfig).ToList();

                if (configs?.Count > 0)
                {
                    var configs2 = subSceneConfigObj.GetComponentsInChildren<MonoBehaviour>().Where(a => a is ISyncableConfig).ToList();

                    if (configs2?.Count > 0)
                        configs.AddRange(configs2);

                    foreach (var item in configs)
                    {
                        var config = item as ISyncableConfig;
                        config.SyncConfig();
                    }
                }

                if (copyToSubscene)
                {
                    UnityEngine.Debug.Log("Config has been copied to subscene");
                }
                else
                {
                    UnityEngine.Debug.Log("Config has been copied from subscene");
                }
            }
        }

        private void CopyConfigInternal(GameObject sourceConfigRoot, GameObject targetConfigRoot)
        {
            var sourcePool = sourceConfigRoot.gameObject.GetComponentInChildren<TrafficEntityPoolBakerRef>();
            var targetPool = targetConfigRoot.gameObject.GetComponentInChildren<TrafficEntityPoolBakerRef>();

            if (sourcePool && targetPool)
            {
                targetPool.PresetData = new TrafficEntityPoolBakerRef.TrafficPresetDictionary();

                foreach (var item in sourcePool.PresetData)
                {
                    targetPool.PresetData.Add(item.Key, item.Value);
                }

                EditorSaver.SetObjectDirty(targetPool);
            }
        }

        private void PreProcessGeneration()
        {
            CheckSurface();
        }

        private void PostProcessGeneration()
        {
            var roots = RoadSubscene.GetRootGameObjects();

            EntityRoadRoot entityRoadRoot = null;

            foreach (var rootObj in roots)
            {
                if (rootObj.GetComponent<EntityRoadRoot>() != null)
                {
                    entityRoadRoot = rootObj.GetComponent<EntityRoadRoot>();
                    break;
                }
            }

            foreach (var rootObj in roots)
            {
                var configRoot = rootObj.GetComponentInChildren<ConfigRoot>();

                if (configRoot != null)
                {
                    DestroyImmediate(configRoot.gameObject);
                    break;
                }
            }

            if (entityRoadRoot == null)
            {
                entityRoadRoot = createdEntityRoadRoot;
            }

            if (entityRoadRoot == null)
            {
                entityRoadRoot = PrefabUtility.InstantiatePrefab(entityRoadRootPrefab) as EntityRoadRoot;
            }

            var pedestrianNodeCreator = FindObject<PedestrianNodeCreator>();
            var roadSegmentPlacer = FindObject<RoadSegmentPlacer>();

            TryToMoveObjectToSubscene(entityRoadRoot.gameObject);

            if (roadParent != null)
            {
                TryToMoveObjectToSubscene(roadParent.gameObject, entityRoadRoot.RoadParentRoot);
                entityRoadRoot.RoadParent = roadParent;
            }

            var configRootPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(configRootSource.gameObject);
            var configCopy = PrefabUtility.InstantiatePrefab(configRootPrefab) as GameObject;

            PrefabExtension.CopyPrefabOverridenProperties(configRootSource.gameObject, configCopy);

            TryToMoveObjectToSubscene(configCopy.gameObject);

            foreach (var customPedestianNode in customPedestianNodes)
            {
                TryToMoveObjectToSubscene(customPedestianNode.gameObject, entityRoadRoot.PedestrianNodesRoot);
            }

            if (moveTools)
            {
                if (pedestrianNodeCreator)
                {
                    TryToMoveObjectToSubscene(pedestrianNodeCreator.gameObject, entityRoadRoot.ToolsRoot);
                }

                if (roadSegmentPlacer)
                {
                    TryToMoveObjectToSubscene(roadSegmentPlacer.gameObject, entityRoadRoot.ToolsRoot);
                }
            }

            var trafficLights = ObjectUtils.FindObjectsOfType<TrafficLightObject>();

            if (MoveLights)
            {
                for (int i = 0; i < trafficLights?.Length; i++)
                {
                    TrafficLightObject trafficLight = trafficLights[i];
                    TryToMoveObjectToSubscene(trafficLight.gameObject, entityRoadRoot.LightsRoot);
                }
            }
            else
            {
                if (!DOTSSimulation)
                {
                    for (int i = 0; i < trafficLights?.Length; i++)
                    {
                        TrafficLightObject trafficLight = trafficLights[i];

                        trafficLight.RebindCrossroad();

                        bool isChild =
                            TransformExtensions.IsChild(entityRoadRoot.gameObject, trafficLight.gameObject) ||
                            trafficLight.TrafficLightCrossroad && TransformExtensions.IsChild(trafficLight.TrafficLightCrossroad.gameObject, trafficLight.gameObject);

                        if (isChild)
                        {
                            TryToMoveObjectToMainScene(trafficLight.gameObject, trafficLightRoot);
                        }
                    }
                }
            }

            if (MoveProps)
            {
                Func<GameObject, bool> searchPropsPredicate = GetSearchPredicate(propsSearchType, propsTag, propsLayer, "Props");

                if (searchPropsPredicate != null)
                {
                    var props = FindObjects(searchPropsPredicate);

                    for (int i = 0; i < props?.Length; i++)
                    {
                        var prop = props[i];
                        TryToMoveObjectToSubscene(prop.gameObject, entityRoadRoot.PropsRoot);
                    }
                }
            }

            if (CopyPhysicsShapes)
            {
                if (!SubsceneChunksHasPhysics)
                {
                    var shapes = physicsShapeTransferService.GetShapes(true);

                    physicsShapesIsCopied = shapes?.Count > 0;

                    if (physicsShapesIsCopied)
                    {
                        foreach (var shape in shapes)
                        {
                            TryToMoveObjectToSubscene(shape.gameObject, entityRoadRoot.SurfaceRoot);
                        }
                    }
                }
                else
                {
                    physicsShapesIsCopied = false;
                    UnityEngine.Debug.Log("SubSceneCreator. Physics shapes already cloned. Physics shape transfer cancelled.");
                }
            }

            if (MoveSurface)
            {
                if (physicsShapesIsCopied)
                {
                    physicsShapesIsCopied = false;
                    physicsShapeTransferService.Restore();
                }

                Func<GameObject, bool> searchPredicate = GetSearchPredicate(searchType, groundTag, groundLayer, "Ground");

                if (searchPredicate != null)
                {
                    var grounds = FindObjects(searchPredicate);

                    for (int i = 0; i < grounds?.Length; i++)
                    {
                        GameObject ground = grounds[i];
                        TryToMoveObjectToSubscene(ground.gameObject, entityRoadRoot.SurfaceRoot);
                    }
                }
            }

            createdEntityRoadRoot = null;
            EditorSaver.SetObjectDirty(entityRoadRoot);
            EditorSaver.SetObjectDirty(this);

            UnityEngine.Debug.Log("Subscene successfully generated");
        }

        private void CheckSurface()
        {
            if (!createdEntityRoadRoot)
                return;

            if (CopyPhysicsShapes)
            {
                if (!SubsceneChunksHasPhysics)
                {
                    RestoreSurface(createdEntityRoadRoot);
                }
            }
        }

        private void RestoreSurface(EntityRoadRoot entityRoadRoot)
        {
            var surfaceRoot = entityRoadRoot.SurfaceRoot;

            if (surfaceRoot.childCount > 0)
            {
                var newSurfaceRoot = new GameObject(surfaceRoot.name);
                EditorSceneManager.MoveGameObjectToScene(newSurfaceRoot, entityRoadRoot.gameObject.scene);

                UnityEngine.Debug.Log($"All subscene physics surfaces are moved to the new '{surfaceRoot.name}' in the main scene due to physics surface cloning.");

                List<Transform> childs = new List<Transform>();

                for (int i = 0; i < surfaceRoot.childCount; i++)
                {
                    childs.Add(surfaceRoot.GetChild(i));
                }

                foreach (var child in childs)
                {
                    child.transform.SetParent(newSurfaceRoot.transform);
                }
            }
        }

        private Func<GameObject, bool> GetSearchPredicate(SearchType searchType, string tag, string layer, string searchObjectName)
        {
            Func<GameObject, bool> searchPredicate = null;

            switch (searchType)
            {
                case SearchType.ByTag:
                    {
                        if (!string.IsNullOrEmpty(tag))
                        {
                            searchPredicate = a => a.gameObject.CompareTag(tag);
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"{searchObjectName} tag is empty");
                        }

                        break;
                    }
                case SearchType.ByLayer:
                    {
                        if (!string.IsNullOrEmpty(layer))
                        {
                            if (layer != "Default")
                            {
                                var layerIndex = LayerMask.NameToLayer(layer);
                                searchPredicate = a => a.gameObject.layer == layerIndex;
                            }
                            else
                            {
                                UnityEngine.Debug.Log($"{searchObjectName} is using default layer");
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"{searchObjectName} layer is empty");
                        }

                        break;
                    }
            }

            return searchPredicate;
        }

        private void PostProcessMoveback()
        {
            if (RoadSubscene == null)
                return;

            var roots = RoadSubscene.GetRootGameObjects();

            foreach (var rootObj in roots)
            {
                var entityRoadRoot = rootObj.GetComponentInChildren<EntityRoadRoot>();

                if (entityRoadRoot != null)
                {
                    TryToMoveObjectToMainScene(entityRoadRoot.gameObject);
                    this.createdEntityRoadRoot = entityRoadRoot;
                    EditorSaver.SetObjectDirty(this);
                    RestoreTrafficLights();
                    break;
                }
            }

            pedestrianNodeTransferService.RestoreScene();
            physicsShapeTransferService.Restore();

            if (this.createdEntityRoadRoot)
            {
                EditorGUIUtility.PingObject(this.createdEntityRoadRoot);
            }
        }

        private void TryToMoveObjectToSubscene(GameObject targetObj, Transform parent = null)
        {
            TryToMoveObjectToScene(targetObj, RoadSubscene, parent);
        }

        private void TryToMoveObjectToMainScene(GameObject targetObj, Transform parent = null)
        {
            TryToMoveObjectToScene(targetObj, this.gameObject.scene, parent);
        }

        private void TryToMoveObjectToScene(GameObject targetObj, Scene targetScene, Transform parent = null)
        {
            if (!targetObj)
                return;

            GameObject root = null;

            if (targetObj.scene != targetScene)
            {
                root = GetRoot(targetObj);

                root.transform.parent = null;

                try
                {
                    EditorSceneManager.MoveGameObjectToScene(root, targetScene);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }
            }

            if (parent != null)
            {
                root = GetRoot(targetObj);

                if (root)
                {
                    root.transform.SetParent(parent);
                }
                else
                {
                    var root2 = PrefabUtility.GetNearestPrefabInstanceRoot(targetObj);
                    var IsAnyPrefabInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(targetObj);
                    UnityEngine.Debug.LogError($"{targetObj.name} {root2?.name ?? "NULL name"} IsPartOfAnyPrefab {PrefabUtility.IsPartOfAnyPrefab(targetObj)} IsAnyPrefabInstanceRoot {IsAnyPrefabInstanceRoot} root is null");
                }
            }
        }

        private GameObject GetRoot(GameObject targetObj)
        {
            GameObject root;

            if (PrefabUtility.IsAnyPrefabInstanceRoot(targetObj) || !PrefabUtility.IsPartOfAnyPrefab(targetObj))
            {
                root = targetObj;
            }
            else
            {
                root = PrefabUtility.GetNearestPrefabInstanceRoot(targetObj);
            }

            return root;
        }

        private T[] FindObjects<T>(Func<GameObject, bool> customPredicate = null) where T : Component
        {
            Func<GameObject, bool> predicate = GetPredicate(customPredicate);

            return ObjectUtils.FindObjectsOfType<T>().Where(a => predicate(a.gameObject)).ToArray();
        }

        private Func<GameObject, bool> GetPredicate(Func<GameObject, bool> customPredicate = null)
        {
            Func<GameObject, bool> predicate = (a) =>
            {
                return a.gameObject.scene == this.gameObject.scene;
            };

            Func<GameObject, bool> predicate1 = null;
            Func<GameObject, bool> predicate2 = null;

            if (customPredicate != null)
            {
                predicate1 = a => predicate(a) && customPredicate(a);
            }
            else
            {
                predicate1 = predicate;
            }

            if (createdEntityRoadRoot != null)
            {
                predicate2 = a => predicate1(a) && !TransformExtensions.IsChild(createdEntityRoadRoot.gameObject, a.gameObject);
            }
            else
            {
                predicate2 = predicate1;
            }

            return predicate2;
        }

        private GameObject[] FindObjects(Func<GameObject, bool> customPredicate = null)
        {
            Func<GameObject, bool> predicate = GetPredicate(customPredicate);

            return ObjectUtils.FindObjectsOfType<GameObject>().Where(a => predicate(a.gameObject)).ToArray();
        }

        private T FindObject<T>() where T : Component
        {
            Func<GameObject, bool> predicate = GetPredicate();

            return ObjectUtils.FindObjectsOfType<T>().Where(a => predicate(a.gameObject)).FirstOrDefault();
        }

        private bool CheckAvailableStatus()
        {
            if (!pedestrianNodeTransferService)
            {
                UnityEngine.Debug.LogError("Assign 'PedestrianNodeTransferService' field");
                return false;
            }

            if (!physicsShapeTransferService)
            {
                UnityEngine.Debug.LogError("Assign 'PhysicsShapeTransferService' field");
                return false;
            }

            return true;
        }

        private void RestoreTrafficLights()
        {
            if (!trafficLightRoot)
                return;

            var lights = trafficLightRoot.GetComponentsInChildren<TrafficLightObject>();

            if (lights.Length == 0)
                return;

            Dictionary<int, TrafficLightCrossroad> cachedCrossroads = new Dictionary<int, TrafficLightCrossroad>();

            var sceneCrossRoads = ObjectUtils.FindObjectsOfType<TrafficLightCrossroad>();

            for (int i = 0; i < sceneCrossRoads.Length; i++)
            {
                cachedCrossroads.Add(sceneCrossRoads[i].UniqueId, sceneCrossRoads[i]);
            }

            for (int i = 0; i < lights.Length; i++)
            {
                var trafficLight = lights[i];

                if (!cachedCrossroads.TryGetValue(trafficLight.ConnectedId, out var crossroad))
                    continue;

                trafficLight.AssignCrossRoad(crossroad);
            }
        }

        public void OnInspectorEnabled()
        {
            var newEntitySubSceneSavePath = SubSceneUtils.GetSubfolderProjectPathOfActiveScene();

            if (entitySubSceneSavePath != newEntitySubSceneSavePath)
            {
                entitySubSceneSavePath = newEntitySubSceneSavePath;
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void EditorSceneManager_SubSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!scene.isLoaded)
            {
                StartDelayedAction(() =>
                {
                    var path = AssetDatabase.GetAssetPath(entitySubScene.SceneAsset);
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }, 0.2f);

                return;
            }

            EditorSceneManager.sceneOpened -= EditorSceneManager_SubSceneOpened;
            onOpenedCallback?.Invoke();
            onOpenedCallback = null;

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.CloseScene(scene, true);
            OperationInProgress = false;
        }

        private void EditorApplication_update_DelayedCallback()
        {
            if (EditorApplication.timeSinceStartup > delayedTimeStamp)
            {
                delayedTimeStamp = 0;
                EditorApplication.update -= EditorApplication_update_DelayedCallback;
                onDelayedCallback?.Invoke();
                onDelayedCallback = null;
            }
        }
#endif
    }
}
