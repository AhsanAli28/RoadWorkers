using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public static class StateExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToSetNextState(this ref NextStateComponent nextStateComponent, ActionState state, bool force = false)
        {
            if (nextStateComponent.CanSwitchState(state) || force)
            {
                if (nextStateComponent.NextActionState == ActionState.Default)
                {
                    nextStateComponent.NextActionState = state;

                    if (force)
                    {
                        nextStateComponent.ForceState = true;
                    }

                    return true;
                }
                else if (nextStateComponent.NextActionState != state && nextStateComponent.NextActionState2 == ActionState.Default)
                {
                    nextStateComponent.NextActionState2 = state;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToSetNextState(EntityManager entityManager, Entity entity, ActionState state, bool force = false)
        {
            var nextStateComponent = entityManager.GetComponentData<NextStateComponent>(entity);

            if (nextStateComponent.TryToSetNextState(state, force))
            {
                entityManager.SetComponentData(entity, nextStateComponent);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ActionState GetActionState(EntityManager entityManager, Entity entity)
        {
            return entityManager.GetComponentData<StateComponent>(entity).ActionState;
        }

        /// <summary>
        /// If state can’t be set, then target swap back.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToSetNextState(this ref NextStateComponent nextStateComponent, ActionState state, ref DestinationComponent destinationComponent)
        {
            if (TryToSetNextState(ref nextStateComponent, state))
            {
                return true;
            }

            destinationComponent = destinationComponent.SwapBack();

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNextState(this in NextStateComponent nextStateComponent, ActionState state)
        {
            if (nextStateComponent.NextActionState != ActionState.Default)
            {
                if (nextStateComponent.NextActionState == state)
                {
                    return true;
                }
                else if (nextStateComponent.NextActionState2 != ActionState.Default && nextStateComponent.NextActionState2 == state)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the state has given ActionState flag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasActionStateFlag(this in StateComponent stateComponent, ActionState state) => DotsEnumExtension.HasFlagUnsafe(stateComponent.ActionState, state);

        /// <summary>
        /// Check if the entity has state or is waiting for proccess next state.
        /// <param name="checkAnyAdditiveFlag">CheckAnyAdditiveFlag is true: additive states check if any non-default flag returns false.</param>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasActionState(this in StateComponent stateComponent, in NextStateComponent nextStateComponent, ActionState state, bool checkAnyAdditiveFlag = false)
        {
            var hasState = stateComponent.IsActionState(state) || nextStateComponent.HasNextState(state);

            if (!checkAnyAdditiveFlag)
            {
                return hasState;
            }
            else
            {
                return hasState && !HasAnyAdditiveStateFlags(stateComponent);
            }
        }

        /// <summary>
        /// Check that the state has set ActionState without flags.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsActionState(this in StateComponent stateComponent, ActionState state) => stateComponent.ActionState == state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefaltActionState(this in StateComponent stateComponent) => stateComponent.ActionState == ActionState.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyAdditiveStateFlags(this in StateComponent stateComponent) => stateComponent.AdditiveStateFlags != ActionState.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovementState(this in StateComponent stateComponent, MovementState state) => stateComponent.MovementState == state;
    }
}