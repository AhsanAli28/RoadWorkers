using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.MainMenu.UI
{
    public class MainMenuBottomView : MonoBehaviour
    {
        [SerializeField] private Button resetButton;
        [SerializeField] private TextMeshProUGUI resetText;
        [SerializeField] private Button loadSceneButton;

        public event Action OnResetClicked = delegate { };
        public event Action OnLoadSceneButtonClicked = delegate { };

        private void Awake()
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() => OnResetClicked());

            loadSceneButton.onClick.RemoveAllListeners();
            loadSceneButton.onClick.AddListener(() => OnLoadSceneButtonClicked());
        }

        public void SwitchResetState(bool isActive)
        {
            if (resetButton.interactable != isActive)
            {
                resetButton.interactable = isActive;

                byte alpha = resetButton.interactable ? (byte)255 : (byte)160;
                Color32 color = resetText.color;
                color.a = alpha;
                resetText.color = color;
            }
        }
    }
}

