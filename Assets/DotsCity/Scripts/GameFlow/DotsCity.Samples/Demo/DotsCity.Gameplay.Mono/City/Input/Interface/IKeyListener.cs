using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public interface IKeyListener
    {
        void Raise(KeyCode keyCode);
    }
}