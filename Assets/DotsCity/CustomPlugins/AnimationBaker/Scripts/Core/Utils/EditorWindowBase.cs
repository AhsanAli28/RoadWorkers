#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Editor")]

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class EditorWindowBase : EditorWindow
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
            currentName = ReplaceWhiteSpaces(currentName);

            return currentName;
        }

        public static string ReplaceWhiteSpaces(string str)
        {
            str = str.Trim();
            str = str.Replace(" ", "_");

            return str;
        }
    }
}
#endif