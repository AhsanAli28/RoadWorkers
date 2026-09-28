using Pathfinding;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public abstract class AstarEntityGraphBase : MonoBehaviour
    {
        [SerializeField] protected int localGraphIndex;

        private PointGraph pointGraph;

        protected Dictionary<Entity, CustomEntityNode> entityBinding = new Dictionary<Entity, CustomEntityNode>();
        protected Dictionary<CustomEntityNode, Entity> entityBindingInversed = new Dictionary<CustomEntityNode, Entity>();

        public PointGraph PointGraph { get => pointGraph; set => pointGraph = value; }

        protected virtual void Start()
        {
            if (AstarPath.active == null)
            {
                Debug.LogError($"{GetName()}. AstarPath not found");
                return;
            }

            if (AstarPath.active.graphs.Length > localGraphIndex)
            {
                var graph = AstarPath.active.graphs[localGraphIndex];

                if (graph is PointGraph)
                {
                    PointGraph = AstarPath.active.graphs[localGraphIndex] as PointGraph;
                }
                else
                {
                    Debug.LogError($"{GetName()}. Graph with index {localGraphIndex} is not PointGraph");
                }
            }
            else
            {
                Debug.LogError($"{GetName()}. Graph with index {localGraphIndex} not found");
            }
        }


        public Entity GetEntity(GraphNode customEntityNode)
        {
            return GetEntity(customEntityNode as CustomEntityNode);
        }

        public Entity GetEntity(CustomEntityNode customEntityNode)
        {
            if (entityBindingInversed.TryGetValue(customEntityNode, out var entity))
                return entity;

            return customEntityNode.Entity;
        }

        protected Int3 PosToInt(Vector3 pos)
        {
            return (Int3)(pos);
        }

        protected uint GetCost(Vector3 pos1, Vector3 pos2)
        {
            return (uint)(Vector3.SqrMagnitude(pos2 - pos1));
        }

        private string GetName()
        {
            return $"{this.GetType().Name}";
        }
    }
}
