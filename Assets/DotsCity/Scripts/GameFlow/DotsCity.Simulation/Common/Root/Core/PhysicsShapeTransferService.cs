using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Authoring;
using Spirit604.DotsCity.Simulation.Level;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    public class PhysicsShapeTransferService : MonoBehaviourBase
    {
        private const float CellHashSize = 0.1f;

        #region Helper types

        public enum ProccesingType { DisableCollider, DisableObject, StayPrevious }
        public enum ColliderType { Legacy, PhysicsShape }
        public enum SubSceneType { RoadScene, ChunkSubscene }
        public enum SearchType { ByTag, ByLayer }

        [Serializable]
        public class PhysicsObject
        {
            public GameObject SceneShape;
            public Collider Collider;
            public ColliderType ColliderType;
            public SubSceneType SubSceneType;
            public int SourceHash;
        }

        [Serializable]
        public class ObjectProcessSettings
        {
            public int Layer;
            public int TagIndex;

            [Tooltip("Enable pre-init cull state for physics objects")]
            public bool PreinitLayer;

            public ProccesingType LegacyColliderPerfType = ProccesingType.DisableObject;

#if UNITY_EDITOR
            public LayerMask LayerMask
            {
                get
                {
                    LayerMask layerMask = new LayerMask();
                    layerMask.value = 1 << Layer;

                    return layerMask;
                }
            }

            public string Tag
            {
                get
                {
                    var tags = UnityEditorInternal.InternalEditorUtility.tags;

                    if (tags.Length > TagIndex)
                    {
                        return tags[TagIndex];
                    }

                    return string.Empty;
                }
            }
#endif
        }

        #endregion

        #region Serialized variables

#pragma warning disable 0414

        [SerializeField] private GameObject customRootSearch;
        [SerializeField] private bool cleanComponents = true;
        [SerializeField] private bool cleanChilds = true;
        [SerializeField] private SearchType searchType;
        [SerializeField][Range(1, 3)] private int layerCount = 1;
        [SerializeField] private ObjectProcessSettings[] layersData = new ObjectProcessSettings[3];
        [SerializeField] private bool newLayer;

        [ShowIf(nameof(newLayer))]
        [Layer]
        [SerializeField] private string newLayerValue;

        [SerializeField] private List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

#pragma warning restore 0414

        #endregion

        #region Properties

        public bool ByTagSearch => searchType == SearchType.ByTag;

        public bool ByLayerSearch => searchType == SearchType.ByLayer;

        public SearchType CurrentSearchType => searchType;

        public ObjectProcessSettings[] LayersData { get => layersData; set => layersData = value; }

        #endregion

#if UNITY_EDITOR

        #region Public methods

        public List<GameObject> GetShapes(bool autoRestore = false)
        {
            if (autoRestore)
            {
                Restore();
            }

            List<GameObject> newShapes = new List<GameObject>();

            for (int i = 0; i < layerCount; i++)
            {
                var layerData = layersData[i];

                Func<Transform, bool> searchPredicate = null;

                switch (searchType)
                {
                    case SearchType.ByTag:
                        {
                            var tag = layerData.Tag;

                            if (!string.IsNullOrEmpty(tag))
                            {
                                searchPredicate = a => a.gameObject.CompareTag(tag);
                            }

                            break;
                        }
                    case SearchType.ByLayer:
                        searchPredicate = a => a.gameObject.layer == layerData.Layer;
                        break;
                }

                if (searchPredicate == null)
                {
                    continue;
                }

                IEnumerable<Transform> sceneShapes = null;

                if (!customRootSearch)
                {
                    sceneShapes = ObjectUtils.FindObjectsOfType<Transform>().Where(searchPredicate);
                }
                else
                {
                    sceneShapes = customRootSearch.GetComponentsInChildren<Transform>().Where(searchPredicate);
                }

                if (sceneShapes == null)
                {
                    continue;
                }

                foreach (var sceneShape in sceneShapes)
                {
                    var collider = sceneShape.GetComponentInChildren<Collider>();
                    var colliderType = collider != null ? ColliderType.Legacy : ColliderType.PhysicsShape;

                    var hash = GetHash(sceneShape.transform);

                    var newShape = Instantiate(sceneShape);
                    newShape.transform.position = sceneShape.transform.position;
                    newShape.transform.rotation = sceneShape.transform.rotation;
                    newShape.transform.localScale = sceneShape.transform.lossyScale;
                    newShape.gameObject.isStatic = true;

                    if (newLayer)
                    {
                        newShape.gameObject.layer = LayerMask.NameToLayer(newLayerValue);
                    }

                    if (cleanComponents)
                    {
                        CleanObject(newShape.gameObject, searchPredicate);
                    }

                    var cullPhysicsEntityAuthoring = newShape.gameObject.AddComponent<CullPhysicsEntityAuthoring>();

                    if (layerData.PreinitLayer)
                    {
                        cullPhysicsEntityAuthoring.PreinitEnabling = true;
                        EditorSaver.SetObjectDirty(cullPhysicsEntityAuthoring);
                    }

                    var mirrorPhysicsObject = newShape.gameObject.AddComponent<MirrorPhysicsObject>();
                    mirrorPhysicsObject.SourceNodeHash = hash;

                    if (colliderType == ColliderType.Legacy)
                    {
                        if (layerData.LegacyColliderPerfType == ProccesingType.DisableObject)
                        {
                            sceneShape.gameObject.SetActive(false);
                        }

                        if (layerData.LegacyColliderPerfType == ProccesingType.DisableCollider)
                        {
                            if (collider)
                            {
                                collider.enabled = false;
                            }
                        }
                    }

                    PhysicsObject physicsObject = new PhysicsObject()
                    {
                        SceneShape = sceneShape.gameObject,
                        SourceHash = hash,
                        Collider = collider,
                        SubSceneType = SubSceneType.RoadScene,
                        ColliderType = colliderType,
                    };

                    physicsObjects.Add(physicsObject);

                    newShapes.Add(newShape.gameObject);
                }
            }

            EditorSaver.SetObjectDirty(this);

            string messageText = string.Empty;

            if (newShapes.Count == 0)
            {
                messageText = "Make sure the physics surfaces have the appropriate layer and in the main scene.";
            }

            UnityEngine.Debug.Log($"PhysicsShapeTransferService. {newShapes.Count} physics shapes have been cloned. {messageText}");

            return newShapes;
        }

        public void Restore()
        {
            var sceneShapes = ObjectUtils.FindObjectsOfType<MirrorPhysicsObject>().ToList();

            foreach (var item in physicsObjects)
            {
                if (item.SceneShape)
                {
                    item.SceneShape.gameObject.SetActive(true);
                }

                if (item.Collider)
                {
                    item.Collider.enabled = true;
                }
            }

            sceneShapes.DestroyGameObjects();

            physicsObjects.Clear();
            EditorSaver.SetObjectDirty(this);
        }

        #endregion

        #region Private methods

        private void CleanObject(GameObject newShape, Func<Transform, bool> searchPredicate)
        {
            var components = newShape.GetComponentsInChildren<Component>().Where(a => !(a is Transform)).ToArray();

            foreach (var item in components)
            {
                if (!ValidType(item))
                {
                    DestroyImmediate(item);
                }
            }

            var renders = newShape.GetComponentsInChildren<Renderer>().ToArray();

            foreach (var render in renders)
            {
                DestroyImmediate(render);
            }

            if (newShape.transform.childCount > 0)
            {
                var disabledObjects = newShape.GetComponentsInChildren<Transform>().Where(a => !a.gameObject.activeSelf).ToArray();

                foreach (var disabledObject in disabledObjects)
                {
                    if (disabledObject)
                    {
                        DestroyImmediate(disabledObject.gameObject);
                    }
                }

                if (cleanChilds)
                {
                    DestroyChild(newShape.transform, searchPredicate);
                }
            }
        }

        private void DestroyChild(Transform transform, Func<Transform, bool> searchPredicate)
        {
            if (!transform || transform.childCount == 0)
            {
                return;
            }

            var childCount = transform.childCount;
            int index = 0;

            while (index < childCount)
            {
                var child = transform.GetChild(index);

                if (searchPredicate(child))
                {
                    DestroyChild(child, searchPredicate);
                    index++;
                }
                else
                {
                    childCount--;
                    child.parent = null;
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private bool ValidType(Component component)
        {
            return component is Rigidbody || component is Collider || component is PhysicsShapeAuthoring || component is PhysicsBodyAuthoring;
        }

        private int GetHash(Transform transform) => HashMapHelper.GetHashMapPosition(transform.position, CellHashSize);

        #endregion
#endif
    }
}