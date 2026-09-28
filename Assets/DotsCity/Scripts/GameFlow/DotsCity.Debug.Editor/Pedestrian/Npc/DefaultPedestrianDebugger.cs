#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class DefaultPedestrianDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public DefaultPedestrianDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return EntityManager.HasComponent(entity, typeof(StateComponent));
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var stateComponent = EntityManager.GetComponentData<StateComponent>(entity);
            var actionState = stateComponent.ActionState;
            var movementState = stateComponent.MovementState;

            sb.Clear();
            sb.Append("Entity index: ");
            sb.Append((entity.Index).ToString()).Append("\n");
            sb.Append("AS: "); // AS - 'ActionState'

            var actionStateText = GetActionStateText(actionState);
            sb.Append(actionStateText).Append("\n");
            sb.Append("MS: "); // MS - 'MovementState'
            sb.Append(movementState.ToString()).Append("\n");

            return sb;
        }

        private string GetActionStateText(ActionState actionState)
        {
            var actionStateText = string.Empty;

            if (actionState == ActionState.CrossingTheRoad)
            {
                actionStateText = "CrossRoad";
            }
            if (actionState == ActionState.MovingToNextTargetPoint)
            {
                actionStateText = "MoveToNext";
            }
            if (actionState == ActionState.WaitForGreenLight)
            {
                actionStateText = "WaitLight";
            }

            if (string.IsNullOrEmpty(actionStateText))
            {
                actionStateText = actionState.ToString();
            }

            return actionStateText;
        }
    }
}
#endif