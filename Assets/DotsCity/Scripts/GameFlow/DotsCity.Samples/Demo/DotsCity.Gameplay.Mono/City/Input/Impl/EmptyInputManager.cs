using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class EmptyInputManager : IKeyboardInputManager
    {
        public void AddListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType)
        {
        }

        public void RemoveListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType)
        {
        }
    }
}