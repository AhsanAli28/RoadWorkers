using Spirit604.AnimationBaker.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CreateAssetMenu(menuName = "Spirit604/Animation Baker/Animator Container")]
    public class AnimatorDataContainer : ScriptableObject
    {
        [SerializeField] private List<TransitionLayerData> layers = new List<TransitionLayerData>();
        [SerializeField] private int selectedLayerIndex = -1;

        public int SelectedLayerIndex
        {
            get => selectedLayerIndex;
            set
            {
                if (selectedLayerIndex != value)
                {
                    selectedLayerIndex = value;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        public int LayerCount => layers.Count;

        public void AddNode(NodeData nodeData, string guid)
        {
            var layerData = GetSelectedLayerData();
            nodeData.Initialize(this, guid);

            if (layerData.TryToAddNode(nodeData))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void TryToRemoveNodeFromAll(NodeData nodeData)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                var removed = TryToRemoveNode(nodeData, i);

                if (removed)
                {
                    break;
                }
            }
        }

        public bool TryToRemoveNode(NodeData nodeData) => TryToRemoveNode(nodeData, selectedLayerIndex);

        public bool TryToRemoveNode(NodeData nodeData, int layerIndex)
        {
            var layerData = GetLayerData(layerIndex);

            if (layerData != null && layerData.TryToRemoveNode(nodeData))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif

                return true;
            }

            return false;
        }

        public bool TryToRemoveNode(string guid) => TryToRemoveNode(guid, selectedLayerIndex);

        public bool TryToRemoveNode(string guid, int layerIndex)
        {
            var layerData = GetLayerData(layerIndex);

            if (layerData != null && layerData.TryToRemoveNode(guid))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
                return true;
            }

            return false;
        }

        public NodeData GenerateAnimationNode(string name, string guid)
        {
            var node = ScriptableObject.CreateInstance<AnimationNodeData>();
            node.name = name;
            AddNode(node, guid);
            AddNodeToAssets(node);

            return node;
        }

        public NodeData GenerateTransitionNode(string name, string guid)
        {
            var node = ScriptableObject.CreateInstance<TransitionNodeData>();
            node.name = name;
            AddNode(node, guid);
            AddNodeToAssets(node);

            return node;
        }

        public void AddLayer()
        {
            layers.Add(new TransitionLayerData());

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool RemoveSelectedLayer() => RemoveLayer(SelectedLayerIndex);

        public bool RemoveLayer(int index)
        {
            if (index >= 0 && layers.Count > index)
            {
                layers.RemoveAt(index);

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif

                return true;
            }

            return false;
        }

        public TransitionLayerData GetSelectedLayerData() => GetLayerData(selectedLayerIndex);

        public TransitionLayerData GetLayerData(int index)
        {
            if (index >= 0 && layers.Count > index)
            {
                return layers[index];
            }

            return null;
        }

        public void Connect(NodeData sourceNode, NodeData targetNode)
        {
            if (sourceNode.ConnectedNodes.TryToAdd(targetNode))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void Disconnect(NodeData sourceNode, NodeData targetNode)
        {
            if (sourceNode.ConnectedNodes.TryToRemove(targetNode))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public NodeData GetEntryNode()
        {
            var layerData = GetLayerData(selectedLayerIndex);

            if (layerData != null)
            {
                return layerData.EntryNode;
            }

            return null;
        }

        public void SetEntryPoint(NodeData relatedNode) => SetEntryPoint(relatedNode, selectedLayerIndex);

        public void SetEntryPoint(NodeData relatedNode, int layerIndex)
        {
            var layerData = GetLayerData(layerIndex);

            if (layerData != null)
            {
                layerData.EntryNode = relatedNode;

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void SetEntryPointPosition(Vector2 position)
        {
            var layerData = GetLayerData(selectedLayerIndex);

            if (layerData != null && layerData.EntryNodePosition != position)
            {
                layerData.EntryNodePosition = position;

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public Vector2 GetEntryPointPosition()
        {
            var layerData = GetLayerData(selectedLayerIndex);

            if (layerData != null)
            {
                return layerData.EntryNodePosition;
            }

            return Vector2.zero;
        }

        private void AddNodeToAssets(NodeData node)
        {
#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(node, this);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Clear All")]
        private void ClearAll()
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Clear All");
#endif

            for (int i = 0; i < layers.Count; i++)
            {
                for (int j = 0; j < layers[i].AllNodes.Count; j++)
                {
                    if (layers[i].AllNodes[j] != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(layers[i].AllNodes[j], true);
#endif
                    }
                }
            }

            layers.Clear();
            selectedLayerIndex = 0;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}
