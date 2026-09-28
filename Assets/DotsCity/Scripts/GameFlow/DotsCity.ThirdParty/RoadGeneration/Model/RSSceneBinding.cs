using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class RSSceneBinding : MonoBehaviourBase
    {
        [field: SerializeField] public RoadSegmentCreator RoadSegmentCreator { get; private set; }
        [field: SerializeField] public GameObject SelectedSceneObject { get; set; }

        [Tooltip("The next time the generator is regenerated, the current segment will not be recreated and all custom settings for it will be saved")]
        [SerializeField] private bool lockAutoRecreation;

        [ReadOnly]
        [SerializeField] private RSGenType rsGenType;

        [ReadOnly]
        [SerializeField] private string bindingName;

        [ReadOnly]
        [SerializeField] private int bindingHash;

        public bool LockAutoRecreation { get => lockAutoRecreation; set => lockAutoRecreation = value; }

        public RSGenType RSGenType { get => rsGenType; set => rsGenType = value; }

        public string BindingName { get => bindingName; set => bindingName = value; }

        public int BindingHash { get => bindingHash; set => bindingHash = value; }

        public bool HasSceneObject => SelectedSceneObject;

        private bool SplineRoad => rsGenType == RSGenType.SplineRoad;

        public void SetBinding(GameObject sceneObject, int hash)
        {
            SelectedSceneObject = sceneObject;
            bindingHash = hash;
            Rebind();
            RoadSegmentCreator = GetComponent<RoadSegmentCreator>();
            EditorSaver.SetObjectDirty(this);
        }

        public bool RestoreSceneObject(Dictionary<int, IRoadObject> bindData, bool allowByName = false)
        {
            if (bindData.ContainsKey(bindingHash))
            {
                var data = bindData[bindingHash];
                SelectedSceneObject = data.SceneObject;
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            if (allowByName)
            {
                return RestoreSceneObjectByName();
            }

            return false;
        }

        public bool RestoreSceneObjectByName()
        {
            if (!string.IsNullOrEmpty(bindingName))
            {
                var sceneObject = GameObject.Find(bindingName);

                if (sceneObject != null)
                {
                    SelectedSceneObject = sceneObject;
                    EditorSaver.SetObjectDirty(this);
                    return true;
                }
            }

            return false;
        }

        [EnableIf(nameof(SplineRoad))]
        [Button]
        public void UpdateSegment()
        {
            var rsGenerator = ObjectUtils.FindObjectOfType<RSGeneratorBase>();

            if (rsGenerator)
            {
#if UNITY_EDITOR
                rsGenerator.UpdateSegment(this);
#endif
            }
        }

        private void Rebind()
        {
            BindingName = SelectedSceneObject.name;
        }
    }
}
