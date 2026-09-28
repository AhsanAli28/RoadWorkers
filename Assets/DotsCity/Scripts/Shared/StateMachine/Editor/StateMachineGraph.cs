#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Spirit604.StateMachine.InternalEditor
{
    public class StateMachineGraph : EditorWindow
    {
        private StateMachineGraphView stateMachineGraphView;

        public static void OpenStateMachineGraphWindow()
        {
            var window = GetWindow<StateMachineGraph>();
            window.titleContent = new GUIContent("StateMachine Graph");
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var resetButton = new Button(
            () =>
            {
                Reset();
            });

            toolbar.Add(resetButton);
        }

        private void Reset()
        {
        }

        private void ConstructGraphView()
        {
            stateMachineGraphView = new StateMachineGraphView()
            {
                name = "StateMachineGraphView"
            };

            stateMachineGraphView.StretchToParentSize();
            rootVisualElement.Add(stateMachineGraphView);
        }
    }
}
#endif