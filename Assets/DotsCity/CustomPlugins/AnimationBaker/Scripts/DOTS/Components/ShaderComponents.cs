using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Spirit604.AnimationBaker.Entities
{
    [MaterialProperty(Constans.PlaybackTime)]
    public struct ShaderPlaybackTime : IComponentData
    {
        public float Value;
    }

    [MaterialProperty(Constans.TargetPlaybackTime)]
    public struct ShaderTargetPlaybackTime : IComponentData
    {
        public float Value;
    }

    [MaterialProperty(Constans.TransitionTime)]
    public struct ShaderTransitionTime : IComponentData
    {
        public float Value;
    }

    [MaterialProperty(Constans.TargetFrameStepInvParam)]
    public struct ShaderTargetFrameStepInvData : IComponentData
    {
        public float Value;
    }

    [MaterialProperty(Constans.TargetFrameOffsetParam)]
    public struct ShaderTargetFrameOffsetData : IComponentData
    {
        public float2 Value;
    }
}
