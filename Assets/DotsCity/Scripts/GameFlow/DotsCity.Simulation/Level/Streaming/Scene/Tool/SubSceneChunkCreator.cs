using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public class SubSceneChunkCreator : MonoBehaviour
    {
        #region Helper types

        public const string AssigmentsGroup = "Assigments";
        public const string ChunkSettingsGroup = "Chunk Settings";
        public const string PostProcessGroup = "Post Process Settings";
        public const string ChunkDataGroup = "Chunks Data";
        public const string ButtonsGroup = "Buttons";

        private const string DefaultRelativePath = "Scenes/Chunks/";

        private enum PositionSourceType { ObjectPosition, MeshCenter }

        [System.Serializable]
        private class SceneChunkData
        {
            public SubScene SubScene;
            public string ScenePath;
            public Vector3 MinPosition = new Vector3(float.MaxValue, 0, float.MaxValue);
            public Vector3 MaxPosition = new Vector3(float.MinValue, 0, float.MinValue);
            public List<ChunkObjectData> ChunkObjects = new List<ChunkObjectData>();

            public Vector3 ChunkPosition => (MinPosition + MaxPosition) / 2;
        }

        [System.Serializable]
        private class SceneChunkDataDictionary : AbstractSerializableDictionary<int, SceneChunkData> { }

        [System.Serializable]
        private class ChunkObjectData
        {
            public GameObject SourceObject;
            public List<MeshRenderer> RelatedMeshRenders = new List<MeshRenderer>();
        }

        private enum ObjectFindMethod { ByTag, ByLayer }

        private enum DisableObjectType { MeshRenderer, Parent, ParentIfNoMesh }

        private enum PostProcessType { DeleteComponent, DeleteObject }

        [System.Serializable]
        private class PostProcessData
        {
            public string ComponentTypeName;
            public PostProcessType PostProcessType;
        }

        #endregion

        #region Serializable variables

#pragma warning disable 0414

        [HideInInspector]
        [SerializeField] private EntityRootSubsceneGenerator entityRootSubsceneGenerator;
        [SerializeField] private Transform customParent;
        [SerializeField] private string sceneName = "Scene";
        [SerializeField] private string createPath;
        [SerializeField][Range(0, 1000f)] private float chunkSize = 100f;
        [SerializeField] private PositionSourceType positionSourceType;
        [SerializeField] private bool destroyPreviousCreated = true;
        [SerializeField] private ObjectFindMethod objectFindMethod;
        [SerializeField] private string targetTag;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private bool disableOldSourceObjects = true;
        [SerializeField] private DisableObjectType disableSourceObjectType = DisableObjectType.MeshRenderer;
        [SerializeField] private bool assignNewLayer;
        [SerializeField] private LayerMask newObjectLayer = 0;
        [SerializeField] private bool copyPhysicsShapes;
        [HideInInspector][SerializeField] private bool physicsShapesIsCopied;
        [SerializeField] private bool postProcessNewObject;
        [SerializeField] private List<PostProcessData> postProcessDatas = new List<PostProcessData>();
        [SerializeField] private SceneChunkDataDictionary chunkData = new SceneChunkDataDictionary();
        [SerializeField] private bool assigmentsFlag = true;
        [SerializeField] private bool chunkSettingsFlag = true;
        [SerializeField] private bool postProcessFlag = true;
        [SerializeField] private bool chunkFlag = false;
        [SerializeField] private bool buttonsFlag = false;

#pragma warning restore 0414

        #endregion

        #region Properties

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

        public bool FindTargetByTag => objectFindMethod == ObjectFindMethod.ByTag;

        public PhysicsShapeTransferService PhysicsShapeTransferService => entityRootSubsceneGenerator?.PhysicsShapeTransferService ?? null;

        private bool CopyPhysicsShapes => copyPhysicsShapes && entityRootSubsceneGenerator && entityRootSubsceneGenerator.PhysicsShapeTransferService;

        #endregion

        #region Public methods

        public void Create()
        {
#if UNITY_EDITOR
            if (!CreationIsAvailable())
            {
                return;
            }

            ClearInternal();
            SwitchSceneObjectEnabledState(true);
            chunkData.Clear();
            chunkData.showContent = false;

            List<GameObject> childObjects = TryToFindObjects();

            for (int i = 0; i < childObjects?.Count; i++)
            {
                var sourceObject = childObjects[i].gameObject;

                var meshRenders = sourceObject.GetComponentsInChildren<MeshRenderer>().ToList();

                var chunkObjectData = new ChunkObjectData()
                {
                    SourceObject = sourceObject,
                    RelatedMeshRenders = meshRenders
                };

                Vector3 position = GetObjectPosition(chunkObjectData.SourceObject);
                int chunkIndex = GetOrAddChunk(position);

                chunkData[chunkIndex].ChunkObjects.Add(chunkObjectData);
            }

            Dictionary<int, List<GameObject>> physicsShapesDictionary = null;

            if (CopyPhysicsShapes)
            {
                physicsShapesDictionary = new Dictionary<int, List<GameObject>>();
                var physicsShapes = entityRootSubsceneGenerator.PhysicsShapeTransferService.GetShapes(true);

                foreach (var physicsShape in physicsShapes)
                {
                    int chunkIndex = GetOrAddChunk(physicsShape.transform.position);

                    if (!physicsShapesDictionary.ContainsKey(chunkIndex))
                    {
                        physicsShapesDictionary.Add(chunkIndex, new List<GameObject>());
                    }

                    physicsShapesDictionary[chunkIndex].Add(physicsShape);
                    physicsShapesIsCopied = true;
                }
            }

            int sceneIndex = 1;

            foreach (var item in chunkData)
            {
                var chunckObjects = item.Value.ChunkObjects;

                var duplicateObjectList = new List<GameObject>();

                for (int i = 0; i < chunckObjects.Count; i++)
                {
                    var sourceObject = chunckObjects[i].SourceObject;
                    var dulplicateObject = Instantiate(sourceObject, sourceObject.transform.position, sourceObject.transform.rotation);
                    dulplicateObject.transform.parent = null;
                    duplicateObjectList.Add(dulplicateObject);

                    PostProcessNewObject(dulplicateObject);

                    if (disableOldSourceObjects)
                    {
                        SwitchSceneObjectEnabledState(chunckObjects[i], disableSourceObjectType, false);
                    }
                }

                if (physicsShapesDictionary != null && physicsShapesDictionary.ContainsKey(item.Key))
                {
                    duplicateObjectList.AddRange(physicsShapesDictionary[item.Key]);
                }

                var currentSceneName = sceneName + sceneIndex.ToString();
                var chunkPosition = item.Value.ChunkPosition;

                var subScene = SubSceneUtils.CreateSubScene(createPath, currentSceneName, chunkPosition, duplicateObjectList, customParent);
                item.Value.SubScene = subScene;
                item.Value.ScenePath = item.Value.SubScene.EditableScenePath;
                sceneIndex++;
            }

            EditorSaver.SetObjectDirty(this);
#endif
        }

        public void SwitchSceneObjects(bool isEnable)
        {
            SwitchSceneObjectEnabledState(isEnable);
        }

        public void SwitchSubSceneObjects(bool isEnable)
        {
            SwitchSubsceneEnabledState(isEnable);
        }

        public void ResetSavePath()
        {
            InitSavePath(true);
        }

        public void ClearButton()
        {
            ClearInternal();
            SwitchSceneObjectEnabledState(true);
            chunkData.Clear();
            EditorSaver.SetObjectDirty(this);
        }

        #endregion

        #region Private methods

        private int GetOrAddChunk(Vector3 position)
        {
            var chunkIndex = HashMapHelper.GetHashMapPosition(position, chunkSize);

            if (!chunkData.ContainsKey(chunkIndex))
            {
                Vector3 chunkPosition = new Vector3(Mathf.FloorToInt(position.x / chunkSize), 0, Mathf.FloorToInt(position.z / chunkSize)) * chunkSize;

                var chunkDat = new SceneChunkData()
                {
                };

                chunkData.Add(chunkIndex, chunkDat);
            }

            chunkData[chunkIndex].MinPosition = new Vector3(Mathf.Min(chunkData[chunkIndex].MinPosition.x, position.x), 0, Mathf.Min(chunkData[chunkIndex].MinPosition.z, position.z));
            chunkData[chunkIndex].MaxPosition = new Vector3(Mathf.Max(chunkData[chunkIndex].MaxPosition.x, position.x), 0, Mathf.Max(chunkData[chunkIndex].MaxPosition.z, position.z));
            return chunkIndex;
        }

        private void PostProcessNewObject(GameObject dulplicateObject)
        {
            if (postProcessNewObject)
            {
                var components = dulplicateObject.GetComponentsInChildren<MonoBehaviour>();

                foreach (var postProcessData in postProcessDatas)
                {
                    if (string.IsNullOrEmpty(postProcessData.ComponentTypeName.Trim()))
                    {
                        continue;
                    }

                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            var obj = component.GetComponent(postProcessData.ComponentTypeName);

                            if (obj != null)
                            {
                                switch (postProcessData.PostProcessType)
                                {
                                    case PostProcessType.DeleteComponent:
                                        DestroyImmediate(obj);
                                        break;
                                    case PostProcessType.DeleteObject:
                                        DestroyImmediate(obj.gameObject);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (assignNewLayer)
            {
                dulplicateObject.gameObject.layer = (int)Mathf.Log(newObjectLayer.value, 2);
            }
        }

        private Vector3 GetObjectPosition(GameObject sourceObject)
        {
            if (positionSourceType == PositionSourceType.ObjectPosition)
            {
                return sourceObject.transform.position.Flat();
            }
            else
            {
                var meshRenderers = sourceObject.GetComponentsInChildren<MeshRenderer>();

                if (meshRenderers?.Length > 0)
                {
                    var bounds = meshRenderers[0].bounds;

                    foreach (var meshRenderer in meshRenderers)
                    {
                        bounds.Encapsulate(meshRenderer.bounds);
                    }

                    return bounds.center.Flat();
                }
                else
                {
                    return sourceObject.transform.position.Flat();
                }
            }
        }

        private void ClearInternal()
        {
#if UNITY_EDITOR
            if (destroyPreviousCreated)
            {
                foreach (var item in chunkData)
                {
                    if (item.Value.SubScene != null)
                    {
                        DestroyImmediate(item.Value.SubScene.gameObject);
                    }

                    try
                    {
                        AssetDatabase.DeleteAsset(item.Value.ScenePath);
                    }
                    catch
                    {
                        UnityEngine.Debug.Log($"SceneAsset not found '{item.Value.ScenePath}'");
                    }

                }
            }

            if (physicsShapesIsCopied && PhysicsShapeTransferService)
            {
                PhysicsShapeTransferService.Restore();
                physicsShapesIsCopied = false;
            }
#endif
        }

        private List<GameObject> TryToFindObjects()
        {
            switch (objectFindMethod)
            {
                case ObjectFindMethod.ByTag:
                    return GameObject.FindGameObjectsWithTag(targetTag).ToList();
                case ObjectFindMethod.ByLayer:
                    return ObjectUtils.FindObjectsOfType<GameObject>().Where(a => targetLayer == (targetLayer | (1 << a.layer))).ToList();
            }

            return null;
        }

        private void SwitchSceneObjectEnabledState(bool isActive)
        {
            foreach (var chunk in chunkData)
            {
                foreach (var chunkObject in chunk.Value.ChunkObjects)
                {
                    if (chunkObject != null && chunkObject.SourceObject != null)
                    {
                        SwitchSceneObjectEnabledState(chunkObject, disableSourceObjectType, isActive);
                    }
                }
            }
        }

        private void SwitchSceneObjectEnabledState(ChunkObjectData chunkObjectData, DisableObjectType disableObjectType, bool isActive)
        {
            switch (disableObjectType)
            {
                case DisableObjectType.MeshRenderer:
                    {
                        for (int i = 0; i < chunkObjectData.RelatedMeshRenders?.Count; i++)
                        {
                            var meshRenderer = chunkObjectData.RelatedMeshRenders[i];

                            if (meshRenderer != null)
                            {
                                meshRenderer.enabled = isActive;
                            }
                        }

                        break;
                    }
                case DisableObjectType.Parent:
                    {
                        chunkObjectData.SourceObject.SetActive(isActive);
                        break;
                    }
                case DisableObjectType.ParentIfNoMesh:
                    {
                        if (chunkObjectData.RelatedMeshRenders == null || chunkObjectData.RelatedMeshRenders.Count == 0)
                        {
                            SwitchSceneObjectEnabledState(chunkObjectData, DisableObjectType.Parent, isActive);
                        }
                        else
                        {
                            SwitchSceneObjectEnabledState(chunkObjectData, DisableObjectType.MeshRenderer, isActive);
                        }

                        break;
                    }
            }
        }

        private void SwitchSubsceneEnabledState(bool isActive)
        {
            foreach (var chunk in chunkData)
            {
                if (chunk.Value.SubScene != null)
                {
                    chunk.Value.SubScene.gameObject.SetActive(isActive);
                }
            }
        }

        private void InitSavePath(bool force = false)
        {
            if (string.IsNullOrEmpty(createPath) || force)
            {
                createPath = CityEditorBookmarks.GetPath(DefaultRelativePath);
                EditorSaver.SetObjectDirty(this);
            }
        }

        private bool CreationIsAvailable()
        {
            if (copyPhysicsShapes)
            {
                if (!entityRootSubsceneGenerator)
                {
                    entityRootSubsceneGenerator = ObjectUtils.FindObjectOfType<EntityRootSubsceneGenerator>();
                }

                if (entityRootSubsceneGenerator && entityRootSubsceneGenerator.PhysicsShapesIsCopied)
                {
#if UNITY_EDITOR
                    if (EditorUtility.DisplayDialog("Warning", "Road SubScene already have physics shapes. Replace or cancel the transfer of shapes", "Replace", "Not transfer"))
                    {
                        entityRootSubsceneGenerator.RestorePhysics(() =>
                        {
                            entityRootSubsceneGenerator.PhysicsShapesIsCopied = false;
                            Create();
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.Log("SubsceneCreator. CopyPhysicsShapes is disabled");
                        copyPhysicsShapes = false;
                        return true;
                    }
#endif

                    return false;
                }
            }

            return true;
        }

        private void Reset()
        {
            InitSavePath();
        }

        #endregion

        #region Editor events

#if UNITY_EDITOR

        public void OnInspectorEnabled()
        {
            if (entityRootSubsceneGenerator == null)
            {
                entityRootSubsceneGenerator = ObjectUtils.FindObjectOfType<EntityRootSubsceneGenerator>();
                EditorSaver.SetObjectDirty(this);
            }
        }

#endif

        #endregion
    }
}