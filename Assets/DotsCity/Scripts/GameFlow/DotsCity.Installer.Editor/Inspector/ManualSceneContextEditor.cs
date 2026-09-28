#if UNITY_EDITOR
using Spirit604.DotsCity.Installer;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Initialization.Installer
{
    /// <summary>
    /// An analogue of the Zenject scene context that starts manual resolution for each installer.
    /// </summary>
    [CustomEditor(typeof(ManualSceneContext))]
    public class ManualSceneContextEditor : Editor
    {
        private const string ManualSceneContext = "ManualSceneContextTip";

        private ManualSceneContext manualSceneContext;

        private void OnEnable()
        {
            manualSceneContext = target as ManualSceneContext;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorTipExtension.TryToShowInspectorTip(ManualSceneContext, "An analogue of the Zenject scene context that starts manual resolution for each installer.");

#if ZENJECT
            EditorGUILayout.HelpBox("Zenject found. Make sure you are using Zenject or ManualSceneContex.", MessageType.Warning);
            GUI.enabled = false;
#endif

            if (GUILayout.Button("Rebind Inspector"))
            {
                manualSceneContext.RebindInspector();
            }

            GUI.enabled = true;
        }
    }
}
#endif