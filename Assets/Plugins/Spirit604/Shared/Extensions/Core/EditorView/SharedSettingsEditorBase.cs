#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public abstract class SharedSettingsEditorBase<T> : Editor
    {
        protected T SharedSettings { get; private set; }

        protected abstract string SaveKey { get; }

        protected virtual void OnEnable()
        {
            LoadPrefs();
        }

        protected virtual void OnDisable()
        {
            SavePrefs();
        }

        protected abstract T GetDefaultSettings();

        private void SavePrefs()
        {
            EditorPrefs.SetString(SaveKey, JsonUtility.ToJson(SharedSettings));
        }

        private void LoadPrefs()
        {
            if (EditorPrefs.HasKey(SaveKey))
            {
                try
                {
                    SharedSettings = JsonUtility.FromJson<T>(EditorPrefs.GetString(SaveKey));
                }
                catch { }
            }

            if (SharedSettings == null)
            {
                SharedSettings = GetDefaultSettings();
            }
        }
    }
}
#endif
