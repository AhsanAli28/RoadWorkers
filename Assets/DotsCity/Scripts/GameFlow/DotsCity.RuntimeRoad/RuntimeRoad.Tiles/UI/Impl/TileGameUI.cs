using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGameUI : TileGameUIBase
    {
        [SerializeField] private Canvas gameplayCanvas;
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private TileGridView tileGridView;
        [SerializeField] private ModePanel modePanel;
        [SerializeField] private PagePanel pagePanel;
        [SerializeField] private GameModePanel gameModePanel;

        protected virtual void Awake()
        {
            pagePanel.OnPageClicked += OnPageSelectedInternal;
            gameModePanel.OnModeSelected += OnGameModeSelectedInternal;
            tileGridView.OnTileSelected += OnTileSelectedInternal;
            tileGridView.OnVariantSelected += OnVariantSelectedInternal;
            tileGridView.OnVariantPopupOpened += OnPopupOpenedInternal;
            modePanel.OnModeClicked += OnModeSelectedInternal;
        }

        public override void InitPages(int pageCount)
        {
            pagePanel.Init(pageCount);
        }

        public override void Populate(IList<PrefabData> tilePrefabs)
        {
            tileGridView.Populate(tilePrefabs);
        }

        public override void SetVariant(PrefabType prefabType, int variant)
        {
            tileGridView.SetVariant(prefabType, variant);
        }

        public override void SetMode(GameModeType mode)
        {
            switch (mode)
            {
                case GameModeType.TileMode:
                    {
                        gameplayCanvas.enabled = true;
                        controlCanvas.enabled = false;
                        break;
                    }
                case GameModeType.PedestrianPathMode:
                    {
                        gameplayCanvas.enabled = false;
                        controlCanvas.enabled = true;
                        break;
                    }
                case GameModeType.TrafficPathMode:
                    {
                        gameplayCanvas.enabled = false;
                        controlCanvas.enabled = true;
                        break;
                    }
            }

            gameModePanel.SelectMode(mode);
        }
    }
}
