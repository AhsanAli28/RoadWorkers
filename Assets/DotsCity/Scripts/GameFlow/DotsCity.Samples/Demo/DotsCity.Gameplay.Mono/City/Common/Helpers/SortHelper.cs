using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Utils
{
    public class SortHelper : MonoBehaviourBase
    {
        [SerializeField] private string matchText;
        [SerializeField] private bool worldSearch = true;
        [SerializeField] private bool parentShouldBeNull = true;

        [HideIf(nameof(worldSearch))]
        [SerializeField] private Transform searchInParent;
        [SerializeField] private Transform moveToParent;

        [Button]
        public void Launch()
        {
            if (string.IsNullOrWhiteSpace(matchText))
            {
                return;
            }

            if (!worldSearch && searchInParent == null)
            {
                return;
            }

            if (!moveToParent)
            {
                moveToParent = transform;
            }

            List<GameObject> objects = null;

            if (worldSearch)
            {
                if (parentShouldBeNull)
                {
                    objects = ObjectUtils.FindObjectsOfType<GameObject>(true).Where(item => item.name.Contains(matchText) && item != moveToParent.gameObject && item.transform.parent == null).ToList();
                }
                else
                {
                    objects = ObjectUtils.FindObjectsOfType<GameObject>(true).Where(item => item.name.Contains(matchText) && item != moveToParent.gameObject).ToList();
                }
            }
            else
            {
                if (parentShouldBeNull)
                {
                    objects = searchInParent.GetComponentsInChildren<GameObject>().Where(item => item.name.Contains(matchText) && item != moveToParent.gameObject && item.transform.parent == null).ToList();
                }
                else
                {
                    objects = searchInParent.GetComponentsInChildren<GameObject>().Where(item => item.name.Contains(matchText) && item != moveToParent.gameObject).ToList();
                }
            }

#if UNITY_EDITOR
            for (int i = 0; i < objects?.Count; i++)
            {
                bool canReparent = PrefabUtility.GetCorrespondingObjectFromSource(objects[i]) == null || (PrefabUtility.IsAnyPrefabInstanceRoot(objects[i]));

                if (canReparent)
                {
                    objects[i].transform.parent = moveToParent;
                }
            }
#endif

        }
    }
}
