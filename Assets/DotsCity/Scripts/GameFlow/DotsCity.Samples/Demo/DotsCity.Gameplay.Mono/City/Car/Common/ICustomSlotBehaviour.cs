using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    /// <summary>
    /// Interface for custom behavior when entering or exiting vehicle.
    /// </summary>
    public interface ICustomSlotBehaviour
    {
        void SwitchEnteredState(GameObject vehicle, bool isEntered);
    }
}