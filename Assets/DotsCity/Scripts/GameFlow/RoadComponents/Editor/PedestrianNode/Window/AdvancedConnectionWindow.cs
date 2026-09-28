#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class AdvancedConnectionWindow : EditorWindowBase
    {
        private enum ActionType { SplitConnection, JoinToConnection, CreateCustomRouteWidth, ChangeCurrentRouteWidth }

        private const float BUTTON_WIDTH = 50F;
        private const float DASH_SIZE = 4f;
        private const float DISK_RADIUS = 2f;
        private const float SMALL_PREVIEW_DISK_RADIUS = 0.5f;

        [SerializeField] private ActionType actionType = ActionType.JoinToConnection;
        [SerializeField] private PedestrianNode sourcePedestrianNode;
        [SerializeField] private PedestrianNode targetPedestrianNode1;
        [SerializeField] private PedestrianNode targetPedestrianNode2;
        [SerializeField] private bool attachToLine;
        [SerializeField] private Color previewConnectionColor = Color.white;
        [SerializeField] private Color currentRouteColor = Color.blue;
        [SerializeField] private Color customRouteColor = Color.magenta;
        [SerializeField][Range(0, 20f)] private float customRouteWidth = 1f;
        [SerializeField][Range(0, 20f)] private float offsetFromNodes = 1f;
        [SerializeField] private bool showCurrentRoute;
        [SerializeField][Range(1, 20)] private int splitCount = 1;

        private PedestrianNode SourcePedestrianNode
        {
            get => sourcePedestrianNode;
            set
            {
                if (sourcePedestrianNode)
                {
                    sourcePedestrianNode.OnHandleConnection -= PedestrianNode_OnHandleConnection;
                    sourcePedestrianNode.LockConnection = false;
                }

                if (value != null)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Selection Changed");
                    Undo.RegisterCompleteObjectUndo(value, "Node locked");
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

                    value.OnHandleConnection += PedestrianNode_OnHandleConnection;
                    value.LockConnection = true;

                    customRouteWidth = value.MaxPathWidth;
                }

                sourcePedestrianNode = value;
            }
        }

        public static event Action OnProcessedNodes = delegate { };
        public event Action OnClose = delegate { };

        public static AdvancedConnectionWindow ShowWindow()
        {
            AdvancedConnectionWindow window = (AdvancedConnectionWindow)GetWindow(typeof(AdvancedConnectionWindow));
            window.titleContent = new GUIContent("Advanced Connection");
            return window;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return base.GetDefaultWindowSize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
            SceneView.duringSceneGui += SceneView_OnSceneView;
            Selection.selectionChanged += Selection_selectionChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            OnClose();
            SourcePedestrianNode = null;

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
            SceneView.duringSceneGui -= SceneView_OnSceneView;
            Selection.selectionChanged -= Selection_selectionChanged;
        }

        private void OnDestroy()
        {
            SourcePedestrianNode = null;
        }

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            so.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(actionType)));

            if (EditorGUI.EndChangeCheck())
            {
                OnActionTypeChanged();
                OnValueChanged();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(targetPedestrianNode1)));

            if (actionType == ActionType.JoinToConnection)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(targetPedestrianNode2)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(attachToLine)));
            }

            if (actionType == ActionType.SplitConnection)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(splitCount)));
            }

            if (actionType == ActionType.CreateCustomRouteWidth || actionType == ActionType.ChangeCurrentRouteWidth)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(customRouteWidth)));
            }

            if (actionType == ActionType.CreateCustomRouteWidth)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(offsetFromNodes)));
            }

            if (actionType == ActionType.CreateCustomRouteWidth || actionType == ActionType.ChangeCurrentRouteWidth)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(showCurrentRoute)));
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(previewConnectionColor)));

            if (actionType == ActionType.CreateCustomRouteWidth || actionType == ActionType.ChangeCurrentRouteWidth)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(currentRouteColor)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(customRouteColor)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                OnValueChanged();
            }

            EditorGUILayout.Separator();

            ShowButtons();

            so.ApplyModifiedProperties();
        }

        private void ShowButtons()
        {
            GUI.enabled = CanProcessOperation();

            switch (actionType)
            {
                case ActionType.SplitConnection:
                    {
                        if (GUILayout.Button("Split Connection"))
                        {
                            SplitConnection();
                            OnProcessedNodes();
                        }

                        break;
                    }
                case ActionType.JoinToConnection:
                    {
                        if (GUILayout.Button("Join Connection"))
                        {
                            JoinConnection();
                            OnProcessedNodes();
                        }

                        break;
                    }
                case ActionType.CreateCustomRouteWidth:
                    {
                        if (GUILayout.Button("Create Custom Route Width"))
                        {
                            CreateCustomRoute();
                            OnProcessedNodes();
                        }

                        break;
                    }
                case ActionType.ChangeCurrentRouteWidth:
                    {
                        if (GUILayout.Button("Change Current Custom Route Width"))
                        {
                            ChangeCurrentCustomRoute();
                            OnProcessedNodes();
                        }

                        break;
                    }
            }

            GUI.enabled = true;
        }

        private void ShowAddButton(PedestrianNode pedestrianNode)
        {
            Action callback = () =>
            {
                Undo.RegisterCompleteObjectUndo(this, "Window Settings Changed");
                targetPedestrianNode1 = pedestrianNode;
                Repaint();
            };

            EditorExtension.DrawButton("+", pedestrianNode.transform.position, BUTTON_WIDTH, callback);
        }

        private void ShowRemoveButton()
        {
            Action callback = () =>
            {
                Undo.RegisterCompleteObjectUndo(this, "Window Settings Changed");
                targetPedestrianNode1 = null;
                Repaint();
            };

            EditorExtension.DrawButton("-", targetPedestrianNode1.transform.position, BUTTON_WIDTH, callback);
        }

        private void JoinConnection()
        {
            bool canJoin = targetPedestrianNode1 && targetPedestrianNode2 && SourcePedestrianNode && targetPedestrianNode1.HasDoubleConnection(targetPedestrianNode2);

            if (!canJoin)
                return;

            Undo.RegisterCompleteObjectUndo(SourcePedestrianNode, "PedestrianNode connection changed");
            Undo.RegisterCompleteObjectUndo(SourcePedestrianNode.transform, "PedestrianNode position changed");
            Undo.RegisterCompleteObjectUndo(targetPedestrianNode1, "Pedestrian Node connection changed");
            Undo.RegisterCompleteObjectUndo(targetPedestrianNode2, "Pedestrian Node connection changed");
            Undo.RegisterCompleteObjectUndo(this, "Window settings changed");

            targetPedestrianNode1.RemoveConnection(targetPedestrianNode2);
            targetPedestrianNode1.AddConnection(SourcePedestrianNode);
            targetPedestrianNode2.AddConnection(SourcePedestrianNode);

            if (attachToLine)
            {
                SourcePedestrianNode.transform.transform.position = GetAttachPosition();
            }

            targetPedestrianNode1 = null;
            targetPedestrianNode2 = null;
            Repaint();

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private void CreateCustomRoute()
        {
            bool sourceNodeHasConnection = SourceNodeHasConnection();

            if (!sourceNodeHasConnection)
                return;

            PedestrianNode newNode1 = null;
            PedestrianNode newNode2 = null;

            Vector3 newNode1position = GetNode1Position();
            Vector3 newNode2position = GetNode2Position();

#if UNITY_EDITOR
            newNode1 = PedestrianNodeUtils.CreatePrefab(sourcePedestrianNode, newNode1position, sourcePedestrianNode.transform.parent);
            newNode2 = PedestrianNodeUtils.CreatePrefab(sourcePedestrianNode, newNode2position, sourcePedestrianNode.transform.parent);
#endif

            Undo.RegisterCreatedObjectUndo(newNode1.gameObject, "Created New Pedestrian Node");
            Undo.RegisterCreatedObjectUndo(newNode2.gameObject, "Created New Pedestrian Node");
            Undo.RegisterCompleteObjectUndo(SourcePedestrianNode, "PedestrianNode connection changed");
            Undo.RegisterCompleteObjectUndo(targetPedestrianNode1, "Pedestrian Node connection changed");
            Undo.RegisterCompleteObjectUndo(this, "Window settings changed");

            SourcePedestrianNode.RemoveConnection(targetPedestrianNode1);

            SourcePedestrianNode.AddConnection(newNode1);
            newNode1.AddConnection(newNode2);
            newNode2.AddConnection(targetPedestrianNode1);

            newNode1.MaxPathWidth = customRouteWidth;
            newNode2.MaxPathWidth = customRouteWidth;

            if (SourcePedestrianNode.ConnectedTrafficNode != null && SourcePedestrianNode.ConnectedTrafficNode == targetPedestrianNode1.ConnectedTrafficNode)
            {
                newNode1.ConnectedTrafficNode = SourcePedestrianNode.ConnectedTrafficNode;
                newNode2.ConnectedTrafficNode = SourcePedestrianNode.ConnectedTrafficNode;
            }

            EditorSaver.SetObjectDirty(sourcePedestrianNode);
            EditorSaver.SetObjectDirty(targetPedestrianNode1);
            EditorSaver.SetObjectDirty(newNode1);
            EditorSaver.SetObjectDirty(newNode2);

            targetPedestrianNode1 = null;

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private void ChangeCurrentCustomRoute()
        {
            bool sourceNodeHasConnection = SourceNodeHasConnection();

            if (!sourceNodeHasConnection)
                return;

            Undo.RegisterCompleteObjectUndo(SourcePedestrianNode, "PedestrianNode settings changed");
            Undo.RegisterCompleteObjectUndo(targetPedestrianNode1, "Pedestrian Node settings changed");
            Undo.RegisterCompleteObjectUndo(this, "Window settings changed");

            SourcePedestrianNode.MaxPathWidth = customRouteWidth;
            targetPedestrianNode1.MaxPathWidth = customRouteWidth;

            EditorSaver.SetObjectDirty(sourcePedestrianNode);
            EditorSaver.SetObjectDirty(targetPedestrianNode1);

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private Vector3 GetNode2Position()
        {
            var direction = (targetPedestrianNode1.transform.position - sourcePedestrianNode.transform.position).normalized;
            return targetPedestrianNode1.transform.position - direction * offsetFromNodes;
        }

        private Vector3 GetNode1Position()
        {
            var direction = (targetPedestrianNode1.transform.position - sourcePedestrianNode.transform.position).normalized;
            return sourcePedestrianNode.transform.position + direction * offsetFromNodes;
        }

        private bool SourceNodeHasConnection()
        {
            return targetPedestrianNode1 != null && targetPedestrianNode1 != SourcePedestrianNode && SourcePedestrianNode.HasDoubleConnection(targetPedestrianNode1);
        }

        private void SplitConnection()
        {
            bool canSplit = SourceNodeHasConnection();

            if (!canSplit)
            {
                UnityEngine.Debug.Log($"Selected node doesn't have connection with source node");
                return;
            }

            Undo.RegisterCompleteObjectUndo(this, "Window settings changed");
            Undo.RegisterCompleteObjectUndo(SourcePedestrianNode, "Pedestrian Node connection changed");
            Undo.RegisterCompleteObjectUndo(targetPedestrianNode1, "Pedestrian Node connection changed");

            SourcePedestrianNode.RemoveConnection(targetPedestrianNode1);

            var previousNode = SourcePedestrianNode;
            PedestrianNode firstCreatedNode = null;

            for (int i = 0; i < splitCount; i++)
            {
                var createPosition = GetSplitPosition(i);

                PedestrianNode newNode = null;

#if UNITY_EDITOR
                newNode = PedestrianNodeUtils.CreatePrefab(sourcePedestrianNode, createPosition, sourcePedestrianNode.transform.parent);
#endif

                Undo.RegisterCreatedObjectUndo(newNode.gameObject, "Created New Pedestrian Node");

                previousNode.AddConnection(newNode);
                previousNode = newNode;

                if (i == 0)
                {
                    firstCreatedNode = newNode;
                }

                if (i == splitCount - 1)
                {
                    targetPedestrianNode1.AddConnection(newNode);
                    targetPedestrianNode1 = firstCreatedNode;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private Vector3 GetSplitPosition(int i)
        {
            float t = (float)(i + 1) / (splitCount + 1);

            Vector3 createPosition = Vector3.Lerp(SourcePedestrianNode.transform.position, targetPedestrianNode1.transform.position, t);

            return createPosition;
        }

        public void Initialize(PedestrianNode pedestrianNode)
        {
            SourcePedestrianNode = pedestrianNode;
        }

        private void Clear()
        {
            targetPedestrianNode1 = null;
            targetPedestrianNode2 = null;
        }

        private void HandleTargetNode()
        {
            if (targetPedestrianNode1 == null)
            {
                for (int i = 0; i < SourcePedestrianNode.AutoConnectedPedestrianNodes.Count; i++)
                {
                    var node = SourcePedestrianNode.AutoConnectedPedestrianNodes[i];

                    ShowAddButton(node);
                }

                for (int i = 0; i < SourcePedestrianNode.DefaultConnectedPedestrianNodes.Count; i++)
                {
                    var node = SourcePedestrianNode.DefaultConnectedPedestrianNodes[i];

                    ShowAddButton(node);
                }
            }
            else
            {
                ShowRemoveButton();
            }
        }

        private Vector3 GetAttachPosition()
        {
            var point = sourcePedestrianNode.transform.position;

            if (targetPedestrianNode1 && targetPedestrianNode2)
            {
                point = VectorExtensions.FindNearestPointOnLine(targetPedestrianNode1.transform.position, (targetPedestrianNode2.transform.position - targetPedestrianNode1.transform.position).normalized, point);
            }

            return point;
        }

        private void DrawSmallWireDisk(Vector3 position)
        {
            var sourceColor = Handles.color;
            Handles.color = previewConnectionColor;
            Handles.DrawWireDisc(position, Vector3.up, SMALL_PREVIEW_DISK_RADIUS);
            Handles.color = sourceColor;
        }

        private bool CanProcessOperation()
        {
            switch (actionType)
            {
                case ActionType.SplitConnection:
                    {
                        return targetPedestrianNode1;
                    }
                case ActionType.JoinToConnection:
                    {
                        return targetPedestrianNode1 && targetPedestrianNode2;
                    }
                case ActionType.CreateCustomRouteWidth:
                    {
                        return targetPedestrianNode1;
                    }
                case ActionType.ChangeCurrentRouteWidth:
                    {
                        return targetPedestrianNode1;
                    }
            }

            return true;
        }

        private void OnActionTypeChanged()
        {
            Clear();
        }

        private void OnValueChanged()
        {
            Repaint();

            var sceneView = SceneView.lastActiveSceneView;
            sceneView?.Repaint();
        }

        private void PedestrianNode_OnHandleConnection(PedestrianNode pedestrianNode)
        {
            if (this == null)
            {
                pedestrianNode.ClearAllSubscriptions();
                pedestrianNode.LockConnection = false;
                return;
            }

            Undo.RegisterCompleteObjectUndo(this, "Window Settings Changed");

            switch (actionType)
            {
                case ActionType.SplitConnection:
                    {
                        if (targetPedestrianNode1 != pedestrianNode && sourcePedestrianNode.HasDoubleConnection(pedestrianNode))
                        {
                            targetPedestrianNode1 = pedestrianNode;
                        }
                        else
                        {
                            targetPedestrianNode1 = null;
                        }

                        break;
                    }
                case ActionType.CreateCustomRouteWidth:
                    {
                        if (targetPedestrianNode1 != pedestrianNode && sourcePedestrianNode.HasDoubleConnection(pedestrianNode))
                        {
                            targetPedestrianNode1 = pedestrianNode;
                        }
                        else
                        {
                            targetPedestrianNode1 = null;
                        }

                        break;
                    }
                case ActionType.ChangeCurrentRouteWidth:
                    {
                        if (targetPedestrianNode1 != pedestrianNode && sourcePedestrianNode.HasDoubleConnection(pedestrianNode))
                        {
                            targetPedestrianNode1 = pedestrianNode;
                        }
                        else
                        {
                            targetPedestrianNode1 = null;
                        }

                        break;
                    }
                case ActionType.JoinToConnection:
                    {
                        if (!targetPedestrianNode1 && !targetPedestrianNode2)
                        {
                            targetPedestrianNode1 = pedestrianNode;
                        }
                        else if (targetPedestrianNode1 != pedestrianNode && !targetPedestrianNode2)
                        {
                            targetPedestrianNode2 = pedestrianNode;
                        }
                        else if (targetPedestrianNode1 == pedestrianNode)
                        {
                            targetPedestrianNode1 = null;
                        }
                        else if (targetPedestrianNode2 == pedestrianNode)
                        {
                            targetPedestrianNode2 = null;
                        }
                        else if (!targetPedestrianNode1 && targetPedestrianNode2 != pedestrianNode)
                        {
                            targetPedestrianNode1 = pedestrianNode;
                        }
                        else
                        {
                            targetPedestrianNode2 = pedestrianNode;
                        }

                        break;
                    }
            }

            Repaint();
        }

        private void SceneView_OnSceneView(SceneView sceneview)
        {
            switch (actionType)
            {
                case ActionType.SplitConnection:
                    {
                        if (!SourcePedestrianNode)
                            return;

                        HandleTargetNode();

                        if (targetPedestrianNode1 != null)
                        {
                            for (int i = 0; i < splitCount; i++)
                            {
                                var splitPosition = GetSplitPosition(i);
                                DrawSmallWireDisk(splitPosition);
                            }
                        }

                        break;
                    }
                case ActionType.CreateCustomRouteWidth:
                    {
                        if (!SourcePedestrianNode)
                            return;

                        HandleTargetNode();

                        if (sourcePedestrianNode && targetPedestrianNode1)
                        {
                            Vector3 newNode1position = GetNode1Position();
                            Vector3 newNode2position = GetNode2Position();

                            DrawSmallWireDisk(newNode1position);
                            DrawSmallWireDisk(newNode2position);

                            sourcePedestrianNode.DrawNodeRouteConnection(newNode1position, newNode2position, customRouteWidth, customRouteWidth, customRouteColor);

                            if (showCurrentRoute)
                            {
                                sourcePedestrianNode.DrawNodeRouteConnection(sourcePedestrianNode.transform.position, targetPedestrianNode1.transform.position, sourcePedestrianNode.MaxPathWidth, targetPedestrianNode1.MaxPathWidth, currentRouteColor);
                            }
                        }

                        break;
                    }
                case ActionType.ChangeCurrentRouteWidth:
                    {
                        if (!SourcePedestrianNode)
                            return;

                        HandleTargetNode();

                        if (sourcePedestrianNode && targetPedestrianNode1)
                        {
                            sourcePedestrianNode.DrawNodeRouteConnection(sourcePedestrianNode.transform.position, targetPedestrianNode1.transform.position, customRouteWidth, customRouteWidth, customRouteColor);

                            if (showCurrentRoute)
                            {
                                sourcePedestrianNode.DrawNodeRouteConnection(sourcePedestrianNode.transform.position, targetPedestrianNode1.transform.position, sourcePedestrianNode.MaxPathWidth, targetPedestrianNode1.MaxPathWidth, currentRouteColor);
                            }
                        }

                        break;
                    }
                case ActionType.JoinToConnection:
                    {
                        if (!sourcePedestrianNode)
                            return;

                        bool hasConnection = true;

                        Vector3 sourcePosition = sourcePedestrianNode.transform.position;

                        if (targetPedestrianNode1 && targetPedestrianNode2)
                        {
                            hasConnection = targetPedestrianNode1.HasDoubleConnection(targetPedestrianNode2);

                            if (hasConnection && attachToLine)
                            {
                                sourcePosition = GetAttachPosition();

                                Handles.color = Color.green;
                                Handles.DrawWireDisc(sourcePosition, Vector3.up, DISK_RADIUS);
                            }
                        }

                        Handles.color = hasConnection ? previewConnectionColor : Color.red;

                        if (targetPedestrianNode1)
                        {
                            Handles.DrawDottedLine(sourcePosition, targetPedestrianNode1.transform.position, DASH_SIZE);
                            Handles.DrawWireDisc(targetPedestrianNode1.transform.position, Vector3.up, DISK_RADIUS);
                        }

                        if (targetPedestrianNode2)
                        {
                            Handles.DrawDottedLine(sourcePosition, targetPedestrianNode2.transform.position, DASH_SIZE);
                            Handles.DrawWireDisc(targetPedestrianNode2.transform.position, Vector3.up, DISK_RADIUS);
                        }

                        break;
                    }
            }
        }

        private void Selection_selectionChanged()
        {
            var go = Selection.activeGameObject;

            if (go != null)
            {
                var node = go.GetComponent<PedestrianNode>();

                if (node != null)
                {
                    SourcePedestrianNode = node;

                    if (actionType == ActionType.SplitConnection)
                    {
                        targetPedestrianNode1 = null;
                    }

                    Repaint();
                }
            }
        }

        private void Undo_undoRedoPerformed()
        {
            Repaint();
        }
    }
}
#endif