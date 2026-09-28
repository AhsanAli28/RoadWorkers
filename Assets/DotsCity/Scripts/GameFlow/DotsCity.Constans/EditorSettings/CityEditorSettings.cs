#if UNITY_EDITOR
using Spirit604.Extensions;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor
{
    public class CityEditorSettings : ScriptableObject
    {
        private const string FolderPath = "Prefabs/CityEditor/Editor/";
        private const string FileName = "CityEditorSettings.asset";

        [Tooltip("Right (default) lane TrafficNode color")]
        [SerializeField] private Color arrowColor = Color.green;

        [Tooltip("Right (default) lane TrafficNode color")]
        [SerializeField] private Color mergeArrowColor = Color.magenta;

        [Tooltip("Left (external) lane TrafficNode color")]
        [SerializeField] private Color externalArrowColor = Color.blue;

        [Tooltip("Path TrafficNode color")]
        [SerializeField] private Color pathArrowColor = Color.yellow;

        [Tooltip("Traffic node arrow length")]
        [SerializeField][Range(0, 5)] float arrowLength = 3f;

        [Tooltip("Traffic node arrow thickness")]
        [SerializeField][Range(0, 2)] float arrowThickness = 0.7f;

        [Tooltip("Pedestrian default color")]
        [SerializeField] private Color pedDefaultColor = Color.green;

        [Tooltip("Pedestrian subnode color")]
        [SerializeField] private Color pedSubNodeColor = Color.yellow;

        [Tooltip("Pedestrian partial connection color")]
        [SerializeField] private Color pedPartialConnectionColor = Color.yellow;

        [Tooltip("Pedestrian connected to traffic node color")]
        [SerializeField] private Color pedConnectedColor = Color.blue;

        [Tooltip("Pedestrian merge color")]
        [SerializeField] private Color pedMergeColor = Color.magenta;

        [Tooltip("Pedestrian waypoint")]
        [SerializeField] private Color pedWaypoint = Color.white;

        [Tooltip("Auto sync city scene config between MainScene & Subscene on change")]
        [SerializeField] private bool syncConfigOnChange;

        [Tooltip("Subscene will open & sync after config change, otherwise config will only sync when subscene is opened by user")]
        [SerializeField] private bool autoOpenClosedScene = true;

        [Tooltip("Auto close subscene if the subscene was closed before config change after config deselection")]
        [SerializeField] private bool autoCloseScene = true;

        [SerializeField] private bool autoSaveChanges = true;

        /// <summary> Used in the <b>TrafficFollowPathDebugger</b>. </summary>        
        [field: SerializeField] public Color FollowPathColor { get; set; } = Color.blue;

        //Path Creator
        [Tooltip("Unselect selcted traffic nodes")]
        [SerializeField] private KeyCode creatorUnselectNodes = KeyCode.Escape;

        [Tooltip("Create a path when traffic nodes are selected")]
        [SerializeField] private KeyCode creatorCreatePath = KeyCode.F1;

        [Tooltip("Swap source & target nodes")]
        [SerializeField] private KeyCode creatorSwapNodes = KeyCode.Tab;

        public Color ArrowColor => arrowColor;
        public Color MergeArrowColor => mergeArrowColor;
        public Color ExternalArrowColor => externalArrowColor;
        public Color SubNodeTrafficColor => pathArrowColor;
        public Color PedSubNodeColor => pedSubNodeColor;
        public float ArrowLength => arrowLength;
        public float ArrowThickness => arrowThickness;

        public bool SyncConfigOnChange => syncConfigOnChange;
        public bool AutoOpenClosedScene => autoOpenClosedScene;
        public bool AutoCloseScene => autoCloseScene;
        public bool AutoSaveChanges => autoSaveChanges || autoCloseScene;

        public Color PedConnectedColor { get => pedConnectedColor; set => pedConnectedColor = value; }
        public Color PedMergeColor { get => pedMergeColor; set => pedMergeColor = value; }
        public Color PedWaypoint { get => pedWaypoint; set => pedWaypoint = value; }
        public Color PedDefaultColor { get => pedDefaultColor; set => pedDefaultColor = value; }
        public Color PedPartialConnectionColor { get => pedPartialConnectionColor; set => pedPartialConnectionColor = value; }
        public KeyCode CreatorUnselectNodes { get => creatorUnselectNodes; set => creatorUnselectNodes = value; }
        public KeyCode CreatorCreatePath { get => creatorCreatePath; set => creatorCreatePath = value; }
        public KeyCode CreatorSwapNodes { get => creatorSwapNodes; set => creatorSwapNodes = value; }

        private static CityEditorSettings settings;

        public static CityEditorSettings GetOrCreateSettings()
        {
            if (settings != null)
            {
                return settings;
            }

            var folderPath = CityEditorBookmarks.GetPath(FolderPath);
            var fullPath = Path.Combine(folderPath, FileName);

            settings = AssetDatabase.LoadAssetAtPath<CityEditorSettings>(fullPath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CityEditorSettings>();

                AssetDatabaseExtension.CheckForFolderExist(folderPath);
                AssetDatabase.CreateAsset(settings, fullPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        public static SerializedObject GetSerializedSettings() => new SerializedObject(GetOrCreateSettings());

        public void MakeDirty()
        {
            EditorUtility.SetDirty(this);
        }
    }
}
#endif