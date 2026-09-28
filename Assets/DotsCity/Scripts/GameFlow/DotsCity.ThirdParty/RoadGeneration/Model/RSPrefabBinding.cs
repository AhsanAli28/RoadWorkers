using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class RSPrefabBinding : MonoBehaviour
    {
        [field: SerializeField] public GameObject BindSourcePrefab { get; private set; }
        [field: SerializeField] public RoadSegmentCreator RoadSegmentCreator { get; private set; }
        [field: SerializeField] public bool LockAutoRecreation { get; set; }

        public GameObject SelectedSceneObject { get; set; }
        public string ScriptBindingType { get; set; }

        public GameObject[] BindPrefabs { get; private set; }
        public bool FindAvailable => !string.IsNullOrEmpty(ScriptBindingType);
        public bool Found => BindPrefabs?.Length > 0;
        public bool BindAvailable => SelectedSceneObject != null;

        public void Find()
        {
            var type = TypeHelper.ByName(ScriptBindingType);

            if (type == default)
            {
                UnityEngine.Debug.Log($"RoadSegmentPrefabBinding. Type '{ScriptBindingType}' not found.");
                return;
            }

            BindPrefabs = ObjectUtils.FindObjectsOfType(type).Select(a => (a as Component).gameObject).ToArray();
        }

        public void Bind()
        {
#if UNITY_EDITOR

            RoadSegmentCreator.ChangeOffsetParentRelative(SelectedSceneObject.transform.position, SelectedSceneObject.transform.rotation, true);

            if (!BindSourcePrefab)
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(SelectedSceneObject.gameObject);

                if (prefab == null)
                {
                    prefab = PrefabExtension.FindPrefabByName(SelectedSceneObject.name);
                }

                BindSourcePrefab = prefab;

                if (!BindSourcePrefab)
                {
                    UnityEngine.Debug.Log($"RoadSegmentPrefabBinding. Source prefab by name '{SelectedSceneObject.name}' not found.");
                }
            }

            EditorSaver.SetObjectDirty(this);
#endif
        }

        private void Reset()
        {
            RoadSegmentCreator = GetComponent<RoadSegmentCreator>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
