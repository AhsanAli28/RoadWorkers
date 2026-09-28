#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathDataViewerWindow : EditorWindowBase
    {
        #region Constans

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/pathDebug.html#path-data-viewer";

        #endregion

        #region Helper types
        private enum PathViewType { SpeedLimit, Priority, PathType, TrafficPathGroup, TrafficPathNodeGroup, NodeDirection, ArrowLight, Rail }

        #endregion

        #region Serializable variables

        [Tooltip("Default path color")]
        [SerializeField] private Color defaultColor = Color.white;

        [Tooltip("" +
            "<b>Speed limit</b> : speed limit of the paths\r\n\r\n" +
            "<b>Priority</b> : priority of the paths\r\n\r\n" +
            "<b>Path type</b> : path type of the paths\r\n\r\n" +
            "<b>Traffic path group</b> : traffic group of the paths\r\n\r\n" +
            "<b>Traffic path node group</b> : traffic group of the waypoints\r\n\r\n" +
            "<b>Node direction</b> : node direction (forward or backward) of the waypoints in the paths\r\n\r\n" +
            "<b>Arrow light</b> : shows the paths with the assigned custom light\r\n\r\n" +
            "<b>Rail</b> : shows the paths with the rail parameter")]
        [SerializeField] private PathViewType pathViewType;

        [Tooltip("On/off custom colors of the paths on the scene")]
        [SerializeField] private bool drawCustomColors = true;

        [Tooltip("Show world path selection buttons")]
        [SerializeField] private bool showWorldButtons = true;

        [Tooltip("On/off visual intersection points on the scene")]
        [SerializeField] private bool showIntersectPoints;

        [Tooltip("On/off waypoints of the path on the scene")]
        [SerializeField] private bool showWayPoints;

        [Tooltip("On/off path position handles of the selected path")]
        [SerializeField] private bool showPathHandles = true;

        [Tooltip("On/off path edit buttons of the selected path")]
        [SerializeField] private bool showPathEditButtons = true;

        [Tooltip("On/off feature to select multiple paths at the same time (useful for setting the same value for multiple paths)")]
        [SerializeField] private bool multipleSelection;

        [Tooltip("Show unselect button for already selected paths in multiple selection mode")]
        [SerializeField] private bool showUnselectButtons;

        [SerializeField] private bool generalSettingsFoldout = true;

        #endregion

        #region Variables

        private Path[] paths;
        private Path selectedPath;
        private List<Path> selectedPaths = new List<Path>();

        private Dictionary<PathViewType, PathDataViewerBase> customDataViewers;

        #endregion

        #region Properties
        public bool DrawCustomColors { get => drawCustomColors; }
        public Color DefaultColor { get => defaultColor; }
        public Path[] Paths { get => paths; }
        #endregion

        #region Unity methods

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Path Data Viewer")]
        public static PathDataViewerWindow ShowWindow()
        {
            PathDataViewerWindow window = (PathDataViewerWindow)GetWindow(typeof(PathDataViewerWindow));
            window.titleContent = new GUIContent("Path Data Viewer");
            return window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            LoadData();

            if (customDataViewers == null)
            {
                customDataViewers = new Dictionary<PathViewType, PathDataViewerBase>();
                customDataViewers.Add(PathViewType.SpeedLimit, new PathDataSpeedLimitViewer());
                customDataViewers.Add(PathViewType.Priority, new PathDataPriorityViewer());
                customDataViewers.Add(PathViewType.PathType, new PathTypeDataViewer());
                customDataViewers.Add(PathViewType.TrafficPathGroup, new PathGroupTypeDataViewer());
                customDataViewers.Add(PathViewType.TrafficPathNodeGroup, new PathNodeGroupTypeViewer());
                customDataViewers.Add(PathViewType.NodeDirection, new PathDataNodeDirectionViewer());
                customDataViewers.Add(PathViewType.ArrowLight, new PathArrowLightDataViewer());
                customDataViewers.Add(PathViewType.Rail, new PathRailDataViewer());

                foreach (var controller in customDataViewers)
                {
                    controller.Value.LoadData();
                    controller.Value.Initialize(this);
                }
            }

            LoadScenePathData();

            SceneView.duringSceneGui += SceneView_duringSceneGui;
            PathSettingsWindowEditor.OnSetSpeedClick += PathSettingsWindowEditor_OnSetSpeedClick;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SaveData();

            ClearSelection();

            foreach (var controller in customDataViewers)
            {
                controller.Value.SaveData();
            }

            customDataViewers[pathViewType].SwitchEnabledState(false);

            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            PathSettingsWindowEditor.OnSetSpeedClick -= PathSettingsWindowEditor_OnSetSpeedClick;
        }

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            so.Update();

            DrawCommonSettings(so);
            customDataViewers[pathViewType].DrawCustomSettings();

            if (GUILayout.Button("Refresh"))
            {
                LoadScenePathData();
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Methods

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(350, 400);
        }

        private void DrawCommonSettings(SerializedObject so)
        {
            Action commonSettingsContent = () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(DocLink, 36f);

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(defaultColor)));

                if (EditorGUI.EndChangeCheck())
                {
                    Action<Path> updateColorAction = (path) =>
                    {
                        path.HighlightColor = defaultColor;
                        EditorSaver.SetObjectDirty(path);
                    };

                    UpdatePathData(updateColorAction);
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(pathViewType)));

                if (EditorGUI.EndChangeCheck())
                {
                    customDataViewers[pathViewType].SwitchEnabledState(false);
                    so.ApplyModifiedProperties();
                    customDataViewers[pathViewType].UpdateData(paths);
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(drawCustomColors)));

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    customDataViewers[pathViewType].SwitchEnabledState(drawCustomColors);
                    SceneView.RepaintAll();
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(showWorldButtons)));

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(showIntersectPoints)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(showWayPoints)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(showPathHandles)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(showPathEditButtons)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(multipleSelection)));

                if (multipleSelection)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(showUnselectButtons)));
                }

                if (multipleSelection)
                {
                    GUI.enabled = selectedPaths.Count > 0;

                    if (GUILayout.Button("Clear Selection"))
                    {
                        ClearSelection();
                    }

                    GUI.enabled = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();

                    Action<Path> updateDataAction = (path) =>
                    {
                        path.ShowIntersectedPoints = showIntersectPoints;
                        path.ShowInfoWaypoints = showWayPoints;
                        path.ShowHandles = showPathHandles;
                        path.ShowEditButtons = showPathEditButtons;
                        EditorSaver.SetObjectDirty(path);
                    };

                    UpdatePathData(updateDataAction);
                    SceneView.RepaintAll();
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Common Settings", commonSettingsContent, ref generalSettingsFoldout);
        }

        private void LoadScenePathData()
        {
            var root = EditorExtension.GetPrefabStageRoot();

            if (root == null)
            {
                paths = ObjectUtils.FindObjectsOfType<Path>();
            }
            else
            {
                paths = root.GetComponentsInChildren<Path>();
            }

            foreach (var path in paths)
            {
                if (path.HighlightColor != defaultColor)
                {
                    path.HighlightColor = defaultColor;
                    EditorSaver.SetObjectDirty(path);
                }
            }

            customDataViewers[pathViewType].UpdateData(paths);

            SceneView.RepaintAll();
        }

        private void UpdatePathData(Action<Path> callback)
        {
            foreach (var path in paths)
            {
                callback(path);
            }
        }

        private bool Selected(Path path)
        {
            return selectedPaths.Contains(path);
        }

        private void ClearSelection()
        {
            for (int i = 0; i < selectedPaths.Count; i++)
            {
                ClearSelection(selectedPaths[i]);
            }

            ClearSelection(selectedPath);
            selectedPath = null;
            selectedPaths.Clear();
        }

        private void ClearSelection(Path path)
        {
            if (path)
            {
                path.Highlighted = false;
            }
        }

        private void Unselect(Path path)
        {
            if (selectedPaths.TryToRemove(path))
            {
                ClearSelection(path);
            }

            if (selectedPath == path)
            {
                ClearSelection(path);
                selectedPath = null;
            }
        }

        #endregion

        #region Event Handlers

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (showWorldButtons)
            {
                foreach (var path in paths)
                {
                    if (!path)
                    {
                        continue;
                    }

                    bool selected = Selected(path);
                    bool shouldShowButton = customDataViewers[pathViewType].ShouldShowPathButton(path);

                    if (shouldShowButton)
                    {
                        var position = path.GetMiddlePosition();

                        if (!selected)
                        {
                            Action selectCallback = () =>
                            {
                                if (!multipleSelection)
                                {
                                    Selection.activeObject = path;
                                    ClearSelection(selectedPath);
                                    selectedPath = path;
                                    path.Highlighted = true;
                                }
                                else
                                {
                                    if (selectedPaths.TryToAdd(path))
                                    {
                                        path.Highlighted = true;
                                        Selection.objects = selectedPaths.ToArray();
                                    }
                                }

                                Repaint();
                            };

                            EditorExtension.DrawButton("P", position, 35f, selectCallback, centralizeGuiAlign: true, drawOnlyInView: true);
                        }
                    }

                    if (selected && multipleSelection && showUnselectButtons)
                    {
                        var position = path.GetMiddlePosition();

                        Action unselectCallback = () =>
                        {
                            Unselect(path);
                            Repaint();
                        };

                        EditorExtension.DrawButton("P-", position, 35f, unselectCallback, centralizeGuiAlign: true, drawOnlyInView: true);
                    }

                }
            }
        }

        private void PathSettingsWindowEditor_OnSetSpeedClick()
        {
            LoadScenePathData();
            Repaint();
        }

        #endregion
    }
}
#endif