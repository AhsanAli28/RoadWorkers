using Spirit604.AnimationBaker.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [System.Serializable]
    public class TransitionLayerData
    {
        public NodeData EntryNode;
        public string ActivateTrigger;

        [HideInInspector] public Vector2 EntryNodePosition;

        [ReadOnly]
        [SerializeField] private int activateTriggerHash;

        [SerializeField] private List<NodeData> allNodes = new List<NodeData>();

        public int ActivateTriggerHash
        {
            get => activateTriggerHash;
            private set
            {
                activateTriggerHash = value;
            }
        }

        public List<NodeData> AllNodes => allNodes;

        public void SetTriggerName(string triggerName)
        {
            this.ActivateTrigger = triggerName;

            activateTriggerHash = Animator.StringToHash(ActivateTrigger);
        }

        public bool TryToAddNode(NodeData nodeData)
        {
            return allNodes.TryToAdd(nodeData);
        }

        public bool TryToRemoveNode(NodeData nodeData)
        {
            var removed = allNodes.TryToRemove(nodeData);

            if (removed)
            {
#if UNITY_EDITOR
                AssetDatabase.RemoveObjectFromAsset(nodeData);
                Object.DestroyImmediate(nodeData, true);
                AssetDatabase.SaveAssets();
#endif
            }

            return removed;
        }

        public bool TryToRemoveNode(string guid)
        {
            var node = allNodes.Where(a => a.Guid == guid).FirstOrDefault();

            if (node != null)
            {
                return TryToRemoveNode(node);
            }

            return false;
        }
    }
}
