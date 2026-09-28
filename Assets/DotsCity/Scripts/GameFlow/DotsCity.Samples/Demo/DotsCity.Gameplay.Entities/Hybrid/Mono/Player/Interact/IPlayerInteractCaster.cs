using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay
{
    public interface IPlayerInteractCaster
    {
        GameObject Interactable { get; }

        event Action<GameObject> OnCastStateChanged;

        void SwitchEnabledState(bool enabled);
    }
}