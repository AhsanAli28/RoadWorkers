using UnityEngine;

#if !UNITY_SPLINE
using Spirit604.Attributes;
using UnityEditor;
using UnityEditor.PackageManager;
#endif

namespace Spirit604.DotsCity.Samples.CustomTrain
{
#if !UNITY_SPLINE
    [ExecuteInEditMode]
#endif
    public class SplineInstaller : MonoBehaviour
    {
#if UNITY_EDITOR && !UNITY_SPLINE

        private const string PackageName = "com.unity.splines";

        private bool installed;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        }

        [Button]
        public void Install()
        {
            installed = true;
            Debug.Log($"Installing '{PackageName}' package");
            Client.Add($"{PackageName}");
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.isPlaying = false;

                if (!installed)
                {
                    Debug.Log($"Install the Unity Spline package before starting the scene. Select SplineInstaller in the scene & press the Install button.");
                    EditorGUIUtility.PingObject(gameObject);
                }
                else
                {
                    Debug.Log($"Installation in progress");
                }
            }
        }

#endif
    }
}
