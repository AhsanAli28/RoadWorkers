using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public interface IKeyboardInputManager
    {
        void AddListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType);
        void RemoveListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType);
    }
}