using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerNpcCarBehaviour : NpcCarBehaviour
    {
        private IShootTargetProvider targetProvider;

        protected override void Update()
        {
            base.Update();

            if (CarSlots.TakenSeatsCount == 0)
                return;

            Vector3 shootingDirection = Vector3.zero;

            for (int i = 0; i < CarSlots.SlotCount; i++)
            {
                var slot = CarSlots.GetSlot(i);

                if (!CanShoot(slot))
                    continue;

                if (targetProvider.GetShootDirection(slot.NpcInCar.Actor.transform.position, out shootingDirection))
                {
                    Shoot(slot, shootingDirection);
                }
                else
                {
                    Shoot(slot, Vector3.zero);
                }
            }

            if (shootingDirection != Vector3.zero)
            {
                ResetIdleTime();
            }
        }

        public void Initialize(
            IShootTargetProvider targetProvider)
        {
            this.targetProvider = targetProvider;
        }
    }
}
