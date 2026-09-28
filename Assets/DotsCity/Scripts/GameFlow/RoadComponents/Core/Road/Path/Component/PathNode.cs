using Spirit604.Attributes;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class PathNode : MonoBehaviourBase
    {
        [SerializeField][Range(0f, 200f)] private float speedLimit;
        [SerializeField] private bool backwardDirection;
        [SerializeField] private bool customGroup;

        [ShowIf(nameof(customGroup))]
        [SerializeField] private TrafficGroupMask trafficGroupMask = new TrafficGroupMask();

        [SerializeField] private bool spawnNode;

        [ReadOnly]
        [SerializeField] private int uniqueId;

        public Color PathCustomColor { get; set; }
        public bool HasPathCustomColor { get; set; }
        public Color SelectCustomColor { get; set; }
        public bool HasSelectCustomColor { get; set; }

        public float SpeedLimit { get => speedLimit; set => speedLimit = value; }
        public float SpeedLimitMs => speedLimit / ProjectConstants.KmhToMs_RATE;
        public bool BackwardDirection { get => backwardDirection; set => backwardDirection = value; }
        public bool CustomGroup { get => customGroup; set => customGroup = value; }

        public bool SpawnNode
        {
            get => spawnNode;
            set
            {
                if (spawnNode != value)
                {
                    spawnNode = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public TrafficGroupType CustomGroupType => trafficGroupMask.GetValue();
        public TrafficGroupMask TrafficGroupMask { get => trafficGroupMask; set => trafficGroupMask = value; }
        public int UniqueID => uniqueId;

        public void GenerateId()
        {
            if (!spawnNode)
                return;

            if (uniqueId == 0)
            {
                uniqueId = UniqueIdUtils.GetUniqueID(this, transform.position);
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void Copy(PathNode sourcePathNode)
        {
            this.speedLimit = sourcePathNode.SpeedLimit;
            this.backwardDirection = sourcePathNode.BackwardDirection;
            this.TrafficGroupMask = sourcePathNode.TrafficGroupMask.GetClone();
            this.spawnNode = sourcePathNode.spawnNode;
            this.uniqueId = sourcePathNode.uniqueId;
            EditorSaver.SetObjectDirty(this);
        }
    }
}