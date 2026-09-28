using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public interface IMotionInput
    {
        Vector3 MovementInput { get; }
        Vector3 FireInput { get; }
    }
}