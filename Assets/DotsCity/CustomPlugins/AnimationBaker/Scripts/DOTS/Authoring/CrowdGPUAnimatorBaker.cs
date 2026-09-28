using System.Collections.Generic;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public class CrowdGPUAnimatorBaker : Baker<CrowdGPUAnimatorAuthoring>
    {
        private Dictionary<NodeData, Entity> createdEntitiesCache = new Dictionary<NodeData, Entity>();

        public override void Bake(CrowdGPUAnimatorAuthoring authoring)
        {
            DependsOn(authoring.AnimatorContainer);

            if (!authoring.AnimatorContainer)
            {
                UnityEngine.Debug.LogError("CrowdGPUAnimatorBaker. AnimatorContainer is null");
            }

            for (int i = 0; i < authoring.AnimatorContainer?.LayerCount; i++)
            {
                var layerData = authoring.AnimatorContainer.GetLayerData(i);

                if (layerData != null)
                {
                    if (layerData.EntryNode != null)
                    {
                        var entryEntity = AddNode(layerData.EntryNode);

                        AddComponent(entryEntity, new AnimNodeEntryEntityData()
                        {
                            ActivateTriggerHash = layerData.ActivateTriggerHash,
                        });
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"PedestrianBakedAnimatorBaker layer index {i}. Entry node is null");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"PedestrianBakedAnimatorBaker layer index {i} is null");
                }
            }

            createdEntitiesCache.Clear();
        }

        private Entity AddNode(NodeData node)
        {
            if (node == null)
            {
                return Entity.Null;
            }

            if (createdEntitiesCache.ContainsKey(node))
            {
                return createdEntitiesCache[node];
            }

            var entity = CreateAdditionalEntity(TransformUsageFlags.None);

            createdEntitiesCache.Add(node, entity);

            if (node is AnimationNodeData)
            {
                var animNode = node as AnimationNodeData;

                AddComponent(entity, new AnimNodeEntityData()
                {
                    AnimHash = animNode.AnimHash,
                    UniqueAnimation = animNode.UniqueAnimation,
                });
            }
            else if (node is TransitionNodeData)
            {
                var transitionNode = node as TransitionNodeData;

                AddComponent(entity, new TransitionNodeEntityData()
                {
                    TransitionDuration = transitionNode.TransitionDuration,
                    AnimationTransitionType = transitionNode.AnimationTransitionType,
                });

                return entity;
            }

            var animBuffer = AddBuffer<AnimConnectedNode>(entity);

            for (int i = 0; i < node.ConnectedNodes?.Count; i++)
            {
                var connectedNode = node.ConnectedNodes[i];

                if (!connectedNode)
                {
                    UnityEngine.Debug.Log($"CrowdGPUAnimatorBaker. Node {node.name} Guid {node.Guid} connected node index {i} is null");
                    continue;
                }

                AnimationNodeData animationNode = null;
                TransitionNodeData transitionNode = null;

                if (connectedNode is AnimationNodeData)
                {
                    animationNode = connectedNode as AnimationNodeData;
                }

                if (connectedNode is TransitionNodeData)
                {
                    transitionNode = connectedNode as TransitionNodeData;
                }

                if (transitionNode && !animationNode)
                {
                    if (transitionNode.ConnectedNodes?.Count > 0)
                    {
                        animationNode = transitionNode.ConnectedNodes[0] as AnimationNodeData;
                    }
                }

                var transitionEntity = Entity.Null;
                var connectedAnimationEntity = Entity.Null;

                if (transitionNode)
                {
                    transitionEntity = AddNode(transitionNode);
                }

                if (animationNode)
                {
                    connectedAnimationEntity = AddNode(animationNode);
                }

                if (connectedAnimationEntity != Entity.Null)
                {
                    animBuffer.Add(new AnimConnectedNode()
                    {
                        NextState = connectedAnimationEntity,
                        TransitionState = transitionEntity
                    });
                }
            }

            return entity;
        }
    }
}
