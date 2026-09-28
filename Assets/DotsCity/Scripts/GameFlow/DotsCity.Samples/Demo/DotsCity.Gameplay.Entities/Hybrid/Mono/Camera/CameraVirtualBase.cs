#if CINEMACHINE
using System;
using UnityEngine;
#if !CINEMACHINE_V3
using Cinemachine;
#else
using Unity.Cinemachine;
#endif
#endif

namespace Spirit604.DotsCity.Gameplay.CameraService
{
    public abstract class CameraVirtualBase : CameraBase
    {

#if CINEMACHINE
#if !CINEMACHINE_V3
        private CinemachineVirtualCamera activeCamera;

        public CinemachineVirtualCamera ActiveCamera { get => activeCamera; protected set => activeCamera = value; }

        public event Action<CinemachineVirtualCamera> OnCameraChanged = delegate { };
#else
        private CinemachineCamera activeCamera;

        public CinemachineCamera ActiveCamera { get => activeCamera; protected set => activeCamera = value; }

        public event Action<CinemachineCamera> OnCameraChanged = delegate { };
#endif

        public override bool SetTarget(Transform newActor)
        {
            if (base.SetTarget(newActor))
            {
                activeCamera.Follow = CurrentActor;
                activeCamera.LookAt = CurrentActor;
                return true;
            }

            return false;
        }

#if !CINEMACHINE_V3
        public virtual void ChangeCamera(CinemachineVirtualCamera newCamera)
        {
            if (activeCamera != null)
            {
                newCamera.Follow = activeCamera.Follow;
                newCamera.LookAt = activeCamera.LookAt;
                activeCamera.enabled = false;
            }

            activeCamera = newCamera;
            activeCamera.enabled = true;
            OnCameraChanged(activeCamera);
        }
#else
        public virtual void ChangeCamera(CinemachineCamera newCamera)
        {
            if (activeCamera != null)
            {
                newCamera.Follow = activeCamera.Follow;
                newCamera.LookAt = activeCamera.LookAt;
                activeCamera.enabled = false;
            }

            activeCamera = newCamera;
            activeCamera.enabled = true;
            OnCameraChanged(activeCamera);
        }
#endif
#endif
    }
}