using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public enum IgnitionState { Default, IdleBeforeIgnite, Ignition, EngineStarted, EngineStopping }

    public struct CarIgnitionData : IComponentData
    {
        public IgnitionState IgnitionState;
        public bool EngineStarted;
        public float EndTime;
    }

    public struct CarIgnitionStartedTag : IComponentData { }

    public struct CarStoppingEngineStartedTag : IComponentData { }

    public struct CarRemoveDriverAfterStopEngineTag : IComponentData { }
}