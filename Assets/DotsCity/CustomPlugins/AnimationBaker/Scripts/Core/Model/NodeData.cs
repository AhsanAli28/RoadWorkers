using Spirit604.AnimationBaker.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class NodeData : ScriptableObject
    {
        [HideInInspector]
        [SerializeField] private AnimatorDataContainer animContainer;

        [HideInInspector]
        [SerializeField] private Vector2 animatorNodePosition;

        [ReadOnly]
        [SerializeField] private string guid;

        [SerializeField] private List<NodeData> connectedNodes = new List<NodeData>();

        public AnimatorDataContainer AnimContainer => animContainer;
        public Vector2 AnimatorNodePosition { get => animatorNodePosition; set => animatorNodePosition = value; }
        public string Guid => guid;
        public List<NodeData> ConnectedNodes => connectedNodes;

        public void OnDestroy()
        {
            animContainer?.TryToRemoveNodeFromAll(this);
        }

        public void Initialize(AnimatorDataContainer animContainer, string guid)
        {
            this.animContainer = animContainer;
            this.guid = guid;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void TryToAddNode(NodeData nodeData)
        {
            connectedNodes.TryToAdd(nodeData);
        }

        public void TryToRemoveNode(NodeData nodeData)
        {
            connectedNodes.TryToRemove(nodeData);
        }

        public void SetAssetName(string name)
        {
            if (this.name != name)
            {
                this.name = name;

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Delete")]
        public void Delete()
        {
            DestroyImmediate(this, true);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
