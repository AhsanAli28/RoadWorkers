#if UNITY_EDITOR
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [CustomEditor(typeof(RSPrefabBinding))]
    public class RSPrefabBindingEditor : Editor
    {
        private const string Key = "RoadSegmentPrefabBinding_Key";
        private const float ButtonSize = 35f;

        private RSPrefabBinding segmentPrefabBinding;

        private void OnEnable()
        {
            segmentPrefabBinding = target as RSPrefabBinding;
            Load();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            segmentPrefabBinding.SelectedSceneObject = (GameObject)EditorGUILayout.ObjectField("Selected Scene Object", segmentPrefabBinding.SelectedSceneObject, typeof(GameObject), true);

            EditorGUI.BeginChangeCheck();

            segmentPrefabBinding.ScriptBindingType = EditorGUILayout.TextField("Script Binding Type", segmentPrefabBinding.ScriptBindingType);

            if (EditorGUI.EndChangeCheck())
            {
                SaveKey();
            }

            GUI.enabled = segmentPrefabBinding.FindAvailable;

            if (GUILayout.Button("Find"))
            {
                segmentPrefabBinding.Find();
            }

            GUI.enabled = true;

            GUI.enabled = segmentPrefabBinding.BindAvailable;

            if (GUILayout.Button("Bind"))
            {
                segmentPrefabBinding.Bind();
            }

            GUI.enabled = true;
        }

        private void OnSceneGUI()
        {
            if (!segmentPrefabBinding.Found) return;

            var objs = segmentPrefabBinding.BindPrefabs;

            foreach (var obj in objs)
            {
                bool added = obj == segmentPrefabBinding.SelectedSceneObject;
                var pos = obj.transform.position;

                if (!added)
                {
                    EditorExtension.DrawButton("+", pos, ButtonSize, () => { segmentPrefabBinding.SelectedSceneObject = obj; });
                }
                else
                {
                    EditorExtension.DrawButton("-", pos, ButtonSize, () => { segmentPrefabBinding.SelectedSceneObject = null; });
                }
            }
        }

        private void SaveKey() => EditorPrefs.SetString(Key, segmentPrefabBinding.ScriptBindingType);

        private void Load() => segmentPrefabBinding.ScriptBindingType = EditorPrefs.GetString(Key, string.Empty);
    }
}
#endif
