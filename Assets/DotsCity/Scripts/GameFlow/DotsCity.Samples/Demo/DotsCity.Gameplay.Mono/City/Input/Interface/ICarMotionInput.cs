using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public interface ICarMotionInput
    {
        Vector2 GetMovementInput(Vector3 forward);
        Vector3 FireInput { get; }
        bool Brake { get; }
    }
}