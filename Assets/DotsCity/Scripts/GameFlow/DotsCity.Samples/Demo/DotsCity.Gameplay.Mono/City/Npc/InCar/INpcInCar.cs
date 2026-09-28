using Spirit604.Gameplay.Car;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    /// <summary>
    /// The behaviour of NPCs while sitting in a car.
    /// </summary>
    public interface INpcInCar : INpcIDProvider
    {
        GameObject Actor { get; }

        /// <summary> Auto return weapons to the factory when the entity is disabled.</summary>
        bool AutoDisposeWeapon { get; set; }

        void Initialize(CarSlot carSlot);
        void Shoot(Vector3 direction);
        void Show(bool isShowing);
        void SnapHide();
        void Dispose();
    }
}