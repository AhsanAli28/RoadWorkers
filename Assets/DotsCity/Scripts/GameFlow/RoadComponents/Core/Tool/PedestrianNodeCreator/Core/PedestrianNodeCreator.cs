using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Pedestrian;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Pedestrian
{
    [ExecuteInEditMode]
    public class PedestrianNodeCreator : MonoBehaviour
    {
        #region Helper types

        public enum SelectionMode { Single, Multiple }
        public enum ShowHandleType { OnlyCreated, OnlySelected, All }
        public enum MultipleHandleType { Single, All }
        public enum ShowPathType { All, OnlyCreated }
        public enum NodeButtonType { Delete, Unselect }
        public enum AutoSplitConnectionType { Disabled, CustomAngle }
        public enum AttachType { Collider, Mesh }
        public enum ShowPathWidthType { Current, Selected }

        #endregion

        #region Serialized variables

        public bool cachedValues;
        public Transform createRoot;
        public PedestrianNode pedestrianNodePrefab;
        public PedestrianNodeCreatorConfig pedestrianNodeCreatorConfig;
        public PedestrianNodeHotkeyConfig pedestrianNodeHotkeyConfig;
        public SceneDataViewerConfig pedestrianNodeDataViewerConfig;

        [Tooltip("Custom user parent for newly created nodes")]
        public Transform customParent;

        [Tooltip("Child path from the creator for newly created nodes")]
        public string createPath = "PedestrianNodes";

        [Tooltip("On/off position handles for nodes")]
        public bool showHandlers = true;

        [Tooltip("" +
            "<b>Only created</b> : only the created nodes will have handles shown\r\n\r\n" +
            "<b>Only selected</b> : only the selected nodes will have handles shown\r\n\r\n" +
            "<b>All</b> : all nodes will have handles shown")]
        public ShowHandleType showHandleType;

        [Tooltip("" +
            "<b>Single</b> : only 1 node is selected\r\n\r\n" +
            "<b>Multiple</b> : multiple nodes can be selected")]
        public SelectionMode selectionMode;

        [Tooltip("" +
            "<b>Single</b> : node has a position handle each individually\r\n\r\n" +
            "<b>All</b> : all nodes have the same position handle")]
        public MultipleHandleType multipleHandleType;

        [Tooltip("The node will be deselected the second time you try to select it")]
        public bool unselectSelected = true;

        [Tooltip("Global width of routes for all nodes")]
        [Range(0.1f, 10f)] public float maxPathWidth = 1f;

        [Tooltip("Currently created node will be connected to the previously created node")]
        public bool connectWithPreviousNode;

        [Tooltip("Node will be selected after it is connected to the source node")]
        public bool autoSelectConnectedNode;

        [Tooltip("On/off feature to connect to the TrafficNode")]
        public bool allowConnectTrafficNode;

        [Tooltip("If a node is located between a connection of existing nodes, the connection will be reconnected between them (made with a Raycast)")]
        public AutoSplitConnectionType autoSplitConnection = PedestrianNodeCreator.AutoSplitConnectionType.CustomAngle;

        [Range(1f, 90f)] public float customSplitAngle = 5f;

        [Tooltip("If there are other nodes on the connection line, they will automatically be connected to each other in one row")]
        public bool autoRejoinLine = true;

        [Tooltip("Auto attach created node to surface")]
        public bool autoAttachToSurface;

        public LayerMask surfaceMask = ~0;

        [Tooltip("" +
            "<b>Collider</b> : attach to collider\r\n\r\n" +
            "<b>Mesh</b> : attach to mesh")]
        public AttachType attachType;

        [Tooltip("Auto snap node position during creation")]
        public bool autoSnapPosition = true;

        [Tooltip("Snapping value")]
        [Range(0f, 20f)] public float snapValue = 0.5f;

        [Tooltip("Show pedestrian node routes")]
        public bool showPath = true;

        [Tooltip("" +
            "<b>All</b> : all the nodes will be shown.\r\n\r\n" +
            "<b>Only</b> created : only the nodes created by the creator will be shown")]
        public ShowPathType showPathType;

        [Tooltip("On/off display custom buttons of selected node")]
        public bool showNodeButtons = true;

        [Tooltip("" +
            "<b>Delete</b> : node will be deleted by clicking\r\n\r\n" +
            "<b>Unselect</b> : node will be unselected by clicking")]
        public NodeButtonType nodeButtonType;

        [Tooltip("Unique information of the node will be displayed (different from the original prefab)")]
        public bool showUniqueInfo;

        public bool showFullDescription;
        public bool showOnlyOnCursor;

        [Tooltip("For nodes with a custom route width, the reset buttons will be displayed")]
        public bool showResetCustomRouteButtons;

        public bool showBorderRoutes;

        [Tooltip("" +
            "<b>Current</b> : route will be displayed with the assigned width of the nodes\r\n\r\n" +
            "<b>Selected</b> : route will be displayed with the selected route width in the creator settings")]
        public ShowPathWidthType showPathWidthType;
        public Color defaultRouteColor = Color.blue;
        public Color customRouteColor = Color.magenta;

        [Tooltip("On/off display the connection to the TrafficNode")]
        public bool showTrafficNodeConnection;
        public Color trafficNodeConnectionColor = Color.magenta;

        [Tooltip("Shows node settings in the inspector.")]
        public bool showSelectedNodeSettings;

        public bool sceneSettings = true;
        public bool selectedNodeSettingsFoldout = true;
        public bool selectionInfo;
        public PedestrianNode selectedNode;
        public List<PedestrianNode> selectedPedestrianNodes = new List<PedestrianNode>();
        public List<PedestrianNode> createdPedestrianNodes = new List<PedestrianNode>();
        public List<PedestrianNode> allPedestrianNodes = new List<PedestrianNode>();
        public List<PedestrianNode> customPathPedestrianNodes = new List<PedestrianNode>();

        #endregion

        #region Variables

        private PedestrianNode tempPedestrianNode;
        private PedestrianNode previousCreatedPedestrianNode;
        private GameObject previousSelectedObject;
        private Vector3 previousCursorPosition;
        private Transform pedestrianNodeRoot;

        #endregion

        #region Properties

#if UNITY_EDITOR
        public SceneObjectDataFilter<PedestrianNode> pedestrianNodeDataFilter { get; private set; }
#endif

        public Transform Root
        {
            get
            {
                if (createRoot)
                {
                    return createRoot;
                }

                return transform;
            }
        }

        public PedestrianNodeCreatorConfig PedestrianNodeCreatorConfig { get => pedestrianNodeCreatorConfig; }

        public bool AutoSelectFromNode => pedestrianNodeCreatorConfig?.AutoSelectFromNode ?? false;

        public bool HasConnectionWindow { get; set; }

        private float SplitAngle
        {
            get
            {
                switch (autoSplitConnection)
                {
                    case AutoSplitConnectionType.CustomAngle:
                        return customSplitAngle;
                }

                return 0;
            }
        }

        #endregion

        #region Events

        public event Action<PedestrianNode> OnSelected = delegate { };
        public event Action OnSelectionModeChangedEvent = delegate { };

        #endregion

        #region Public methods

        public void Create()
        {

#if UNITY_EDITOR
            if (HasConnectionWindow && selectedNode)
            {
                selectedNode.HandleConnection();
                return;
            }
#endif

            Unselect();

            if (tempPedestrianNode == null)
            {
#if UNITY_EDITOR
                tempPedestrianNode = (PrefabUtility.InstantiatePrefab(pedestrianNodePrefab.gameObject, transform) as GameObject).GetComponent<PedestrianNode>();
#endif

                if (autoSnapPosition)
                {
                    var center = VectorExtensions.GetCenterOfSceneView();
                    tempPedestrianNode.transform.position = center;

                    SnapToClosestGridPoint(tempPedestrianNode);
                }
            }
        }

        public void Spawn()
        {
            if (tempPedestrianNode != null)
            {
                PedestrianNode newNode = null;

#if UNITY_EDITOR
                newNode = (PrefabUtility.InstantiatePrefab(pedestrianNodePrefab.gameObject, transform) as GameObject).GetComponent<PedestrianNode>();
                EditorUtility.CopySerialized(tempPedestrianNode, newNode);
                Undo.RegisterCreatedObjectUndo(newNode.gameObject, "Created Pedestrian Node");
#endif

                newNode.transform.position = tempPedestrianNode.transform.position;
                newNode.transform.rotation = tempPedestrianNode.transform.rotation;
                newNode.MaxPathWidth = maxPathWidth;

                Transform currentParent = null;

                if (!customParent)
                {
                    if (!pedestrianNodeRoot)
                    {
                        currentParent = TransformExtensions.GetChild(Root, createPath);
                    }
                    else
                    {
                        currentParent = pedestrianNodeRoot;
                    }
                }
                else
                {
                    currentParent = customParent;
                }

                newNode.transform.SetParent(currentParent);

                if (connectWithPreviousNode && previousCreatedPedestrianNode != null)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(previousCreatedPedestrianNode, "Previous Connected Pedestrian Node Changed");
#endif
                    previousCreatedPedestrianNode.AddNode(newNode);
                    newNode.AddNode(previousCreatedPedestrianNode);
                }

                previousCreatedPedestrianNode = newNode;
                Add(newNode);
                newNode.PedestrianNodeCreator = this;
                EditorSaver.SetObjectDirty(newNode);

                if (SplitAngle > 0)
                {
                    int splitCount = Mathf.RoundToInt(360f / SplitAngle);

                    for (int i = 0; i < splitCount; i++)
                    {
                        float angle = SplitAngle * i;

                        var dir1 = Quaternion.Euler(new Vector3(0, angle)) * Vector3.forward;
                        var dir2 = Quaternion.Euler(new Vector3(0, angle)) * -Vector3.forward;

                        Physics.Raycast(newNode.transform.position, dir1, out var hitInfo1, float.MaxValue, 1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME));
                        Physics.Raycast(newNode.transform.position, dir2, out var hitInfo2, float.MaxValue, 1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME));

                        var splitted = TryToSplitConnection(hitInfo1, hitInfo2, newNode);

                        if (splitted)
                        {
                            break;
                        }
                    }
                }

#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }

            ConnectWithSelected();
        }

        private bool TryToSplitConnection(RaycastHit hitInfo1, RaycastHit hitInfo2, PedestrianNode newNode)
        {
            if (hitInfo1.collider != null && hitInfo2.collider != null)
            {
                var node1 = hitInfo1.collider.GetComponent<PedestrianNode>();
                var node2 = hitInfo2.collider.GetComponent<PedestrianNode>();

                if (node1 && node2)
                {
                    if (node1.HasDoubleConnection(node2) && node2.HasDoubleConnection(node1))
                    {
#if UNITY_EDITOR
                        Undo.RegisterCompleteObjectUndo(node1, "PedestrianNode connection changed");
                        Undo.RegisterCompleteObjectUndo(node2, "PedestrianNode connection changed");
#endif

                        node1.RemoveConnection(node2);
                        node1.AddConnection(newNode);
                        node2.AddConnection(newNode);

                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryToSelectNode(Vector2 mousePosition)
        {
            ClearTempNode();

            var colliders = PedestrianNodeUtils.GetColliders(mousePosition, out var worldPosition);
            bool undoSaved = false;

            switch (selectionMode)
            {
                case SelectionMode.Single:
                    {
                        List<Transform> gos = colliders.Select(a => a.transform).ToList();

                        if (gos?.Count > 0)
                        {
                            var closestObject = VectorExtensions.FindClosestTarget(gos, worldPosition);

                            if (closestObject != null)
                            {
                                var newSelectedNode = closestObject.GetComponent<PedestrianNode>();

                                if (newSelectedNode != null)
                                {
#if UNITY_EDITOR
                                    Undo.RegisterCompleteObjectUndo(this, "Selected Node Changed");
                                    selectedNode = newSelectedNode;
                                    OnSelected(selectedNode);
                                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

#endif

                                    return true;
                                }
                            }
                        }
                        break;
                    }
                case SelectionMode.Multiple:
                    {
                        bool added = false;

                        for (int i = 0; i < colliders?.Length; i++)
                        {
                            if (colliders[i].transform == null)
                            {
                                continue;
                            }

                            PedestrianNode newSelectedNode = colliders[i].transform.GetComponent<PedestrianNode>();

                            if (newSelectedNode == null)
                            {
                                continue;
                            }

                            if (!undoSaved)
                            {
                                undoSaved = true;

#if UNITY_EDITOR
                                Undo.RegisterCompleteObjectUndo(this, "Selected Node Changed");
#endif

                            }

                            if (selectedPedestrianNodes.TryToAdd(newSelectedNode))
                            {
                                added = true;
                            }
                            else if (unselectSelected)
                            {
                                selectedPedestrianNodes.TryToRemove(newSelectedNode);
                            }
                        }

                        if (added)
                        {
                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        public void Add(PedestrianNode pedestrianNode)
        {
            allPedestrianNodes.TryToAdd(pedestrianNode);

            if (!createdPedestrianNodes.Contains(pedestrianNode))
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Pedestrian Node Creator Added Node");
#endif
                createdPedestrianNodes.Add(pedestrianNode);
            }
        }

        public void Remove(PedestrianNode pedestrianNode)
        {
            if (allPedestrianNodes != null)
                allPedestrianNodes.TryToRemove(pedestrianNode);

            if (createdPedestrianNodes != null && createdPedestrianNodes.Contains(pedestrianNode))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && this != null)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Pedestrian Node Creator Removed Node");
                }
#endif
                createdPedestrianNodes.Remove(pedestrianNode);
            }
        }

        public void Unselect()
        {
            ClearTempNode();

            selectedNode = null;
            selectedPedestrianNodes.Clear();
        }

        private void ClearTempNode()
        {
            if (tempPedestrianNode != null)
            {
                DestroyImmediate(tempPedestrianNode.gameObject);
            }
        }

        public void SelectCreator()
        {
#if UNITY_EDITOR
            Selection.activeGameObject = gameObject;
#endif
        }

        public void HandleSelectedNodeButton(PedestrianNode selectedNode)
        {
            if (selectedNode != null)
            {
                switch (nodeButtonType)
                {
                    case NodeButtonType.Delete:
                        {
                            Remove(selectedNode);
#if UNITY_EDITOR
                            Undo.DestroyObjectImmediate(selectedNode.gameObject);
                            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
                            break;
                        }
                    case NodeButtonType.Unselect:
                        {
                            selectedPedestrianNodes.TryToRemove(selectedNode);
                            selectedNode = null;
                            break;
                        }
                }
            }
        }

        public void HandleCursor(Vector2 mousePosition)
        {
            if (tempPedestrianNode == null)
            {
                return;
            }

            Vector3 worldCursorPosition = GetCursorPosition(mousePosition);

            var oldPosition = tempPedestrianNode.transform.position;
            var moveOffset = worldCursorPosition - oldPosition;

            SetPosition(tempPedestrianNode, oldPosition, moveOffset);
        }

        public void ConnectWithSelected()
        {
            if (selectedNode != null)
            {
                var newNode = selectedNode.HandleConnection(allowConnectTrafficNode, autoRejoinLine);

                if (autoSelectConnectedNode && newNode != null)
                {
                    selectedNode = newNode;
                }
            }
        }

        public void SetNodeNewPosition(PedestrianNode pedestrianNode, Vector3 oldPosition, Vector3 offset)
        {
#if UNITY_EDITOR
            Undo.RecordObject(pedestrianNode.transform, "Pedestrian Node Position");
#endif

            SetPosition(pedestrianNode, oldPosition, offset);
        }

        public void MoveMultipleSelectedNodesPosition(Vector3 offset)
        {
            SaveSelectedNodes();

            for (int i = 0; i < selectedPedestrianNodes?.Count; i++)
            {
                Vector3 oldPosition = selectedPedestrianNodes[i].transform.position;

                SetPosition(selectedPedestrianNodes[i], oldPosition, offset);
            }
        }

        private void SaveSelectedNodes()
        {
            if (selectedPedestrianNodes.Count > 0)
            {
#if UNITY_EDITOR
                var selectedTransforms = selectedPedestrianNodes.Select(item => item.transform).Cast<UnityEngine.Object>().ToArray();
                Undo.RecordObjects(selectedTransforms, "Pedestrian Nodes Position Changed");
#endif
            }
        }

        private void SetPosition(PedestrianNode pedestrianNode, Vector3 oldPosition, Vector3 offset)
        {
            Vector3 newPosition = default;

            if (autoSnapPosition)
            {
                newPosition = oldPosition;
                MathUtilMethods.CustomRoundOffsetVectorValue(ref newPosition, offset, snapValue, false);
            }
            else
            {
                newPosition = oldPosition + offset;
            }

            pedestrianNode.transform.position = newPosition;
        }

        private Vector3 GetCursorPosition(Vector2 mousePosition)
        {
            if (autoAttachToSurface)
            {
                var layeMask = surfaceMask & ~(1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME));

#if UNITY_EDITOR
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

                switch (attachType)
                {
                    case AttachType.Collider:
                        {
                            if (Physics.Raycast(ray, out var hit, float.MaxValue, layeMask))
                            {
                                return hit.point;
                            }
                            break;
                        }
                    case AttachType.Mesh:
                        {
                            if (Event.current.type == EventType.MouseMove)
                            {
                                GameObject go = HandleUtility.PickGameObject(mousePosition, false);

                                if (go)
                                {
                                    previousSelectedObject = go;
                                    var inLayer = EditorExtension.IsInLayer(go.layer, layeMask);

                                    if (inLayer)
                                    {
                                        var meshFilter = go.GetComponentInChildren<MeshFilter>();

                                        if (meshFilter)
                                        {
                                            EditorHandles_UnityInternal.IntersectRayMesh(ray, meshFilter, out var hit);

                                            if (hit.point != Vector3.zero)
                                            {
                                                previousCursorPosition = hit.point;
                                                return hit.point;
                                            }
                                        }
                                    }
                                }
                            }

                            return previousCursorPosition;
                        }
                }
#endif
            }

            return mousePosition.GUIScreenToWorldSpace();
        }

        public bool ShowPath(PedestrianNode pedestrianNode)
        {
            if (!showPath)
            {
                return false;
            }

            if (showPathType == ShowPathType.All)
            {
                return true;
            }

            return createdPedestrianNodes.Contains(pedestrianNode);
        }

        private void SnapToClosestGridPoint(PedestrianNode pedestrianNode)
        {
            var position = pedestrianNode.transform.position;
            MathUtilMethods.CustomRoundVectorValue(ref position, snapValue);
            pedestrianNode.transform.position = position;
        }

        public void DrawRoutes()
        {
            if (!showBorderRoutes)
            {
                return;
            }

            for (int i = 0; i < allPedestrianNodes.Count; i++)
            {
                var routeNode = allPedestrianNodes[i];

                if (!routeNode)
                {
                    continue;
                }

                float width = 0;
                Color color = default;

                switch (showPathWidthType)
                {
                    case ShowPathWidthType.Current:
                        {
                            width = routeNode.MaxPathWidth;
                            var customMaxPathWidth = HasCustomPathWidth(routeNode);
                            color = !customMaxPathWidth ? defaultRouteColor : customRouteColor;

#if UNITY_EDITOR
                            routeNode.DrawRoutes(color);
#endif

                            break;
                        }
                    case ShowPathWidthType.Selected:
                        {
                            width = maxPathWidth;
                            color = defaultRouteColor;

#if UNITY_EDITOR
                            routeNode.DrawTempRoutes(color, width, width);
#endif
                            break;
                        }
                }
            }
        }

        public void SaveGlobalRoutesWidth()
        {
            pedestrianNodePrefab.MaxPathWidth = maxPathWidth;
#if UNITY_EDITOR
            PrefabUtility.SavePrefabAsset(pedestrianNodePrefab.gameObject);
#endif
        }

        public void ResetAllCustomPath()
        {
            UpdateCustomPedestrianNodesData();

#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Reset custom node with");
#endif

            for (int i = 0; i < customPathPedestrianNodes?.Count; i++)
            {
                ResetCustomPathNode(customPathPedestrianNodes[i]);
            }

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif

            customPathPedestrianNodes.Clear();
        }

        public void ClearCreatedNodes()
        {
            createdPedestrianNodes.Clear();
            EditorSaver.SetObjectDirty(this);
        }

        public void ClearPartialConnection()
        {
            List<PedestrianNode> tempNodes = new List<PedestrianNode>();
            HashSet<PedestrianNode> recordNodes = new HashSet<PedestrianNode>();

            int count = 0;

            foreach (var node in allPedestrianNodes)
            {
                foreach (var connectedNode in node.AutoConnectedPedestrianNodes)
                {
                    if (connectedNode && !node.HasDoubleConnection(connectedNode))
                    {
                        tempNodes.Add(connectedNode);
                    }
                }

                foreach (var connectedNode in node.DefaultConnectedPedestrianNodes)
                {
                    if (connectedNode && !node.HasDoubleConnection(connectedNode))
                    {
                        tempNodes.Add(connectedNode);
                    }
                }

                while (tempNodes.Count > 0)
                {
#if UNITY_EDITOR
                    if (!recordNodes.Contains(node))
                    {
                        recordNodes.Add(node);
                        Undo.RegisterChildrenOrderUndo(node, "Undo connection");
                    }
#endif

                    node.RemoveConnection(tempNodes[0]);
                    tempNodes.RemoveAt(0);
                    count++;
                }
            }

            if (recordNodes.Count > 1)
            {
#if UNITY_EDITOR
                EditorExtension.CollapseUndoCurrentOperations();
#endif
            }

            Debug.Log($"{count} partial connections have been cleaned.");
        }

        public void ResetCustomPathNode(PedestrianNode nodeToReset, bool recordCreator = false, bool collapseUndoOperation = false)
        {
#if UNITY_EDITOR
            if (recordCreator && customPathPedestrianNodes.Contains(nodeToReset))
            {
                Undo.RegisterCompleteObjectUndo(this, "Reset custom node with");
            }

            Undo.RegisterCompleteObjectUndo(nodeToReset, "Reset custom node with");

            if (collapseUndoOperation)
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            var so = new SerializedObject(nodeToReset);

            SerializedProperty sp = so.FindProperty("maxPathWidth");
            sp.prefabOverride = false;

            so.ApplyModifiedProperties();
#endif

            customPathPedestrianNodes.TryToRemove(nodeToReset);

            EditorSaver.SetObjectDirty(nodeToReset);
        }

        public void SnapSelectedToClosestGridPoint()
        {
            switch (selectionMode)
            {
                case SelectionMode.Single:
                    {
#if UNITY_EDITOR
                        Undo.RegisterCompleteObjectUndo(selectedNode.transform, "Selected Node Changed");
#endif
                        SnapToClosestGridPoint(selectedNode);
                    }
                    break;
                case SelectionMode.Multiple:
                    {
                        SaveSelectedNodes();

                        for (int i = 0; i < selectedPedestrianNodes.Count; i++)
                        {
                            SnapToClosestGridPoint(selectedPedestrianNodes[i]);
                        }
                    }

                    break;
            }
        }

        public void UpdateAllScenePedestrianNodesData()
        {
            allPedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().ToList();

            for (int i = 0; i < allPedestrianNodes.Count; i++)
            {
                if (allPedestrianNodes[i].PedestrianNodeCreator != this)
                {
                    allPedestrianNodes[i].PedestrianNodeCreator = this;
                    EditorSaver.SetObjectDirty(allPedestrianNodes[i]);
                }
            }

            UpdateCustomPedestrianNodesData();
            UpdateFilterData();
        }

        public void UpdateCustomPedestrianNodesData()
        {
            customPathPedestrianNodes = allPedestrianNodes.Where(item => HasCustomPathWidth(item)).ToList();
        }

        public void ClearSelection()
        {
            selectedPedestrianNodes.Clear();

#if UNITY_EDITOR
            var sceneView = SceneView.lastActiveSceneView;
            sceneView?.Repaint();
#endif
        }

        private bool HasCustomPathWidth(PedestrianNode pedestrianNode)
        {
            return pedestrianNode.MaxPathWidth != pedestrianNodePrefab.MaxPathWidth;
        }

        #endregion

        #region Editor events

        public void OnSelectionModeChanged()
        {
            selectedNode = null;
            selectedPedestrianNodes.Clear();
            OnSelectionModeChangedEvent();
        }

        public void OnInspectorEnabled()
        {
            var layer = LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME);

            if (layer == -1)
            {
                Debug.Log($"PedestrianNodeCreator. PedestrianNode '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer is not defined, make sure you have added the '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer in the Spirit604/Package Initialiazation/Layer settings");
            }
            else if (pedestrianNodePrefab.gameObject.layer != layer)
            {
                string layerName = "NaN";

                string tempLayerName = LayerMask.LayerToName(pedestrianNodePrefab.gameObject.layer);

                if (!string.IsNullOrEmpty(tempLayerName))
                {
                    layerName = tempLayerName;
                }

                Debug.Log($"PedestrianNodeCreator. PedestrianNode prefab has '{layerName}' layer, but should have '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer");
            }

            var entityRoadRootTemp = GameObject.Find("EntityRoadRoot");

            if (entityRoadRootTemp != null)
            {
                pedestrianNodeRoot = entityRoadRootTemp.transform.GetChild(1);
            }
#if UNITY_EDITOR

            if (pedestrianNodeDataFilter == null)
            {
                pedestrianNodeDataFilter = new SceneObjectDataFilter<PedestrianNode>(pedestrianNodePrefab, pedestrianNodeDataViewerConfig);
            }

            UpdateAllScenePedestrianNodesData();
#endif
        }

        public void OnInspectorDisabled()
        {
            Unselect();
        }

        public void UpdateFilterData()
        {
#if UNITY_EDITOR
            pedestrianNodeDataFilter.CreateDefaultFilter(allPedestrianNodes);
#endif
        }


#if UNITY_EDITOR
        public void DoFocus()
        {
            if (selectedNode)
            {
                Vector3 focusPosition = selectedNode.transform.position;
                SceneView.lastActiveSceneView.LookAt(focusPosition);
            }
        }
#endif

        public void OnSettingsChanged(PedestrianNode selectedNode)
        {
            if (showUniqueInfo)
            {
#if UNITY_EDITOR
                pedestrianNodeDataFilter.UpdateFilterNode(selectedNode);
#endif
            }
        }

        #endregion

        #region Event handlers

        private void OnDrawGizmosSelected()
        {
            DrawRoutes();
        }

        #endregion
    }
}