using Spirit604.CityEditor.Utils;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class RoadSegmentPlacer : MonoBehaviour
    {
        private const string RelativeAssetPath = "Prefabs/CityEditor/Roads/";

        public enum SnapType { Custom }

#pragma warning disable 0414

        [SerializeField] private bool prefabFoldout;
        [SerializeField] private RoadParent roadNodeParent;
        [SerializeField] private RoadSegmentPlacerConfig roadCreatorConfig;
        [SerializeField] private RoadSegmentCreatorConfig roadSegmentCreatorConfig;

        [SerializeField] private string assetPath;

        [SerializeField] private bool sceneFoldout = true;
        [SerializeField] private bool showMovementHandlers = true;
        [SerializeField] private bool showRotationHandlers = true;
        [SerializeField] private bool showDeleteButtons = true;
        [SerializeField] private bool showLightInfo;

        [SerializeField] private bool onlyShortNames = true;
        [SerializeField] private bool roadSnapPosition = true;
        [SerializeField] private SnapType snapType = SnapType.Custom;

        [SerializeField][Range(0, 10)] private float customSnapSize = 5f;
        [SerializeField] private bool evenSizeSnapPosition = true;

        [SerializeField] private List<RoadSegment> createdSegments = new List<RoadSegment>();
        [SerializeField] private List<RoadSegment> roadSegmentPrefabs;

        [HideInInspector] public string[] headers;
        [HideInInspector] public int prefabIndex;

#pragma warning restore 0414

        public RoadSegment LastSelected { get; set; }

        public bool PrefabFoldout { get => prefabFoldout; set => prefabFoldout = value; }

        public bool SceneFoldout { get => sceneFoldout; set => sceneFoldout = value; }

        public List<RoadSegment> RoadSegmentPrefabs
        {
            get
            {
                if (ShouldToLoadAssets)
                {
                    LoadAssets();
                }

                return roadSegmentPrefabs;
            }
        }

        public List<RoadSegment> CreatedSegments { get => createdSegments; }
        public bool RoadSnapPosition { get => roadSnapPosition; }
        public bool ShowMovementHandlers { get => showMovementHandlers; }
        public bool ShowRotationHandlers { get => showRotationHandlers; }
        public bool ShowDeleteButtons { get => showDeleteButtons; }
        public RoadSegmentPlacerConfig RoadCreatorConfig { get => roadCreatorConfig; }
        public bool ShowLightInfo { get => showLightInfo; }
        public bool EvenSizeSnapPosition { get => evenSizeSnapPosition; set => evenSizeSnapPosition = value; }

        public bool ShouldToLoadAssets
        {
            get
            {
                return (roadSegmentPrefabs == null || roadSegmentPrefabs.Count == 0);
            }
        }

        public SnapType CurrentSnapType { get => snapType; set => snapType = value; }
        public float CustomSnapSize { get => customSnapSize; set => customSnapSize = value; }

        public void AddRoadSegment()
        {
            GameObject roadGo = null;

#if UNITY_EDITOR
            roadGo = PrefabUtility.InstantiatePrefab(roadSegmentPrefabs[prefabIndex].gameObject, roadNodeParent.transform) as GameObject;
            Undo.RegisterCreatedObjectUndo(roadGo, "Created selected segment");
#endif

            roadGo.transform.position = GetSpawnposition();
            var road = roadGo.GetComponent<RoadSegment>();

            AddRoadSegment(road);

            EditorSaver.SetObjectDirty(this);

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public void AddRoadSegmentCreator()
        {
            var spawnPosition = GetSpawnposition();

            var creator = Instantiate(roadCreatorConfig.RoadSegmentCreatorPrefab, spawnPosition, Quaternion.identity, roadNodeParent.transform);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(creator.gameObject, "Created new segment");
#endif

            creator.Create();
            RoadSegment roadSegment = creator.GetComponent<RoadSegment>();
            AddRoadSegment(roadSegment);

#if UNITY_EDITOR
            Selection.activeObject = creator;
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif

        }

        public void AddRoadSegment(RoadSegment roadSegment)
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Add segment");
#endif

            roadSegment.Initialize(this);

            createdSegments.TryToAdd(roadSegment);
        }

        public void RemoveSegment(RoadSegment roadSegment)
        {
            if (!Application.isPlaying && this != null)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Remove segment");
#endif

                createdSegments.TryToRemove(roadSegment);
            }
        }

        public void DestroySegment(RoadSegment roadSegment)
        {
            RemoveSegment(roadSegment);

#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(roadSegment.gameObject);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public void LoadAssets()
        {
#if UNITY_EDITOR
            var assets = AssetDatabaseExtension.TryGetUnityObjectsOfTypeFromPath<RoadSegment>(assetPath);

            roadSegmentPrefabs = assets;
            headers = null;

            if (assets != null)
            {
                if (onlyShortNames)
                {
                    headers = assets.Select(asset => asset.ShortTitleName).ToArray();
                }
                else
                {
                    headers = assets.Select(asset => (asset.gameObject).name).ToArray();
                }
            }
#endif
        }

        public void Connect()
        {
            roadNodeParent.ConnectSegments();
        }

        public void AddSceneSegments()
        {
            createdSegments = ObjectUtils.FindObjectsOfType<RoadSegment>().ToList();
            EditorSaver.SetObjectDirty(this);
        }

        public void SnapObject(Transform objectToSnap, SnapType snapType, bool evenSizeSnapping, float customSnapSize)
        {
            Vector2Int size = GetMapSize(evenSizeSnapping);

            switch (snapType)
            {
                case SnapType.Custom:
                    PositionHelper.RoundObjectPositionToTile(objectToSnap, size, customSnapSize);
                    break;
            }
        }

        public void RotateSegment(RoadSegment roadSegment, Vector3 rotation)
        {
            LastSelected = roadSegment;
            roadSegment.transform.Rotate(rotation);
        }

        public bool SetNewPath(string path)
        {
            if (!string.IsNullOrEmpty(path) && assetPath != path)
            {
                assetPath = path;
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public void OnInspectorEnabled()
        {
            if (roadNodeParent == null)
            {
                roadNodeParent = ObjectUtils.FindObjectOfType<RoadParent>();

                if (roadNodeParent != null)
                {
                    EditorSaver.SetObjectDirty(this);
                }
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = CityEditorBookmarks.PREFAB_ROOT_PATH + RelativeAssetPath;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public KeyCode GetRotateKey()
        {
            if (roadSegmentCreatorConfig)
            {
                return roadSegmentCreatorConfig.GetKey("RotateRoad", KeyCode.CapsLock);
            }

            return KeyCode.CapsLock;
        }

        public void Focus()
        {
#if UNITY_EDITOR
            if (LastSelected)
            {
                Vector3 focusPosition = LastSelected.transform.position;
                SceneView.lastActiveSceneView.LookAt(focusPosition);
            }
            else
            {
                if (createdSegments.Count > 0)
                {
                    Vector3 worldPosition = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(InputExtension.GetMousePosition());

                    var segment = createdSegments.OrderBy((d) => (worldPosition - transform.position).sqrMagnitude).FirstOrDefault();

                    if (segment)
                    {
                        LastSelected = segment;
                        Focus();
                    }
                }
            }
#endif
        }

        private Vector2Int GetMapSize(bool evenSizeSnapping)
        {
            return evenSizeSnapping ? Vector2Int.one * 2 : Vector2Int.one;
        }

        private Vector3 GetSpawnposition()
        {
            return VectorExtensions.GetCenterOfSceneView();
        }
    }
}
