using UnityEngine;
using static Spirit604.DotsCity.Simulation.Navigation.PathSelectorBase;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    [CreateAssetMenu(menuName = "Spirit604/RuntimeDemo/Selector Settings")]
    public class SelectorSettings : ScriptableObject
    {
        [SerializeField] private bool idleOnSelect = true;
        [SerializeField] private bool showPathPreview = true;

        [Tooltip("Continue random paths on agent entity if path is missing or complete after unselecting agent entity if user doesn't assign new path")]
        [SerializeField] private bool continueRandomOnUnselect = true;

        [SerializeField] private bool autoStartOnFound = true;

        [SerializeField] private float entitySelectionRadius = 2f;
        [SerializeField] private PathCompleteActionType pathCompleteAction;

        public bool IdleOnSelect { get => idleOnSelect; set => idleOnSelect = value; }
        public bool ShowPathPreview { get => showPathPreview; set => showPathPreview = value; }
        public bool ContinueRandomOnUnselect { get => continueRandomOnUnselect; set => continueRandomOnUnselect = value; }
        public bool AutoStartOnFound { get => autoStartOnFound; set => autoStartOnFound = value; }
        public float EntitySelectionRadius { get => entitySelectionRadius; set => entitySelectionRadius = value; }
        public PathCompleteActionType PathCompleteAction { get => pathCompleteAction; set => pathCompleteAction = value; }
    }
}