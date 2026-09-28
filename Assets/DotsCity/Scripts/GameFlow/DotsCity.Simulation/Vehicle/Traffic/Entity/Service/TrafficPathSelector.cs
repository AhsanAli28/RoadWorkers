using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficPathSelector : PathSelectorBase
    {
        protected override EntitySelectionService.EntityType EntityType => EntitySelectionService.EntityType.Traffic;

        protected override void Update()
        {
            base.Update();
            ProcessInput();
        }

        public override void ChangeIdleState(Entity selectedEntity, bool isIdle)
        {
            if (isIdle)
            {
                TrafficStateExtension.AddIdleState(EntityManager, selectedEntity, TrafficIdleState.UserCreated);
            }
            else
            {
                TrafficStateExtension.RemoveIdleState(EntityManager, selectedEntity, TrafficIdleState.UserCreated);
            }
        }

        protected override bool EntityExist(Entity entity)
        {
            return EntityManager.HasComponent<TrafficTag>(entity);
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
    }
}