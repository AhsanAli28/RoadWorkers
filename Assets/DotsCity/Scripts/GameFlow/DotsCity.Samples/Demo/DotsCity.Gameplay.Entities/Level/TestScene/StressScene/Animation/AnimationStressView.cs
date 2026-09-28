using Spirit604.MainMenu.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class AnimationStressView : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private ConfigEnumValueUIItem configEnumValueItem;
        [SerializeField] private ConfigSliderValueUIItem configSliderValueItem;
        [SerializeField] private ConfigBoolValueUIItem configBoolValueUIItem;
        [SerializeField] private Button updateButton;
        [SerializeField] private Button exitButton;

        public event Action<PedestrianAnimationTestSpawner.NpcRigType> OnNpcTypeChanged = delegate { };
        public event Action<int> OnCountChanged = delegate { };
        public event Action<bool> OnRandomizeChanged = delegate { };
        public event Action OnUpdateClick = delegate { };
        public event Action OnExitClick = delegate { };

        private void Awake()
        {
            configEnumValueItem.Initialize("NpcRigType", default, typeof(PedestrianAnimationTestSpawner.NpcRigType), (val) => OnNpcTypeChanged((PedestrianAnimationTestSpawner.NpcRigType)val));
            configSliderValueItem.Initialize("Count", 0, (val) => OnCountChanged(Mathf.RoundToInt(val)), true);
            configBoolValueUIItem.Initialize("Randomize skin", false, (val) => OnRandomizeChanged(val));
            updateButton.onClick.AddListener(() => OnUpdateClick());
            exitButton.onClick.AddListener(() => OnExitClick());
            SetInteractableState(false);
        }

        public void Initialize(PedestrianAnimationTestSpawner.NpcRigType npcRigType)
        {
            configEnumValueItem.SetValue(npcRigType);
        }

        public void Initialize(int count, int maxValue)
        {
            configSliderValueItem.Initialize(count, maxValue);
        }

        public void Initialize(bool randomize)
        {
            configBoolValueUIItem.Initialize(randomize);
        }

        public void SetInteractableState(bool isInteractable)
        {
            updateButton.interactable = isInteractable;
        }
        public void SwitchCanvasState(bool enabled)
        {
            mainCanvas.enabled = enabled;
        }
    }
}