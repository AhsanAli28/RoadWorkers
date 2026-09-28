using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGameMobileUI : TileGameUI
    {
        [SerializeField] private TileGameMobileInput mobileInput;

        protected override void Awake()
        {
            base.Awake();
            mobileInput.SwitchButtons(false);
        }

        public override void Unselect()
        {
            mobileInput.SwitchButtons(false);
        }

        protected override void OnTileSelectedInternal(int id)
        {
            base.OnTileSelectedInternal(id);
            mobileInput.SwitchButtons(true);
        }

        protected override void OnModeSelectedInternal(PlacingType placingType)
        {
            base.OnModeSelectedInternal(placingType);
            mobileInput.SwitchButtons(false);

            switch (placingType)
            {
                case PlacingType.Remove:
                    mobileInput.SwitchAction(true);
                    mobileInput.SwitchUnselect(true);
                    break;
            }
        }
    }
}
