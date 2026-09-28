using UnityEditor;
using UnityEngine;

namespace Spirit604.StateMachine.InternalEditor
{
    [CustomEditor(typeof(StateMachine))]
    public class StateMachineEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var stateMachine = target as StateMachine;
            base.OnInspectorGUI();

            if (GUILayout.Button("Visualize"))
            {
                var hasInitialState = stateMachine.CheckForInitialStateExist();

                if (hasInitialState)
                {
                    StateMachineGraphView.TargetStateMachine = stateMachine;
                    StateMachineGraph.OpenStateMachineGraphWindow();
                }
            }
        }
    }
}