using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    public struct HasSoundTag : IComponentData, IEnableableComponent { }

    public struct SoundComponent : IComponentData
    {
        public int Id;
    }

    public struct SoundCacheVolume : IComponentData
    {
        public float PreviousVolume;
        public float PreviousPitch;
    }

    public struct SoundEventComponent : IComponentData
    {
        public SoundEventType NewEvent;
    }

    public enum SoundEventType { Default, Stop, StopFadeout, Play }

    public struct SoundVolume : IComponentData
    {
        public float Volume;
        public float Pitch;
    }

    public struct TrackSoundComponent : IComponentData
    {
        public Entity TargetEntity;
    }

    public struct FloatParameter : IBufferElementData
    {
        public float Value;
    }

    public struct OneShot : IComponentData { }

    public struct SoundDelayData : IComponentData
    {
        public float Duration;
        public float FinishTimestamp;
    }

    public struct LoopSoundData : IComponentData
    {
        public float Duration;
        public float FinishTimestamp;
    }

    public struct SoundSharedType : ISharedComponentData, IEquatable<SoundSharedType>
    {
        public SoundType SoundType;

        public bool Equals(SoundSharedType other) => this.SoundType == other.SoundType;

        public override int GetHashCode() => (int)SoundType;
    }

    public enum SoundType
    {
        /// <summary> Default sound entity. </summary>
        Default,

        /// <summary> Entity played once & destroyed afterwards. </summary>
        OneShot,

        /// <summary> Entity tracks target entity. </summary>
        Tracking,

        /// <summary> Entity tracks target vehicle entity. </summary>
        TrackingVehicle,

        /// <summary> Entity tracks target entity & loop playback. </summary>
        TrackingAndLoop
    }
}