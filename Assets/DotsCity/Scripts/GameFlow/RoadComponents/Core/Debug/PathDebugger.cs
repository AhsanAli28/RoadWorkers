using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.Gameplay.Road.Debug
{
    [ExecuteInEditMode]
    public class PathDebugger : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pathDebug.html#path-debugger")]
        [SerializeField] private string link;

        [Tooltip("Path visualisation in the scene in Editor")]
        [SerializeField] private bool drawEditorTrafficPath;

        [Tooltip("Entity path visualisation in the scene at runtime")]
        [SerializeField] private bool drawEntityTrafficPath;

        [Tooltip("Draw TrafficNode entity connection debug at runtime")]
        [SerializeField] private bool drawEntityTrafficNodeConnection;

        [Tooltip("PedestrianNode connection visualisation in the scene in the Editor")]
        [SerializeField] private bool drawPedestrianConnectionPath;

        public bool DrawEntityTrafficPath { get => drawEntityTrafficPath; set => drawEntityTrafficPath = value; }

#if UNITY_EDITOR

        public static bool ShouldDrawEditorPath { get; set; }
        public static bool ShouldDrawEntityPath { get; set; }
        public static bool ShoulDrawEntityNodeConnection { get; set; }
        public static bool ShouldDrawPedestrianConnectionPath { get; set; }

        private void Update()
        {
            ShouldDrawEditorPath = drawEditorTrafficPath;
            ShouldDrawEntityPath = drawEntityTrafficPath;
            ShoulDrawEntityNodeConnection = drawEntityTrafficNodeConnection;
            ShouldDrawPedestrianConnectionPath = drawPedestrianConnectionPath;
        }
#endif
    }
}