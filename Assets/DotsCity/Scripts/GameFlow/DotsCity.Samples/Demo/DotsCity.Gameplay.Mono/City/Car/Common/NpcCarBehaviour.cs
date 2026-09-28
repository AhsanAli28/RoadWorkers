using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    [RequireComponent(typeof(CarSlots))]
    public class NpcCarBehaviour : MonoBehaviour
    {
        private const float MaxIdleTime = 5;

        private bool isHided = true;
        private float nextHideTime;

        protected CarSlots CarSlots { get; private set; }

        protected virtual void Awake()
        {
            CarSlots = GetComponent<CarSlots>();
        }

        private void OnDisable()
        {
            ForceHide();
        }

        protected virtual void Update()
        {
            if (!isHided && Time.time > nextHideTime)
            {
                isHided = true;
                Show(!isHided);
            }
        }

        public void ForceHide()
        {
            nextHideTime = 0;
            isHided = true;
        }

        protected void Show(bool isShowing)
        {
            for (int i = 0; i < CarSlots.SlotCount; i++)
            {
                var slot = CarSlots.GetSlot(i);

                if (CanShoot(slot))
                {
                    slot.NpcInCar.Show(isShowing);
                }
            }
        }

        protected void Shoot(CarSlot carSlot, Vector3 direction)
        {
            if (!CanShoot(carSlot))
            {
                return;
            }

            carSlot.NpcInCar?.Shoot(direction);
        }

        protected bool CanShoot(CarSlot carSlot)
        {
            return carSlot.NpcInCar != null && carSlot.ShootingSlot;
        }

        protected void ResetIdleTime()
        {
            if (isHided)
            {
                Show(true);
            }

            isHided = false;
            nextHideTime = Time.time + MaxIdleTime;
        }
    }
}