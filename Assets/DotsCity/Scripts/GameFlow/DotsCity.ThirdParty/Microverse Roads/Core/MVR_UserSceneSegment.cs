using JBooth.MicroVerseCore;
using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.MVR
{
    /// <summary>
    /// This component prevents the road segment from being cleaned up during regeneration.
    /// </summary>
    [DisallowMultipleComponent]
    public class MVR_UserSceneSegment : MonoBehaviour
    {
        [OnValueChanged(nameof(UpdateBinding))]
        [SerializeField] private Intersection relatedIntersection;

        [HideInInspector]
        [SerializeField] private string bindingName;

        [HideInInspector]
        [SerializeField] private bool hasBinding;

        public Intersection RelatedIntersection
        {
            get
            {
                if (hasBinding && relatedIntersection == null)
                {
                    Reset();
                }

                return relatedIntersection;
            }
        }

        private bool HasIntersetion => relatedIntersection != null;

        private void UpdateBinding()
        {
            if (relatedIntersection != null)
            {
                bindingName = relatedIntersection.name;
                hasBinding = true;
                Extensions.EditorSaver.SetObjectDirty(this);
            }
        }

        [EnableIf(nameof(HasIntersetion))]
        [Button]
        private void SyncPosition()
        {
            var creator = GetComponent<RoadSegmentCreator>();
            creator.ChangeOffsetParentRelative(relatedIntersection.transform.position, relatedIntersection.transform.rotation);
            Debug.Log("Position & rotation synced with related intersection");
        }

        private void ResetInternal(bool auto = false)
        {
#if UNITY_EDITOR

            if (relatedIntersection != null) return;

            var intersections = Extensions.ObjectUtils.FindObjectsOfType<Intersection>();

            var position = transform.position;

            if (string.IsNullOrEmpty(bindingName))
                relatedIntersection = intersections.OrderBy(item => (item.transform.position - position).sqrMagnitude).FirstOrDefault();
            else
                relatedIntersection = intersections.Where(a => a.name.Contains(bindingName)).OrderBy(item => (item.transform.position - position).sqrMagnitude).FirstOrDefault();

            if (relatedIntersection != null)
            {
                var distance = Vector3.Distance(relatedIntersection.transform.position, position);

                if (distance > 1 && !auto)
                {
                    Debug.LogError($"Make sure the position of the segment {name} and the intersection {relatedIntersection.name} are synchronized.");
                    relatedIntersection = null;
                }

                UpdateBinding();
            }
#endif
        }

        [Button]
        private void Reset()
        {
            ResetInternal(true);
        }
    }
}
