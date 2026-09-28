using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    public struct CarSoundData : IComponentData
    {
        public Entity SoundEntity;
        public CarSoundType CarSoundType;
        public CarSoundType NewCarSoundType;
        public float Pitch;
        public bool WaitForInit;
    }

    public struct CarUpdateSound : IComponentData, IEnableableComponent
    {
    }

    public struct CarHornComponent : IComponentData
    {
        public float NextHornTime;
    }

    public struct CarUpdateParamSound : IComponentData
    {
        public int ParamId;
        public float ParamValue;
    }

    public struct CarCustomEnginePitchTag : IComponentData
    {
    }

    public struct CarInitSoundEntity : IComponentData
    {
        public Entity VehicleEntity;
        public bool Initialized;
    }

    public struct CarInitSoundEntityTag : IComponentData, IEnableableComponent
    {
    }
}
