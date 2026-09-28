using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Spirit604.Gameplay.UI
{
    public class PointerView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Pressed { get; private set; }

        public event Action<bool> OnPressed = delegate { };

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPressed(true);
            Pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPressed(false);
            Pressed = false;
        }
    }
}