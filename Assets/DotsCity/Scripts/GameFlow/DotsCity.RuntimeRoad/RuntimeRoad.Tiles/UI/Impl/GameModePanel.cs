using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class GameModePanel : MonoBehaviour
    {
        [SerializeField] private PageButtonView tileModeButton;
        [SerializeField] private PageButtonView trafficModeButton;
        [SerializeField] private PageButtonView pedestrianModeButton;

        private PageButtonView selected;

        public event Action<GameModeType> OnModeSelected = delegate { };

        private void Start()
        {
            var modeCount = Enum.GetValues(typeof(GameModeType)).Length;

            for (int index = 0; index < modeCount; index++)
            {
                var mode = (GameModeType)index;

                var view = GetView(mode);

                if (view)
                {
                    view.OnClicked += (view) => OnModeSelected(mode);
                }
                else
                {
                    Debug.LogError($"GameModePanel. View for '{mode}' mode doesn't exist");
                }
            }

            SelectMode(GameModeType.TileMode);
        }

        public void SelectMode(GameModeType mode)
        {
            selected?.SwitchSelectionState(false);
            selected = GetView(mode);
            selected.SwitchSelectionState(true);
        }

        private PageButtonView GetView(GameModeType mode)
        {
            switch (mode)
            {
                case GameModeType.TileMode: return tileModeButton;
                case GameModeType.PedestrianPathMode: return pedestrianModeButton;
                case GameModeType.TrafficPathMode: return trafficModeButton;
            }

            return null;
        }
    }
}
