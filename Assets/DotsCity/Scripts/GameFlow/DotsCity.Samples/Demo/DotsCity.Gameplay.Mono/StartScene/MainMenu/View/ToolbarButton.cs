using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.MainMenu.UI
{
    public class ToolbarButton : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Image indicator;

        private bool blocked;

        public void Initialize(string text, System.Action onClickCallback)
        {
            buttonText.SetText(text);
            toggle.onValueChanged.RemoveAllListeners();

            toggle.onValueChanged.AddListener(delegate
            {
                if (!blocked)
                {
                    if (toggle.isOn)
                    {
                        onClickCallback();
                    }
                }
                else
                {
                    blocked = false;
                }

                ToggleValueChanged(toggle);
            });
        }

        public void Select()
        {
            blocked = true;
            toggle.isOn = true;
        }

        public void Unselect()
        {
            if (toggle != null)
            {
                toggle.isOn = false;
                toggle.interactable = true;
            }
        }

        public void SwitchIndicatorState(bool isActive)
        {
            if (indicator.gameObject.activeSelf != isActive) indicator.gameObject.SetActive(isActive);
        }

        private void ToggleValueChanged(Toggle change)
        {
            if (change.isOn)
            {
                toggle.interactable = false;
            }
        }
    }
}