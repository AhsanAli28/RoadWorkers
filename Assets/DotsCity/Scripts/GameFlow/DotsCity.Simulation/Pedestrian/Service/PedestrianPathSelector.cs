using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianPathSelector : PathSelectorBase
    {
        protected override EntitySelectionService.EntityType EntityType => EntitySelectionService.EntityType.Pedestrian;

        protected override void Update()
        {
            base.Update();
            ProcessInput();
        }

        public override void ChangeIdleState(Entity selectedEntity, bool isIdle)
        {
            EntityManager.SetComponentEnabled<IdleTag>(selectedEntity, isIdle);

            if (isIdle)
            {
                StateExtensions.TryToSetNextState(EntityManager, selectedEntity, ActionState.Idle, true);
            }
            else
            {
                StateExtensions.TryToSetNextState(EntityManager, selectedEntity, ActionState.MovingToNextTargetPoint, true);
            }
        }

        protected virtual void ProcessInput()
        {
            if (InputExtension.GetMouseButtonDown(0)) // Check for left mouse button click
            {
                Ray ray = Camera.main.ScreenPointToRay(InputExtension.GetMousePosition());
                Plane plane = new Plane(Vector3.up, Vector3.zero); // Define a plane, e.g., horizontal plane at y = 0

                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 worldPosition = ray.GetPoint(enter);

                    TryToSelectEntity(worldPosition);
                }
            }
        }

        protected override bool EntityExist(Entity entity)
        {
            return EntityManager.HasComponent<PedestrianMovementSettings>(entity);
        }
    }
}