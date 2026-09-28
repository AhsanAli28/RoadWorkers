using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.Gameplay.UI
{
    public class BrakeView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private PointerView pointerView;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite pressedSprite;

        public bool Pressed => pointerView.Pressed;

        private void Awake()
        {
            pointerView.OnPressed += PointerView_OnPressed;
        }

        private void PointerView_OnPressed(bool pressed)
        {
            button.image.sprite = pressed ? pressedSprite : defaultSprite;
        }
    }
}