using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class KeyContainerDown : KeyContainerBase
    {
        public override bool Fire(KeyCode key) => InputExtension.GetKeyDown(key);
    }
}