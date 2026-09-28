#if UNITY_EDITOR
using Spirit604.DotsCity.Installer;
using Spirit604.Extensions;
using UnityEditor;

namespace Spirit604.DotsCity.Initialization.Installer
{
    [CustomEditor(typeof(ProjectContextSwitcher))]
    public class ProjectContextSwitcherEditor : Editor
    {
        private const string ProjectContextSwitcherTip = "ProjectContextSwitcherTip";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorTipExtension.TryToShowInspectorTip(ProjectContextSwitcherTip, "Helper component for switching from Zenject to manual resolve and back, if you don't need it, feel free to remove it.");
        }
    }
}
#endif