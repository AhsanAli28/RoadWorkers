using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class VehicleCustomDebugger : MonoBehaviourBase
    {
        public enum FilterType { DebugOnly, All }
        public enum DebugType { Raycast, Suspension }

        public bool showDebug;

        [ShowIf(nameof(showDebug))]
        public FilterType filterType;

        [ShowIf(nameof(showDebug))]
        public DebugType debugType;

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying)
            {
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var wheelDebugs = WheelContactSystem.WheelDebugInfoStaticRef;

            if (!wheelDebugs.IsCreated)
            {
                return;
            }

            foreach (var wheelDebug in wheelDebugs)
            {
                if (filterType == FilterType.DebugOnly)
                {
                    var wheelDebugShared = entityManager.GetSharedComponent<WheelDebugShared>(wheelDebug.Key);

                    if (!wheelDebugShared.ShowDebug)
                    {
                        continue;
                    }
                }

                switch (debugType)
                {
                    case DebugType.Raycast:
                        {
                            var color = wheelDebug.Value.IsInContact ? Color.green : Color.red;

                            Gizmos.color = color;
                            Gizmos.DrawLine(wheelDebug.Value.Start, wheelDebug.Value.End);

                            break;
                        }
                    case DebugType.Suspension:
                        {
                            var wheel = entityManager.GetComponentData<WheelContact>(wheelDebug.Key);

                            var color = wheelDebug.Value.IsInContact ? Color.green : Color.red;

                            var end = (Vector3)wheelDebug.Value.Start - Vector3.up * wheel.CurrentSuspensionLength;

                            Gizmos.color = color;
                            Gizmos.DrawLine(wheelDebug.Value.Start, end);

                            break;
                        }
                }
            }
        }
#endif
    }
}
