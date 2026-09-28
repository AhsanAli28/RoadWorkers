using Spirit604.Extensions;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser
{
    public class AiNpcCarBehaviour : NpcCarBehaviour
    {
        [SerializeField][Range(0, 30f)] private float shootDistance = 10f;

        private IShootTargetProvider shootTargetProvider;
        private Camera mainCamera;

        public bool IsCombatState { get; set; }

        protected override void Update()
        {
            if (CarSlots.TakenSeatsCount == 0)
                return;

            base.Update();

            if (IsCombatState)
            {
                bool shot = false;

                if (mainCamera.InViewOfCamera(transform.position) && shootTargetProvider.HasTarget)
                {
                    var target = shootTargetProvider.GetTarget();
                    var distanceToTarget = Vector3.Distance(target, transform.position);

                    if (distanceToTarget < shootDistance)
                    {
                        for (int i = 0; i < CarSlots.SlotCount; i++)
                        {
                            var slot = CarSlots.GetSlot(i);

                            if (slot.NpcInCarActor != null)
                            {
                                var sourcePosition = slot.NpcInCarActor.transform.position;

                                if (shootTargetProvider.GetShootDirection(sourcePosition, out var shootDirection))
                                {
                                    shot = true;
                                    Shoot(slot, shootDirection);
                                }
                            }
                        }

                        if (shot)
                        {
                            ResetIdleTime();
                        }
                    }
                }

                if (!shot)
                {
                    for (int i = 0; i < CarSlots.SlotCount; i++)
                    {
                        var slot = CarSlots.GetSlot(i);
                        Shoot(slot, Vector3.zero);
                    }
                }
            }
        }

        public void Initialize(IShootTargetProvider shootTargetProvider, Camera camera)
        {
            this.shootTargetProvider = shootTargetProvider;
            this.mainCamera = camera;
        }
    }
}