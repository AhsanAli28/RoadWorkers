using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Spirit604.Extensions
{
    public static class EditorSaver
    {
        private static bool IsPlayMode => (!Application.isEditor || Application.isPlaying);

        public static void SetObjectDirty(Object obj)
        {
            if (IsPlayMode || obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void SetObjectDirty(GameObject gameObject)
        {
            if (IsPlayMode || gameObject == null)
            {
                return;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene); //This used to happen automatically from SetDirty
#endif
        }

        public static void SetObjectDirty(Component component)
        {
            if (IsPlayMode || component == null)
            {
                return;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene); //This used to happen automatically from SetDirty
#endif
        }
    }
}