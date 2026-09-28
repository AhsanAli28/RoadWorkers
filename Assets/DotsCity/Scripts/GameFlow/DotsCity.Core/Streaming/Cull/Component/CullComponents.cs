using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public struct CullPointTag : IComponentData { }

    public struct CameraData : IComponentData
    {
        public Matrix4x4 ViewProjectionMatrix;
    }

    public struct CullStateComponent : IComponentData, IEnableableComponent
    {
        public CullState State;
    }

    public enum CullState : sbyte
    {
        /// <summary> Entity is far away (by default, the entity is destroyed or disabled). </summary>
        Culled = -1,

        /// <summary> Initial uninitialized state. </summary>
        Uninitialized = 0,

        /// <summary> Entity is fully enabled. </summary>
        InViewOfCamera = 1,

        /// <summary> State between to `CloseToCamera` and `InVisionOfCamera`, currently used to activate static physics objects. [optional]</summary>
        PreInitInCamera = 2,

        /// <summary> Entity is enabled but with limited or modified functionality for better performance. </summary>
        CloseToCamera = 3,
    }

    public enum CullStateList : byte
    {
        Default, PreInit
    }

    public enum CullMethod { CalculateDistance, CameraView, Custom }

    [TemporaryBakingType]
    public struct CullStateBakingTag : IComponentData { }

    public struct CulledEventTag : IComponentData, IEnableableComponent { }

    public struct InPermittedRangeTag : IComponentData, IEnableableComponent { }

    public struct PreInitInCameraTag : IComponentData, IEnableableComponent { }

    public struct InViewOfCameraTag : IComponentData, IEnableableComponent { }
}