using Spirit604.Extensions;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct NextStateComponent : IComponentData
    {
        public ActionState NextActionState;
        public ActionState NextActionState2;
        public ActionState NextStateFlags;
        public ActionState RemoveState;
        public bool ForceState;

        public NextStateComponent(ActionState nextActionState)
        {
            NextActionState = nextActionState;
            NextStateFlags = ActionState.Default;
            NextActionState2 = ActionState.Default;
            RemoveState = ActionState.Default;
            ForceState = false;
        }

        public bool CanSwitchState(ActionState nextState)
        {
            if ((int)NextStateFlags > 0)
            {
                return DotsEnumExtension.HasFlagUnsafe(NextStateFlags, nextState);
            }

            return true;
        }

        public bool HasNextState => NextActionState != ActionState.Default;
    }
}