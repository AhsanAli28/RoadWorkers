using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public static class CullStateUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ChangeState(
            in CullState newState,
            ref CullStateComponent cullComponent,
            ref EnabledRefRW<CulledEventTag> culledTagRW,
            ref EnabledRefRW<InPermittedRangeTag> inPermittedRangeTagRW,
            ref EnabledRefRW<InViewOfCameraTag> inViewOfCameraTagRW)
        {
            if (cullComponent.State != CullState.Uninitialized)
            {
                // Previous state
                switch (cullComponent.State)
                {
                    case CullState.Culled:
                        {
                            culledTagRW.ValueRW = false;
                            break;
                        }
                    case CullState.InViewOfCamera:
                        {
                            inViewOfCameraTagRW.ValueRW = false;
                            break;
                        }
                    case CullState.CloseToCamera:
                        {
                            inPermittedRangeTagRW.ValueRW = false;
                            break;
                        }
                }

                switch (newState)
                {
                    case CullState.Culled:
                        {
                            culledTagRW.ValueRW = true;
                            break;
                        }
                    case CullState.InViewOfCamera:
                        {
                            inViewOfCameraTagRW.ValueRW = true;
                            break;
                        }
                    case CullState.CloseToCamera:
                        {
                            inPermittedRangeTagRW.ValueRW = true;
                            break;
                        }
                }
            }
            else
            {
                switch (newState)
                {
                    case CullState.Culled:
                        {
                            inViewOfCameraTagRW.ValueRW = false;
                            inPermittedRangeTagRW.ValueRW = false;
                            break;
                        }
                    case CullState.InViewOfCamera:
                        {
                            culledTagRW.ValueRW = false;
                            inPermittedRangeTagRW.ValueRW = false;
                            break;
                        }
                    case CullState.CloseToCamera:
                        {
                            culledTagRW.ValueRW = false;
                            inViewOfCameraTagRW.ValueRW = false;
                            break;
                        }
                }
            }

            cullComponent.State = newState;
        }
    }
}