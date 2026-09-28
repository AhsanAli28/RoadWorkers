using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Spirit604.StateMachine.Utils;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;

namespace Spirit604.StateMachine.InternalEditor
{
    public class StateNode : UnityEditor.Experimental.GraphView.Node
    {
        public string GUID;

        //public string Title;

        public Vector2 position;
        public bool EntryPoint = false;
        public Port entryPort;
        public StateBase state;
        public List<Port> ports = new List<Port>();
        public List<StateNode> transitions = new List<StateNode>();

        public override void OnSelected()
        {
            base.OnSelected();

            if (state != null)
            {
                Selection.activeObject = state;
            }
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