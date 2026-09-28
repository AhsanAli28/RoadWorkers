#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spirit604.AnimationBaker.EditorInternal
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private EditorWindow editorWindow;
        private CrowdGPUAnimatorGraphView pedestrianBakedAnimatorGraphView;

        public void Initialize(EditorWindow editorWindow, CrowdGPUAnimatorGraphView pedestrianBakedAnimatorGraphView)
        {
            this.editorWindow = editorWindow;
            this.pedestrianBakedAnimatorGraphView = pedestrianBakedAnimatorGraphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Nods"), level :0),
                new SearchTreeEntry(new GUIContent("Animation Node"))
                {
                    userData = "AnimationNode",
                    level = 1
                },
                new SearchTreeEntry(new GUIContent("Transition Node"))
                {
                    userData = "TransitionNode",
                    level = 1
                }
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var worldPosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent,
                context.screenMousePosition - editorWindow.position.position);

            var position = pedestrianBakedAnimatorGraphView.WorldToLocal(worldPosition);

            switch (SearchTreeEntry.userData)
            {
                case "AnimationNode":
                    {
                        pedestrianBakedAnimatorGraphView.CreateAnimationNode(position);
                        return true;
                    }
                case "TransitionNode":
                    {
                        pedestrianBakedAnimatorGraphView.CreateTransitionNode(position);
                        return true;
                    }
            }

            return false;
        }
    }
}
#endif