using Spirit604.DotsCity.Simulation.Navigation;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class SceneEntityControlService : MonoBehaviour
    {
        [SerializeField] private TileGameUIBase tileGameUIBase;
        [SerializeField] private Button startButtonView;
        [SerializeField] private EntityCustomPathServiceBase trafficPathService;
        [SerializeField] private EntityCustomPathServiceBase pedestrianPathService;
        [SerializeField] private PathSelectorBase trafficPathSelector;
        [SerializeField] private PathSelectorBase pedestrianPathSelector;
        [SerializeField] private SelectorSettings selectorSettings;

        private PathSelectorBase pathSelector;
        private Entity lastEntity;

        private void Start()
        {
            if (!startButtonView)
                return;

            tileGameUIBase.OnGameModeSelected += TileGameUIBase_OnGameModeSelected;
            pedestrianPathService.OnStatusUpdated += PathService_OnStatusUpdated;
            trafficPathService.OnStatusUpdated += PathService_OnStatusUpdated;

            startButtonView.onClick.AddListener(() =>
            {
                pathSelector.StartPath(pathSelector.SelectedEntity);
            });

            Switch(false);
        }

        private void Switch(bool isActivte)
        {
            if (!startButtonView) return;

            startButtonView.gameObject.SetActive(isActivte);
        }

        private void TileGameUIBase_OnGameModeSelected(GameModeType mode)
        {
            Switch(false);

            switch (mode)
            {
                case GameModeType.PedestrianPathMode: pathSelector = pedestrianPathSelector; break;
                case GameModeType.TrafficPathMode: pathSelector = trafficPathSelector; break;
            }
        }

        private void PathService_OnStatusUpdated(Entity entity, PathStatus status)
        {
            if (status == PathStatus.Found)
            {
                lastEntity = entity;

                if (!selectorSettings.AutoStartOnFound)
                {
                    Switch(true);
                }
            }
            else
            {
                if (lastEntity == entity)
                {
                    lastEntity = default;
                    Switch(false);
                }
            }
        }
    }
}
