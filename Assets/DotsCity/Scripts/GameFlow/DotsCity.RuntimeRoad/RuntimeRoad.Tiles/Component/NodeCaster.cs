using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class NodeCaster : MonoBehaviour
    {
        [SerializeField] private PedestrianNode pedestrianNode;

        public PedestrianNode OwnerNode { get => pedestrianNode; set => pedestrianNode = value; }

        public void Cast(List<PedestrianNode> list)
        {
            pedestrianNode.ConnectDirections(list, false);
        }

        public void BindOwner()
        {
            if (pedestrianNode == null)
            {
                pedestrianNode = GetComponent<PedestrianNode>();
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void Reset()
        {
            BindOwner();
        }
    }
}
