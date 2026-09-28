using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Spirit604.AnimationBaker.EditorInternal
{
#if UNITY_EDITOR
    public class CrowdGPUAnimatorWindow : EditorWindow
    {
        private CrowdGPUAnimatorGraphView animatorView;
        private AnimatorDataContainer animatorContainer;
        private Toolbar toolbar;

        public static CrowdGPUAnimatorWindow OpenAnimatorWindow()
        {
            var window = GetWindow<CrowdGPUAnimatorWindow>();
            window.titleContent = new GUIContent("Animator");
            return window;
        }

        private void OnEnable()
        {
            ConstructGraphView();
        }

        private void ConstructGraphView()
        {
            animatorView = new CrowdGPUAnimatorGraphView()
            {
                name = "AnimatorView"
            };

            animatorView.StretchToParentSize();
            rootVisualElement.Add(animatorView);
        }

        private void UpdateGraph()
        {
            if (toolbar != null)
            {
                rootVisualElement.Remove(toolbar);
            }

            toolbar = new Toolbar();

            var label = new Label($"Selected Transition {animatorContainer.SelectedLayerIndex}");
            toolbar.Add(label);

            for (int i = 0; i < animatorContainer.LayerCount; i++)
            {
                var index = i;

                var selectLayerButton = new Button(() =>
                {
                    animatorContainer.SelectedLayerIndex = index;
                    UpdateGraph();
                });

                if (index == animatorContainer.SelectedLayerIndex)
                {
                    selectLayerButton.Focus();
                }

                selectLayerButton.text = index.ToString();

                toolbar.Add(selectLayerButton);
            }

            if (animatorContainer.LayerCount > 1)
            {
                var removeButton = new Button(() =>
                {
                    animatorContainer.RemoveSelectedLayer();
                    animatorContainer.SelectedLayerIndex = Mathf.Clamp(animatorContainer.SelectedLayerIndex, 0, animatorContainer.LayerCount - 1);
                    UpdateGraph();
                });

                removeButton.text = "-";

                toolbar.Add(removeButton);
            }

            var addButton = new Button(() =>
            {
                animatorContainer.AddLayer();
                UpdateGraph();
            });

            addButton.text = "+";

            toolbar.Add(addButton);

            animatorView.UpdateGraph();

            rootVisualElement.Add(toolbar);
        }

        public void Initialize(AnimatorDataContainer animatorContainer, AnimationCollectionContainer animationCollectionContainer)
        {
            this.animatorContainer = animatorContainer;
            animatorView.Initialize(this, animatorContainer, animationCollectionContainer);
            UpdateGraph();
        }
    }
#endif
}
