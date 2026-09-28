using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class WorldInteractView : MonoBehaviour
    {
        private const string DEFAULT_BUTTON_LABLE = "Enter";

        [SerializeField] private Canvas canvas;
        [SerializeField] private WorldButton worldButton;
        [SerializeField] private Vector3 worldButtonOffset = new Vector3(0, 3, 0);

        public void SetWorldButton(Vector3 worldPosition, System.Action onClickAction, string text = "")
        {
            canvas.worldCamera = Camera.main;
            SwitchWorldButtonState(true);

            string buttonText = !string.IsNullOrEmpty(text) ? text : DEFAULT_BUTTON_LABLE;

            worldButton.Initialize(onClickAction, buttonText);

            SetPosition(worldPosition);
        }

        public void SetPosition(Vector3 worldPosition)
        {
            var buttonPosition = worldPosition + worldButtonOffset;
            worldButton.SetPosition(buttonPosition);
        }

        public void SetText(string text)
        {
            worldButton.SetText(text);
        }

        public void SwitchWorldButtonState(bool isActive)
        {
            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }
    }
}
