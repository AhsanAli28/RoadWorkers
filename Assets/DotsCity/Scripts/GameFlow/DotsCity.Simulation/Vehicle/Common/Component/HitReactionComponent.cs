using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class CarShaderMaterialConsts
    {
        public const string Deviation = "_Deviation";
        public const string LerpValue = "_LerpValue";
    }

    public struct AnimateHitReactionTag : IComponentData { }

    public struct VehicleAnimatedHullTag : IComponentData { }

    public struct HitReactionVehicleBodyTag : IComponentData { }

    public struct HitReactionInitComponent : IComponentData, IEnableableComponent
    {
        public Entity VehicleEntity;
    }

    public struct HitReactionStateComponent : IComponentData
    {
        public float ActivateTime;
    }

    public struct HitReactionMaterialDataComponent : IComponentData
    {
        public int IsForth;
        public float TValue;

        public HitReactionMaterialDataComponent GetDefault()
        {
            IsForth = 1;
            TValue = 0.5f;

            return this;
        }
    }

    public struct CarHitReactionData : IComponentData
    {
        public float3 Offset;
        public Entity HitMeshEntity;
    }

    public struct CarHitReactionTakenIndex : ICleanupComponentData
    {
        public int TakenIndex;
    }

    [MaterialProperty(CarShaderMaterialConsts.Deviation)]
    public struct CarShaderDeviationData : IComponentData
    {
        public float3 Value;
    }

    [MaterialProperty(CarShaderMaterialConsts.LerpValue)]
    public struct CarShaderLerpData : IComponentData
    {
        public float Value;
    }
}