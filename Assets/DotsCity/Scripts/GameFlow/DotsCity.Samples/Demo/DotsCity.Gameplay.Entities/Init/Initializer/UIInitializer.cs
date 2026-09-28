using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.UI;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Initialization
{
    public class UIInitializer : InitializerBase, ILateInitializer
    {
        #region Inspector variables

        [SerializeField] private Canvas canvas;
        [SerializeField] private FPSDisplay fPSDisplay;

        #endregion

        #region Constructor

        private GeneralSettingData generalSettingData;

        [InjectWrapper]
        public void Construct(
            GeneralSettingData generalSettingData)
        {
            this.generalSettingData = generalSettingData;
        }

        #endregion

        public override void Initialize()
        {
            base.Initialize();

            if (canvas)
            {
                canvas.enabled = false;
            }

            if (fPSDisplay != null && generalSettingData.ShowFps)
            {
                fPSDisplay.Enable();
            }
        }

        public void LateInitialize()
        {
            if (canvas && !generalSettingData.HideUI)
            {
                canvas.enabled = true;
            }

            if (fPSDisplay != null && generalSettingData.ShowFps)
            {
                fPSDisplay.ResetWithDelay(1);
            }
        }
    }
}