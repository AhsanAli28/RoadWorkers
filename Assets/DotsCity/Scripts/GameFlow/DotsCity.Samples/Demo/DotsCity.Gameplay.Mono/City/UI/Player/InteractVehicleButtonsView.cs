using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.Gameplay.UI
{
    public class InteractVehicleButtonsView : MonoBehaviour
    {
        [SerializeField] private Button exitCarButton;
        [SerializeField] private Button enterCarButton;

        public event Action OnEnterClicked = delegate { };
        public event Action OnExitClicked = delegate { };

        private void Awake()
        {
            exitCarButton.onClick.AddListener(ExitCarButton_OnClick);
            enterCarButton.onClick.AddListener(EnterCarButton_OnClick);
        }

        public void SwitchExitCarButton(bool isActive)
        {
            if (exitCarButton.gameObject.activeSelf != isActive)
            {
                exitCarButton.gameObject.SetActive(isActive);
            }
        }

        public void SwitchEnterCarButton(bool isActive)
        {
            if (enterCarButton.gameObject.activeSelf != isActive)
            {
                enterCarButton.gameObject.SetActive(isActive);
            }
        }

        public void ExitCarButton_OnClick()
        {
            OnExitClicked();
        }

        public void EnterCarButton_OnClick()
        {
            OnEnterClicked();
        }
    }
}
