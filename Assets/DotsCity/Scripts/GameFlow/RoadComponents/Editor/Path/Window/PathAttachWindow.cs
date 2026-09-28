#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.CityEditor.Road
{
    public class PathAttachWindow : EditorWindow
    {
        public Object sourceTrafficNodeGo;
        public Object targetTrafficNodeGo;
        public int sourceLaneIndex;
        private int targetLaneIndex;

        private Path selectedPath;
        private bool shouldReparent = true;
        private bool connectSameIndex = true;
        private bool attachToNodes = true;
        public bool isRightSide = true;

        public Path SelectedPath
        {
            get => selectedPath;
            set
            {
                if (selectedPath)
                    selectedPath.Highlighted = false;
                selectedPath = value;
                selectedPath.Highlighted = true;
            }
        }

        public int TargetLaneIndex
        {
            get
            {
                if (!connectSameIndex)
                {
                    return targetLaneIndex;
                }
                else
                {
                    return sourceLaneIndex;
                }
            }
        }

        public static PathAttachWindow ShowWindow()
        {
            PathAttachWindow trafficNodePathCreatorWindow = (PathAttachWindow)GetWindow(typeof(PathAttachWindow));
            trafficNodePathCreatorWindow.titleContent = new GUIContent("PathAttachWindow");

            Vector2 windowSize = new Vector2(350, 200);
            trafficNodePathCreatorWindow.position = new Rect((float)Screen.currentResolution.width / 2, (float)Screen.currentResolution.height / 2, windowSize.x, windowSize.y);
            trafficNodePathCreatorWindow.minSize = windowSize;
            return trafficNodePathCreatorWindow;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            sourceTrafficNodeGo = EditorGUILayout.ObjectField("Source Road Node", sourceTrafficNodeGo, typeof(TrafficNode), true);

            if (GUILayout.Button("x", GUILayout.Width(25)))
            {
                sourceTrafficNodeGo = null;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            targetTrafficNodeGo = EditorGUILayout.ObjectField("Target Road Node", targetTrafficNodeGo, typeof(TrafficNode), true);

            if (GUILayout.Button("x", GUILayout.Width(25)))
            {
                targetTrafficNodeGo = null;
            }

            EditorGUILayout.EndHorizontal();

            shouldReparent = EditorGUILayout.Toggle("Should Reparent", shouldReparent);
            attachToNodes = EditorGUILayout.Toggle("Attach To Nodes", attachToNodes);
            isRightSide = EditorGUILayout.Toggle("Is Right Side", isRightSide);

            connectSameIndex = EditorGUILayout.Toggle("Connect Same Lane Index", connectSameIndex);

            int maxLaneCount = 10;

            var sourceTrafficNode = sourceTrafficNodeGo as TrafficNode;

            if (sourceTrafficNode)
            {
                maxLaneCount = sourceTrafficNode.LaneCount - 1;
            }

            EditorGUI.BeginChangeCheck();

            sourceLaneIndex = EditorGUILayout.IntSlider("Source Lane Index", sourceLaneIndex, 0, maxLaneCount);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (!connectSameIndex)
            {
                var targetTrafficNode = targetTrafficNodeGo as TrafficNode;

                maxLaneCount = 10;

                if (targetTrafficNode)
                {
                    maxLaneCount = targetTrafficNode.LaneCount - 1;
                }

                EditorGUI.BeginChangeCheck();

                targetLaneIndex = EditorGUILayout.IntSlider("Target Lane Index", targetLaneIndex, 0, maxLaneCount);

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Swap Nodes"))
            {
                SwapNodes();
            }
            if (GUILayout.Button("Attach"))
            {
                AttachPath();
            }
        }

        private void AttachPath()
        {
            var pathAttachInfo = new PathAttachHelper.PathAttachInfo()
            {
                SourceTrafficNode = sourceTrafficNodeGo as TrafficNode,
                TargetTrafficNode = targetTrafficNodeGo as TrafficNode,
                SelectedPath = selectedPath,
                SourceLaneIndex = sourceLaneIndex,
                TargetLaneIndex = targetLaneIndex,
                ShouldReparent = shouldReparent,
                AttachToNodes = attachToNodes,
                IsRightSide = isRightSide,
                ConnectSameIndex = connectSameIndex
            };

            PathAttachHelper.Attach(pathAttachInfo);
        }

        private void OnDisable()
        {
            if (selectedPath)
                selectedPath.Highlighted = false;
        }

        public void AddNode(TrafficNode road)
        {
            if (sourceTrafficNodeGo == null && targetTrafficNodeGo != road)
            {
                sourceTrafficNodeGo = road;
            }
            else if (sourceTrafficNodeGo != road)
            {
                targetTrafficNodeGo = road;
            }
        }

        public void RemoveNode(TrafficNode road)
        {
            if (sourceTrafficNodeGo == road)
            {
                sourceTrafficNodeGo = null;
            }
            if (targetTrafficNodeGo == road)
            {
                targetTrafficNodeGo = null;
            }
        }

        private void SwapNodes()
        {
            var temp = sourceTrafficNodeGo;
            sourceTrafficNodeGo = targetTrafficNodeGo;
            targetTrafficNodeGo = temp;
        }
    }
}
#endif