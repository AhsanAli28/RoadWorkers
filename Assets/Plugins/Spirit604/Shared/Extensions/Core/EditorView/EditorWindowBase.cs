#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class EditorWindowHelper<T> where T : EditorWindow
    {
        public static T ShowWindow()
        {
            T window = (T)EditorWindow.GetWindow(typeof(T));
            window.Show();

            return window;
        }
    }

    public class EditorWindowBase : EditorWindow
    {
        protected virtual void OnEnable()
        {
            Vector2 windowSize = GetDefaultWindowSize();

            try
            {
                this.position = new Rect((float)Screen.currentResolution.width / 2, (float)Screen.currentResolution.height / 2, windowSize.x, windowSize.y);
            }
            catch { }

            this.minSize = windowSize;
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual Vector2 GetDefaultWindowSize()
        {
            return new Vector2(350, 200);
        }

        protected void SaveData(string saveName = "")
        {
            string currentName = GetName(saveName);

            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(currentName, data);
        }

        protected void LoadData(string saveName = "")
        {
            string currentName = GetName(saveName);

            var data = EditorPrefs.GetString(currentName, JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);
        }

        private string GetName(string saveName)
        {
            string currentName = string.IsNullOrEmpty(saveName) ? GetType().ToString() : saveName;
            currentName = EditorEnumExtension.ReplaceWhiteSpaces(currentName);

            return currentName;
        }
    }
}
#endif