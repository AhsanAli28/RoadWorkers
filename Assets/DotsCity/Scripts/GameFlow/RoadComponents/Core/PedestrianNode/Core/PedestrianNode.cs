using Spirit604.CityEditor.Pedestrian;
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Pedestrian;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public class PedestrianNode : MonoBehaviour, IBakeRoad
    {
        #region Helper types & constans

        [Serializable]
        public class CustomConnectionData
        {
            public int SubNodeCount;
            public bool Oneway;
        }

        [Serializable]
        public class ConnectionDataDictionary : AbstractSerializableDictionary<int, CustomConnectionData> { }

        private struct ConnectionDirectionSettings
        {
            public Vector3 LeftHorizontal;
            public Vector3 RightHorizontal;
            public Vector3 LeftVertical;
            public Vector3 RightVertical;
        }

        private const float GIZMOS_ARROW_LENGTH = 1.5f;
        private const float GIZMOS_ARROW_LINE_THICK = 0.8f;
        private const float CONNECTION_RAY_LENGTH = 500f;

        #endregion

        #region Serialized variables

        public PedestrianNodeHotkeyConfig HotKeyConfig;

        [Tooltip("Connected traffic node (for instance, parking)")]
        [SerializeField] private TrafficNode connectedTrafficNode;

        [Tooltip("Connected traffic light")]
        [SerializeField] private TrafficLightHandler relatedTrafficLightHandler;

        [Tooltip("Connected other nodes along raycasts by RoadParent")]
        [SerializeField] private List<PedestrianNode> autoConnectedPedestrianNodes = new List<PedestrianNode>();

        [Tooltip("Press Tab button over another node to connect by keyboard")]
        [SerializeField] private List<PedestrianNode> defaultConnectedPedestrianNodes = new List<PedestrianNode>();
        [SerializeField] private ConnectionDataDictionary connectionDataContainer;

        [SerializeField] private bool leftHorizontal;
        [SerializeField] private bool rightHorizontal;
        [SerializeField] private bool leftVertical;
        [SerializeField] private bool rightVertical;

        [Tooltip("" +
            "<b>Sit</b> : node for benches, seats\r\n\r\n" +
            "<b>House</b> : node for entry/exit to the house\r\n\r\n" +
            "<b>Idle</b> : node for temporary idling pedestrians\r\n\r\n" +
            "<b>Car parking</b> : node to enter/exit a parked car\r\n\r\n" +
            "<b>Talk area</b> : node for crowd conversations of pedestrians\r\n\r\n" +
            "<b>Traffic public stop station</b> : node for waiting for public transport\r\n\r\n" +
            "<b>Traffic public entry</b> : node for entering public transport\r\n\r\n" +
            "<b>Trigger</b> : node to notify the user that the entity has reached the node. TriggerNodeConfig should be activated\r\n\r\n" +
            "<b>Trigger And Destroy</b> : node to notify the user that the entity has reached the node & then destroy the entity. TriggerNodeConfig should be activated")]
        [SerializeField] private PedestrianNodeType pedestrianNodeType;
        [SerializeField] private PedestrianNodeType previousPedestrianNodeType;

        [Tooltip("Shape of the area for randomization of pedestrian targets")]
        [SerializeField] private NodeShapeType pedestrianNodeShapeType;

        [Tooltip("Can spawn pedestrian in view of camera")]
        [SerializeField] private bool canSpawnInView;

        [Tooltip("-1 value is unlimited; Capacity for objects like benchs, houses etc...")]
        [SerializeField][Range(-1, 100)] private int capacity = -1;

        [Tooltip("Weight for choosing random node by pedestrian")]
        [SerializeField][Range(0f, 1f)] private float priorityWeight = 1f;

        [Tooltip("If 0 then default value is taken")]
        [SerializeField][Range(0f, 10f)] private float customAchieveDistance;

        [Tooltip("Chance to spawn pedestrian at node: 0 = 0%, 1 = 100%")]
        [SerializeField][Range(0f, 1f)] private float chanceToSpawn = 1f;

        [Tooltip("Maximum width of the route around the node")]
        [SerializeField][Range(0.1f, 10f)] private float maxPathWidth = 1f;

        [Tooltip("Maximum height size of the node area (rectangle shape only)")]
        [SerializeField][Range(0.1f, 10f)] private float height = 1f;

        [Tooltip("Are supposed to randomize the position around a node?")]
        [SerializeField] private bool hasMovementRandomOffset = true;

        [SerializeField] private int uniqueId;

        [HideInInspector]
        public PedestrianNodeCreator PedestrianNodeCreator;

        #endregion

        #region Area variable settings 

        [HideInInspector]
        public PedestrianNodeAreaSettings CurrentPedestrianNodeAreaSettings = new PedestrianNodeAreaSettings();

        #endregion

        #region Variables
        #endregion

        #region Properties

        public TrafficNode ConnectedTrafficNode { get => connectedTrafficNode; set => connectedTrafficNode = value; }

        public IList<PedestrianNode> AutoConnectedPedestrianNodes => autoConnectedPedestrianNodes.AsReadOnly();

        public IList<PedestrianNode> DefaultConnectedPedestrianNodes => defaultConnectedPedestrianNodes.AsReadOnly();

        public PedestrianNodeType PedestrianNodeType { get => pedestrianNodeType; set => pedestrianNodeType = value; }

        public NodeShapeType PedestrianNodeShapeType { get => pedestrianNodeShapeType; set => pedestrianNodeShapeType = value; }

        public bool CanSpawnInView { get => canSpawnInView; set => canSpawnInView = value; }

        public int Capacity
        {
            get
            {
                if (pedestrianNodeType == PedestrianNodeType.CarParking)
                {
                    return 0;
                }

                if (CustomCapacity)
                {
                    return capacity;
                }

                return -1;
            }
            set
            {
                if (CustomCapacity)
                {
                    capacity = value;
                }
            }
        }

        public float PriorityWeight { get => priorityWeight; set => priorityWeight = value; }

        public float CustomAchieveDistance { get => customAchieveDistance; set => customAchieveDistance = value; }

        public float ChanceToSpawn { get => chanceToSpawn; set => chanceToSpawn = value; }

        public float MaxPathWidth { get => maxPathWidth; set => maxPathWidth = value; }

        public float Height { get => pedestrianNodeShapeType == NodeShapeType.Rectangle ? height : maxPathWidth; set => height = value; }

        public TrafficLightHandler RelatedTrafficLightHandler { get => relatedTrafficLightHandler; set => relatedTrafficLightHandler = value; }

        public bool HasMovementRandomOffset { get => hasMovementRandomOffset; set => hasMovementRandomOffset = value; }

        public bool LockConnection { get; set; }

        public bool PredefinedCapacity => pedestrianNodeType == PedestrianNodeType.CarParking;

        public bool CustomCapacity => pedestrianNodeType != PedestrianNodeType.Default && !PredefinedCapacity;

        public int UniqueID => uniqueId;

#if UNITY_EDITOR
        private bool CreatorSelected => PedestrianNodeCreator != null && UnityEditor.Selection.activeGameObject == PedestrianNodeCreator.gameObject;
        public bool CachedRect { get; set; }
        public float CachedWidth { get; set; }
        public float CachedHeight { get; set; }
        public Vector3 CachedPosition { get; set; }
        public Vector3 CachedRotation { get; set; }
        public Vector3 CachedP1 { get; set; }
        public Vector3 CachedP2 { get; set; }
        public Vector3 CachedP3 { get; set; }
        public Vector3 CachedP4 { get; set; }

#endif

        #endregion

        #region Events

        public Action<PedestrianNode> OnHandleConnection = delegate { };

        #endregion

        #region Unity lifecycle

        private void OnDestroy()
        {
            PedestrianNodeCreator?.Remove(this);

            for (int i = 0; i < autoConnectedPedestrianNodes?.Count; i++)
            {
                autoConnectedPedestrianNodes[i]?.TryToRemoveNode(this);
            }
            for (int i = 0; i < defaultConnectedPedestrianNodes?.Count; i++)
            {
                defaultConnectedPedestrianNodes[i]?.TryToRemoveNode(this);
            }
        }

        #endregion

        #region IBakeRoad interface

        public void Bake()
        {
            foreach (var connectedNode in autoConnectedPedestrianNodes)
            {
                if (connectedNode == null || !connectedNode.gameObject.activeSelf)
                {
                    continue;
                }

                CheckConnection(connectedNode);
            }

            foreach (var connectedNode in defaultConnectedPedestrianNodes)
            {
                if (connectedNode == null || !connectedNode.gameObject.activeSelf)
                {
                    continue;
                }

                CheckConnection(connectedNode);
            }

            GenerateId();
        }

        #endregion

        #region Public Methods

        public bool AddNode(PedestrianNode newNode)
        {
            if (newNode != this && !ConnectedTo(newNode))
            {
                defaultConnectedPedestrianNodes.Add(newNode);
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public bool AddAutoNode(PedestrianNode newNode)
        {
            if (newNode != this && !ConnectedTo(newNode))
            {
                autoConnectedPedestrianNodes.Add(newNode);
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public bool TryToRemoveNode(PedestrianNode newNode)
        {
            if (autoConnectedPedestrianNodes.TryToRemove(newNode))
            {
                TryToRemoveConnectionData(newNode);
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            if (defaultConnectedPedestrianNodes.TryToRemove(newNode))
            {
                TryToRemoveConnectionData(newNode);
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public bool AddAutoConnection(PedestrianNode newNode)
        {
            bool cond1 = AddAutoNode(newNode);
            bool cond2 = newNode.AddAutoNode(this);

            return cond1 && cond2;
        }

        public bool AddConnection(PedestrianNode newNode)
        {
            bool cond1 = AddNode(newNode);
            bool cond2 = newNode.AddNode(this);

            return cond1 && cond2;
        }

        public bool RemoveConnection(PedestrianNode newNode)
        {
            bool cond1 = TryToRemoveNode(newNode);
            bool cond2 = newNode.TryToRemoveNode(this);

            return cond1 && cond2;
        }

        public void AttachToTrafficNode()
        {
            SaveUndoInternal();
            float overlapRadius = 0.5f;
            var colliders = Physics.OverlapSphere(transform.position, overlapRadius, 1 << LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME));

            for (int i = 0; i < colliders?.Length; i++)
            {
                if (colliders[i] != null)
                {
                    TrafficNode trafficNode = colliders[i].transform.GetComponent<TrafficNode>();

                    if (trafficNode != null)
                    {
                        connectedTrafficNode = trafficNode;
                        break;
                    }
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        public void ConnectButton()
        {
            SaveUndoInternal();
            autoConnectedPedestrianNodes.Clear();

            var connectionSettings = GetConnectionDirectionSettings();

            Connect(connectionSettings.LeftHorizontal);
            Connect(connectionSettings.RightHorizontal);
            Connect(connectionSettings.LeftVertical);
            Connect(connectionSettings.RightVertical);

            EditorSaver.SetObjectDirty(this);
        }

        public void ConnectDirections(List<PedestrianNode> list, bool autoConnect = false)
        {
            var connectionSettings = GetConnectionDirectionSettings();

            Connect(connectionSettings.LeftHorizontal, false, autoConnect, list);
            Connect(connectionSettings.RightHorizontal, false, autoConnect, list);
            Connect(connectionSettings.LeftVertical, false, autoConnect, list);
            Connect(connectionSettings.RightVertical, false, autoConnect, list);
        }

        public void Connect(Vector3 direction, bool editorConnection = true, bool autoConnect = true, List<PedestrianNode> list = null)
        {
            if (direction == Vector3.zero)
                return;

            Vector3 origin = transform.position;

            float radius = GetComponent<CapsuleCollider>().radius;
            float maxDistance = GetComponent<CapsuleCollider>().radius + 0.1f;

            LayerMask raycastMask = 1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME);

            if (editorConnection)
            {
                RaycastHit[] sphereHits = Physics.SphereCastAll(origin, radius, Vector3.up, maxDistance, raycastMask);

                for (int i = 0; i < sphereHits.Length; i++)
                {
                    if (sphereHits[i].collider != null)
                    {
                        var pedestrianNode = sphereHits[i].transform.GetComponent<PedestrianNode>();

                        if (ShouldAddNode(pedestrianNode))
                        {
                            AddAutoNode(pedestrianNode);
                        }
                    }
                }
            }

            RaycastHit raycastHit;

            Physics.Raycast(origin, direction, out raycastHit, CONNECTION_RAY_LENGTH, raycastMask);

            if (raycastHit.collider != null)
            {
                var pedestrianNode = raycastHit.transform.GetComponent<PedestrianNode>();

                if (ShouldAddNode(pedestrianNode))
                {
                    if (autoConnect)
                        AddAutoNode(pedestrianNode);

                    if (list != null)
                    {
                        list.TryToAdd(pedestrianNode);
                    }
                }
            }

            bool ShouldAddNode(PedestrianNode node)
            {
                return (node != null && node != this && !this.ConnectedTo(node));
            }
        }

        public bool HasDoubleConnection(PedestrianNode connectedPedestrianNode)
        {
            return ConnectedTo(connectedPedestrianNode) && connectedPedestrianNode.ConnectedTo(this);
        }

        public bool ConnectedTo(PedestrianNode connectedPedestrianNode)
        {
            return defaultConnectedPedestrianNodes.Contains(connectedPedestrianNode) || autoConnectedPedestrianNodes.Contains(connectedPedestrianNode);
        }

        public bool PartialConnectedTo(PedestrianNode connectedPedestrianNode)
        {
            return !connectedPedestrianNode.HasDoubleConnection(this) && !IsOneWayConnection(connectedPedestrianNode);
        }

        public bool CanConnectBeOneway(PedestrianNode connectedNode)
        {
            return this.ConnectedTo(connectedNode) && !connectedNode.ConnectedTo(this);
        }

        public bool IsOneWayConnection(PedestrianNode connectedNode)
        {
            var oneWay = CanConnectBeOneway(connectedNode);

            if (oneWay)
            {
                var data = TryToGetConnectionData(connectedNode);

                if (data != null)
                {
                    oneWay = data.Oneway;
                }
                else
                {
                    oneWay = false;
                }
            }

            return oneWay;
        }

        public bool HasConnection(PedestrianNode connectedPedestrianNode)
        {
            return !(connectedTrafficNode != null &&
                connectedPedestrianNode != null &&
                connectedPedestrianNode.gameObject != null &&
                connectedPedestrianNode.connectedTrafficNode != null &&
                connectedTrafficNode == connectedPedestrianNode.connectedTrafficNode &&
                connectedTrafficNode.HasCrosswalk == false
                ) && gameObject.activeInHierarchy && connectedPedestrianNode.gameObject.activeInHierarchy;
        }

        public PedestrianNode HandleConnection(bool allowConnectTrafficNode = true, bool autoRejoinLine = false, Scene customRaycastScene = default)
        {
            var newNode = PedestrianNodeUtils.TryToFindConnectedObjects(this, allowConnectTrafficNode, customRaycastScene);

            if (newNode == null)
                return null;

            OnHandleConnection(newNode);

            if (!LockConnection)
            {
                SaveUndoInternal();

#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(newNode, "Pedestrian Node Connection Changed");
                UnityEditor.Undo.CollapseUndoOperations(UnityEditor.Undo.GetCurrentGroup());
#endif

                if (!autoRejoinLine || newNode.TryToGetMergeNode())
                {
                    ConnectOrDisconnect(newNode);
                }
                else
                {
                    var origin = this.transform.position;
                    var direction = (newNode.transform.position - origin).normalized;

                    float raycastDistance = Vector3.Distance(origin, newNode.transform.position);

                    RaycastHit[] colliders = null;

                    if (!customRaycastScene.isLoaded)
                    {
                        colliders = Physics.RaycastAll(origin, direction, raycastDistance, (1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME)));
                    }
                    else
                    {
                        if (customRaycastScene.GetPhysicsScene().Raycast(origin, direction, out var hit, raycastDistance, (1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME))))
                        {
                            colliders = new RaycastHit[] { hit };
                        }
                    }

                    List<PedestrianNode> newLineNodes = new List<PedestrianNode>();

                    for (int i = 0; i < colliders?.Length; i++)
                    {
                        var lineNode = colliders[i].collider.GetComponent<PedestrianNode>();

                        if (lineNode != null && lineNode != newNode && lineNode != this)
                        {
                            newLineNodes.TryToAdd(lineNode);
                        }
                    }

                    if (newLineNodes.Count > 0)
                    {
#if UNITY_EDITOR
                        for (int i = 0; i < newLineNodes.Count; i++)
                        {
                            PedestrianNode newLineNode = newLineNodes[i];
                            UnityEditor.Undo.RegisterCompleteObjectUndo(newLineNode, "Pedestrian Node Connection Changed");
                        }

                        UnityEditor.Undo.CollapseUndoOperations(UnityEditor.Undo.GetCurrentGroup());
#endif

                        if (newLineNodes.Count > 1)
                        {
                            newLineNodes = VectorExtensions.SortTargetsByDistance(newLineNodes, this.transform.position).ToList();
                        }

                        newLineNodes.Insert(0, this);
                        newLineNodes.Add(newNode);

                        for (int i = 0; i < newLineNodes.Count; i++)
                        {
                            for (int j = 0; j < newLineNodes.Count; j++)
                            {
                                PedestrianNode newLineNode1 = newLineNodes[i];
                                PedestrianNode newLineNode2 = newLineNodes[j];

                                if (newLineNode1 != newLineNode2)
                                {
                                    newLineNode1.RemoveConnection(newLineNode2);
                                }
                            }
                        }

                        for (int i = 0; i < newLineNodes.Count - 1; i++)
                        {
                            var firstNode = newLineNodes[i];
                            var nextNode = newLineNodes[i + 1];

                            firstNode.AddConnection(nextNode);
                        }
                    }
                    else
                    {
                        ConnectOrDisconnect(newNode);
                    }
                }
            }

            return newNode;
        }

        public bool CheckConnection(PedestrianNode connectedNode)
        {
            if (this.PartialConnectedTo(connectedNode))
            {
                UnityEngine.Debug.Log($"PedestrianNode Instance {this.GetInstanceID()} partially connected with node Instance {connectedNode.GetInstanceID()}{TrafficObjectFinderMessage.GetMessage()}");
                return false;
            }

            return true;
        }

        public void ClearAllSubscriptions()
        {
            foreach (Delegate d in OnHandleConnection?.GetInvocationList())
            {
                OnHandleConnection -= (Action<PedestrianNode>)d;
            }
        }

        public void ChangeNodeType()
        {
            ChangeNodeType(pedestrianNodeType);
        }

        public void ChangeNodeType(PedestrianNodeType newPedestrianNodeType)
        {
            if (previousPedestrianNodeType == newPedestrianNodeType)
                return;

            RemoveComponent(previousPedestrianNodeType);
            AddComponent(newPedestrianNodeType);

            previousPedestrianNodeType = newPedestrianNodeType;

            EditorSaver.SetObjectDirty(this);
        }

        public void BakeConnections(float subNodeDistance)
        {
            bool changed = false;

            for (int i = 0; i < autoConnectedPedestrianNodes.Count; i++)
            {
                var connectedNode = autoConnectedPedestrianNodes[i];
                var changedLocal = CalcSubNodes(subNodeDistance, connectedNode);

                if (!changed)
                {
                    changed = changedLocal;
                }
            }

            for (int i = 0; i < defaultConnectedPedestrianNodes.Count; i++)
            {
                var connectedNode = defaultConnectedPedestrianNodes[i];

                var changedLocal = CalcSubNodes(subNodeDistance, connectedNode);

                if (!changed)
                {
                    changed = changedLocal;
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public CustomConnectionData TryToGetConnectionData(PedestrianNode pedestrianNode)
        {
            if (connectionDataContainer != null && pedestrianNode && connectionDataContainer.TryGetValue(pedestrianNode.UniqueID, out var connectionData))
                return connectionData;

            return null;
        }

        public CustomConnectionData TryToGetConnectionData(int index, bool defaultConnection = true)
        {
            var node = TryToGetConnectedNode(index, defaultConnection);
            return TryToGetConnectionData(node);
        }

        public bool TryToRemoveConnectionData(PedestrianNode pedestrianNode)
        {
            bool changed = false;

            if (connectionDataContainer != null && pedestrianNode && connectionDataContainer.ContainsKey(pedestrianNode.UniqueID))
            {
                connectionDataContainer.Remove(pedestrianNode.UniqueID);
                changed = true;
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }

            return changed;
        }

        public bool TryToRemoveConnectionData(int index, bool defaultConnection = true)
        {
            var node = TryToGetConnectedNode(index, defaultConnection);
            return TryToRemoveConnectionData(node);
        }

        public CustomConnectionData AddConnectionData(int index, bool defaultConnection = true)
        {
            var connectedNode = TryToGetConnectedNode(index, defaultConnection);
            return AddConnectionData(connectedNode);
        }

        public CustomConnectionData AddConnectionData(PedestrianNode pedestrianNode)
        {
            if (!pedestrianNode) return null;

            CustomConnectionData data = null;

            var changed = false;

            if (connectionDataContainer == null)
            {
                connectionDataContainer = new ConnectionDataDictionary();
                changed = true;
            }

            if (!connectionDataContainer.ContainsKey(pedestrianNode.UniqueID))
            {
                data = new CustomConnectionData();
                connectionDataContainer.Add(pedestrianNode.UniqueID, data);
                changed = true;
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }

            return data;
        }

        public PedestrianNode TryToGetConnectedNode(int index, bool defaultConnection)
        {
            var list = defaultConnection ? defaultConnectedPedestrianNodes : autoConnectedPedestrianNodes;

            if (list.Count > index)
            {
                return list[index];
            }

            return null;
        }

        public void ClearCustomConnectionData()
        {
            if (connectionDataContainer != null)
            {
                connectionDataContainer = null;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public TrafficNodeCrosswalk GetCrosswalk()
        {
            if (connectedTrafficNode && connectedTrafficNode.TrafficNodeCrosswalk)
                return connectedTrafficNode.TrafficNodeCrosswalk;

            return null;
        }

        public void GenerateId(bool force = false)
        {
            if (uniqueId == 0 || force)
            {
                uniqueId = UniqueIdUtils.GetUniqueID(this, transform.position);
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void IterateConnectedNodes(Action<PedestrianNode> callback)
        {
            for (int i = 0; i < autoConnectedPedestrianNodes.Count; i++)
            {
                if (autoConnectedPedestrianNodes[i])
                    callback(autoConnectedPedestrianNodes[i]);
            }

            for (int i = 0; i < defaultConnectedPedestrianNodes.Count; i++)
            {
                if (defaultConnectedPedestrianNodes[i])
                    callback(defaultConnectedPedestrianNodes[i]);
            }
        }

        public int ClearAutoConnection()
        {
            int count = autoConnectedPedestrianNodes.Count;
            autoConnectedPedestrianNodes.Clear();
            return count;
        }

        public bool HasAnyPartial()
        {
            bool partial = false;

            IterateConnectedNodes((connectedNode) =>
            {
                if (partial) return;

                if (this.PartialConnectedTo(connectedNode))
                {
                    partial = true;
                }
            });

            return partial;
        }

        public int ReconnectPartial(bool recordUndo = false)
        {
            int count = 0;

            IterateConnectedNodes((connectedNode) =>
            {
                if (this.PartialConnectedTo(connectedNode))
                {
                    if (recordUndo)
                    {
                        connectedNode.SaveUndoState("Undo Partial");
                    }

                    this.AddConnection(connectedNode);
                    count++;
                }
            });

            return count;
        }

        public int DisconnectPartial(bool recordUndo = false)
        {
            int count = 0;

            bool recorded = false;

            IterateConnectedNodes((connectedNode) =>
            {
                if (this.PartialConnectedTo(connectedNode))
                {
                    if (recordUndo && !recorded)
                    {
                        recorded = true;
                        this.SaveUndoState("Undo Partial");
                    }

                    this.RemoveConnection(connectedNode);
                    count++;
                }
            });

            return count;
        }

        public PedestrianNode TryToGetMergeNode()
        {
            if (ConnectedTrafficNode != null && ConnectedTrafficNode.NodeIsMerged)
            {
                var index = ConnectedTrafficNode.GetCrosswalkNodeIndex(this);

                if (index != -1)
                {
                    index = ++index % 2;

                    var mergeNode = ConnectedTrafficNode.MergeNode.GetCrosswalkNode(index);

                    if (mergeNode && mergeNode.gameObject.activeSelf)
                        return mergeNode;
                }
            }

            return null;
        }

        public Vector3 GetSubNodePosition(Vector3 connectedPosition, int index, int count)
        {
            float t = (float)(index + 1) / (count + 1);
            return Vector3.Lerp(transform.position, connectedPosition, t);
        }

        #endregion

        #region Private Methods

        private void ConnectOrDisconnect(PedestrianNode newNode)
        {
            var currentNode = this;
            var currentMergeNode = currentNode.TryToGetMergeNode();
            var targetMergeNode = newNode.TryToGetMergeNode();

            if (targetMergeNode)
            {
                var crosswalk = this.connectedTrafficNode;
                var targetCrosswalk = newNode.connectedTrafficNode;

                if (crosswalk && targetCrosswalk)
                {
                    var sourceCrossroad = crosswalk.TrafficLightCrossroad;
                    var targetCrossroad = targetCrosswalk.TrafficLightCrossroad;

                    if (sourceCrossroad != targetCrossroad)
                    {
                        if (!currentMergeNode)
                        {
                            newNode = targetMergeNode;
                        }
                        else
                        {
                            var sourceMergeCrossroad = currentMergeNode.connectedTrafficNode.TrafficLightCrossroad;

                            if (sourceMergeCrossroad == targetCrossroad)
                            {
                                currentNode = currentMergeNode;
                            }
                            else
                            {
                                currentNode = currentMergeNode;
                                newNode = targetMergeNode;
                            }
                        }
                    }
                }
            }
            else
            {
                if (currentMergeNode)
                {
                    var crosswalk = currentMergeNode.connectedTrafficNode;
                    var targetCrossroad = newNode.GetComponentInParent<TrafficLightCrossroad>();

                    if (crosswalk && targetCrossroad)
                    {
                        var sourceCrossroad = crosswalk.TrafficLightCrossroad;

                        if (sourceCrossroad == targetCrossroad)
                        {
                            currentNode = currentMergeNode;
                        }
                    }
                }
            }

            bool added = currentNode.AddNode(newNode);

            if (added)
            {
                newNode.AddNode(currentNode);
            }
            else
            {
                currentNode.TryToRemoveNode(newNode);
                newNode.TryToRemoveNode(currentNode);
            }
        }

        private void SaveUndoInternal()
        {
            SaveUndoState("Pedestrian Node Connection Changed");
        }

        private ConnectionDirectionSettings GetConnectionDirectionSettings()
        {
            ConnectionDirectionSettings connectionSettings = new ConnectionDirectionSettings()
            {
                LeftHorizontal = leftHorizontal ? transform.rotation * new Vector3(-1, 0, 0) : Vector3.zero,
                RightHorizontal = rightHorizontal ? transform.rotation * new Vector3(1, 0, 0) : Vector3.zero,
                LeftVertical = leftVertical ? transform.rotation * new Vector3(0, 0, -1) : Vector3.zero,
                RightVertical = rightVertical ? transform.rotation * new Vector3(0, 0, 1) : Vector3.zero,
            };

            return connectionSettings;
        }

        private bool CalcSubNodes(float subNodeDistance, PedestrianNode connectedNode)
        {
            bool changed = false;

            var crosswalk1 = GetCrosswalk();
            var crosswalk2 = connectedNode.GetCrosswalk();

            if (crosswalk1 == crosswalk2 && crosswalk1 != null)
                return changed;

            var distance = Vector3.Distance(transform.position, connectedNode.transform.position);
            var subNodeCount = 0;

            if (subNodeDistance > 0)
                subNodeCount = Mathf.FloorToInt(distance / subNodeDistance);

            var data = TryToGetConnectionData(connectedNode);

            if (data == null)
            {
                if (subNodeCount > 0)
                {
                    data = AddConnectionData(connectedNode);
                    data.SubNodeCount = subNodeCount;
                    changed = true;
                }
            }
            else
            {
                data.SubNodeCount = subNodeCount;
                changed = true;
            }

            return changed;
        }

        private void RemoveComponent(PedestrianNodeType pedestrianNodeType)
        {
            var componentType = GetComponentType(pedestrianNodeType);
            RemoveComponent(componentType);
        }

        private void AddComponent(PedestrianNodeType pedestrianNodeType)
        {
            var componentType = GetComponentType(pedestrianNodeType);
            AddComponent(componentType);
        }

        private Type GetComponentType(PedestrianNodeType pedestrianNodeType)
        {
            switch (pedestrianNodeType)
            {
                case PedestrianNodeType.Sit:
                    return typeof(PedestrianNodeSeatSettings);
                case PedestrianNodeType.Idle:
                    return typeof(PedestrianNodeIdleSettings);
                case PedestrianNodeType.TrafficPublicStopStation:
                    return typeof(PedestrianNodeStopStationSettings);
            }

            return default;
        }

        private void RemoveComponent(Type componentType)
        {
            if (componentType == null || componentType.Equals(default))
                return;

            try
            {
                var component = gameObject.GetComponent(componentType);

                if (component)
                {
                    DestroyImmediate(component);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        private void AddComponent(Type componentType)
        {
            if (componentType == null || componentType.Equals(default))
                return;

            var component = gameObject.GetComponent(componentType);

            if (!component)
            {
                try
                {
                    gameObject.AddComponent(componentType);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        #endregion

        #region Editor methods

        public void SaveUndoState(string text)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, text);
#endif
        }

#if UNITY_EDITOR

        private CityEditor.CityEditorSettings EditorSettings => CityEditor.CityEditorSettings.GetOrCreateSettings();

        public void DrawRoutes()
        {
            DrawRoutes(Color.blue);
        }

        public void DrawRoutes(Color color)
        {
            for (int i = 0; i < autoConnectedPedestrianNodes?.Count; i++)
            {
                if (autoConnectedPedestrianNodes[i] == null)
                {
                    autoConnectedPedestrianNodes.RemoveAt(i);
                    EditorSaver.SetObjectDirty(this);
                    break;
                }

                DrawRouteInternal(color, autoConnectedPedestrianNodes[i]);
            }

            for (int i = 0; i < defaultConnectedPedestrianNodes?.Count; i++)
            {
                if (defaultConnectedPedestrianNodes[i] == null)
                {
                    defaultConnectedPedestrianNodes.RemoveAt(i);
                    EditorSaver.SetObjectDirty(this);
                    break;
                }

                DrawRouteInternal(color, defaultConnectedPedestrianNodes[i]);
            }
        }

        public void DrawTempRoutes(Color color, float sourceWidth, float targetWidth)
        {
            for (int i = 0; i < autoConnectedPedestrianNodes?.Count; i++)
            {
                if (autoConnectedPedestrianNodes[i] == null)
                {
                    autoConnectedPedestrianNodes.RemoveAt(i);
                    EditorSaver.SetObjectDirty(this);
                    break;
                }

                if (HasConnection(autoConnectedPedestrianNodes[i]))
                {
                    DrawNodeRouteConnection(transform.position, autoConnectedPedestrianNodes[i].transform.position, sourceWidth, targetWidth, color);
                }
            }

            for (int i = 0; i < defaultConnectedPedestrianNodes?.Count; i++)
            {
                if (defaultConnectedPedestrianNodes[i] == null)
                {
                    defaultConnectedPedestrianNodes.RemoveAt(i);
                    EditorSaver.SetObjectDirty(this);
                    break;
                }

                if (HasConnection(defaultConnectedPedestrianNodes[i]))
                {
                    DrawNodeRouteConnection(transform.position, defaultConnectedPedestrianNodes[i].transform.position, sourceWidth, targetWidth, color);
                }
            }
        }

        public void DrawNodeRouteConnection(Vector3 sourceNode, Vector3 targetNode, float sourceWidth, float targetWidth, Color color, float yOffset = 0.5f, float thickness = 0.4f)
        {
            PedestrianNodeUtils.DrawNodeRouteConnection(sourceNode, targetNode, Quaternion.identity, Quaternion.identity, sourceWidth, 0, targetWidth, 0, NodeShapeType.Circle, NodeShapeType.Circle, color, yOffset: yOffset, thickness: thickness);
        }

        private void DrawBounds()
        {
            switch (pedestrianNodeShapeType)
            {
                case NodeShapeType.Circle:
                    {
                        GizmosDrawer.DrawWireSphere(transform.position, maxPathWidth, Color.green);
                        break;
                    }
                case NodeShapeType.Square:
                    {
                        DrawRectangle();
                        break;
                    }
                case NodeShapeType.Rectangle:
                    {
                        DrawRectangle();
                        break;
                    }
            }
        }

        private void DrawRectangle()
        {
            var color = Color.magenta;
            var yOffset = 0.5f;

            DrawRectangle(color, yOffset);
        }

        private void DrawRectangle(Color color, float yOffset = 0.5f, float thickness = 0.4f)
        {
            Vector3 p1, p2, p3, p4;
            PedestrianNodeUtils.GetRectanglePoints(this, out p1, out p2, out p3, out p4, yOffset);

            GizmosDrawer.DrawLine(p1, p2, color);
            GizmosDrawer.DrawLine(p2, p3, color);
            GizmosDrawer.DrawLine(p3, p4, color);
            GizmosDrawer.DrawLine(p4, p1, color);
        }

        private void DrawRouteInternal(Color color, PedestrianNode node)
        {
            if (HasConnection(node))
            {
                PedestrianNodeUtils.DrawNodeRouteConnection(
                      transform.position,
                      node.transform.position,
                      transform.rotation,
                      node.transform.rotation,
                      MaxPathWidth,
                      Height,
                      node.MaxPathWidth,
                      node.height,
                      pedestrianNodeShapeType,
                      node.PedestrianNodeShapeType,
                      color,
                      this,
                      node);
            }
        }

        private bool ShouldDebug()
        {
            bool shouldDebug = false;

            if (PedestrianNodeCreator != null && UnityEditor.Selection.activeGameObject == PedestrianNodeCreator.gameObject)
            {
                shouldDebug = PedestrianNodeCreator.ShowPath(this);
            }
            else
            {
                shouldDebug = Debug.PathDebugger.ShouldDrawPedestrianConnectionPath;
            }

            return shouldDebug;
        }

        private void OnDrawGizmos()
        {
            if (!ShouldDebug())
                return;

            var color = EditorSettings.PedDefaultColor;

            if (this.chanceToSpawn == 0)
            {
                color = EditorSettings.PedWaypoint;
            }
            else if (connectedTrafficNode)
            {
                color = TryToGetMergeNode() == null ? EditorSettings.PedConnectedColor : EditorSettings.PedMergeColor;
            }

            DrawNode(transform.position, color);

            DrawConnectionNodeList(autoConnectedPedestrianNodes);
            DrawConnectionNodeList(defaultConnectedPedestrianNodes);

            void DrawConnectionNodeList(List<PedestrianNode> nodes)
            {
                for (int i = 0; i < nodes?.Count; i++)
                {
                    if (nodes[i] == null)
                    {
                        TryToRemoveNode(nodes[i]);
                        break;
                    }

                    var hasConnection = HasConnection(nodes[i]);

                    if (hasConnection)
                    {
                        DrawConnection(nodes[i]);
                    }
                }
            }

            void DrawConnection(PedestrianNode connectedPedestrianNode)
            {
                if (!connectedPedestrianNode)
                    return;

                Vector3 offset = new Vector3(0, 0, 0);
                Vector3 sourcePosition = transform.position + offset;
                Vector3 targetPosition = connectedPedestrianNode.transform.position + offset;

                bool fullConnected = connectedPedestrianNode.HasDoubleConnection(this);

                var color = fullConnected ? EditorSettings.PedDefaultColor : EditorSettings.PedPartialConnectionColor;

                // Avoid double draw
                if (!fullConnected || this.GetInstanceID() > connectedPedestrianNode.GetInstanceID())
                    GizmosDrawer.DrawLine(sourcePosition, targetPosition, color);

                var isOneway = IsOneWayConnection(connectedPedestrianNode);

                if (!fullConnected && isOneway)
                {
                    var pos = (sourcePosition + targetPosition) / 2;
                    var dir = Vector3.Normalize(targetPosition - sourcePosition);
                    DrawArrow(pos, dir, EditorSettings.PedPartialConnectionColor);
                }

                var data = TryToGetConnectionData(connectedPedestrianNode);

                if (data != null && data.SubNodeCount > 0 && (this.GetInstanceID() < connectedPedestrianNode.GetInstanceID() || isOneway))
                {
                    var subNodeCount = data.SubNodeCount;

                    for (int i = 0; i < subNodeCount; i++)
                    {
                        var pos = GetSubNodePosition(connectedPedestrianNode.transform.position, i, subNodeCount);
                        var pedSubNodeColor = EditorSettings.PedSubNodeColor;
                        DrawNode(pos, pedSubNodeColor);
                    }
                }
            }
        }

        private void DrawNode(Vector3 pos, Color color)
        {
            GizmosDrawer.DrawSphere(pos, 0.5f, color);
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShouldDebug())
                return;

            if (CreatorSelected)
                return;

            DrawRoutes();
            DrawBounds();

            var connectionSettings = GetConnectionDirectionSettings();

            TryToDrawArrow(connectionSettings.LeftHorizontal);
            TryToDrawArrow(connectionSettings.RightHorizontal);
            TryToDrawArrow(connectionSettings.LeftVertical);
            TryToDrawArrow(connectionSettings.RightVertical);

            void TryToDrawArrow(Vector3 offsetVector)
            {
                if (offsetVector == Vector3.zero)
                    return;

                Vector3 sourceTargetPoint = transform.position + offsetVector * GIZMOS_ARROW_LENGTH;
                Vector3 sourcePoint = transform.position;

                Vector3 direction = (sourceTargetPoint - sourcePoint).normalized;

                DrawArrow(sourcePoint, direction, Color.magenta);
            }
        }

        private void DrawArrow(Vector3 sourcePoint, Vector3 direction, Color color)
        {
            GizmosDrawer.DrawArrow(sourcePoint, direction, color, GIZMOS_ARROW_LINE_THICK, arrowLength: GIZMOS_ARROW_LENGTH);
        }

#endif

        #endregion
    }
}