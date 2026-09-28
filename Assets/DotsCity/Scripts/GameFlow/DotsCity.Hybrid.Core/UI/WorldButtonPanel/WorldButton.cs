using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class WorldButton : WorldUIItem
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;

        private void OnEnable()
        {
            button.onClick.RemoveAllListeners();
        }

        public void Initialize(Action onClick, string buttonLabel = "")
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(buttonLabel))
            {
                SetText(buttonLabel);
            }
        }

        public void SetText(string buttonLabel)
        {
            buttonText.SetText(buttonLabel);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
