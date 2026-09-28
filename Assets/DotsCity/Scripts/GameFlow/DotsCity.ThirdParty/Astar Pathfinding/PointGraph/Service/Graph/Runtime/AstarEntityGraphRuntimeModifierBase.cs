using Spirit604.DotsCity.RuntimeRoad;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public abstract class AstarEntityGraphRuntimeModifierBase : AstarEntityGraphBase
    {
        [SerializeField] protected RuntimeRoadManager runtimeRoadManager;

        private void Awake()
        {
            runtimeRoadManager.OnSegmentAdded += RuntimeRoadManager_OnSegmentAdded;
            runtimeRoadManager.OnSegmentRemoved += RuntimeRoadManager_OnSegmentRemoved;
        }

        protected abstract void AddSegment(RuntimeSegment segment);

        protected abstract void RemoveSegment(RuntimeSegment segment);

#if UNITY_EDITOR
        private void Reset()
        {
            if (!runtimeRoadManager)
            {
                runtimeRoadManager = Extensions.ObjectUtils.FindObjectOfType<RuntimeRoadManager>();
                Extensions.EditorSaver.SetObjectDirty(this);
            }
        }
#endif

        private void RuntimeRoadManager_OnSegmentAdded(RuntimeSegment segment)
        {
            AddSegment(segment);
        }

        private void RuntimeRoadManager_OnSegmentRemoved(RuntimeSegment segment)
        {
            RemoveSegment(segment);
        }
    }
}
