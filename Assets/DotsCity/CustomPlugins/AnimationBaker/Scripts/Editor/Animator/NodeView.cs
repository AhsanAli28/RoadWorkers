using System.Collections.Generic;
using UnityEngine;
using Spirit604.AnimationBaker.Utils;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;

namespace Spirit604.AnimationBaker.EditorInternal
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public string GUID;

        public Vector2 position;
        public bool EntryPoint = false;
        public Port entryPort;
        public List<Port> ports = new List<Port>();
        public List<NodeView> transitions = new List<NodeView>();
        public NodeData RelatedNode;
        public bool transitionNode;

        public override void OnSelected()
        {
            base.OnSelected();
        }

        protected override void OnPortRemoved(Port port)
        {
            base.OnPortRemoved(port);
        }

        public void TryToRemovePort(Port port)
        {
            ports.TryToRemove(port);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
        }

        public void SetPosition(Vector2 newPos)
        {
            var rect = GetPosition();

            rect.position = newPos;
            SetPosition(rect);
        }

        public void LoadPosition(Vector2 spawnPosition)
        {
            SetPosition(spawnPosition);
        }
    }
}
#endif