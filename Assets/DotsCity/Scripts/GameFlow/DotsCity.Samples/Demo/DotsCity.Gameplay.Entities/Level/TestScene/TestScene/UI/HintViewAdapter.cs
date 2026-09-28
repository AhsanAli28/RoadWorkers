using Spirit604.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.TestScene.UI
{
    public class HintViewAdapter : MonoBehaviourBase
    {
        private const string CustomVehicleHintKey = "CustomVehicleHint";

        [SerializeField]
        private Canvas hintCanvas;

        [SerializeField]
        private List<GameObject> mobileMessages = new List<GameObject>();

        [SerializeField]
        private GameObject textPanel;

        [SerializeField]
        private Button closeButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => SwitchCanvasState(false));

            var showed = PlayerPrefs.GetInt(CustomVehicleHintKey, 0) == 1;

            if (!showed)
            {
                PlayerPrefs.SetInt(CustomVehicleHintKey, 1);
                Show();
            }
        }

        private void Show()
        {
            SwitchCanvasState(true);
            var isMobile = Application.isMobilePlatform;

            if (!isMobile)
            {
                for (int i = 0; i < mobileMessages.Count; i++)
                {
                    mobileMessages[i].gameObject.SetActive(false);
                }
            }
            else
            {
                textPanel.gameObject.SetActive(false);
            }
        }

        [Button]
        private void ClearKey()
        {
            PlayerPrefs.DeleteKey(CustomVehicleHintKey);
        }

        private void SwitchCanvasState(bool isActive)
        {
            hintCanvas.enabled = isActive;
        }
    }
}
