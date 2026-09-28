using Spirit604.Extensions;
using UnityEngine;

#if CINEMACHINE
#if !CINEMACHINE_V3
using Cinemachine;
#else
using Unity.Cinemachine;
#endif
#endif

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerCameraBehaviourExample : SingletonMonoBehaviour<PlayerCameraBehaviourExample>
    {
#if CINEMACHINE
#if !CINEMACHINE_V3
        [SerializeField] private CinemachineVirtualCamera _camera;
        [SerializeField] private CinemachineVirtualCamera _carCamera;
#else
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineCamera _carCamera;
#endif
#endif

        private bool HasCarCamera => _carCamera != null;

        public void SetTarget(Transform target, bool playerNpcCamera = true)
        {
#if CINEMACHINE
            if (!HasCarCamera)
            {
                _camera.Follow = target;
            }
            else
            {
                _camera.enabled = playerNpcCamera;
                _carCamera.enabled = !playerNpcCamera;

                if (playerNpcCamera)
                {
                    _camera.Follow = target;
                }
                else
                {
                    _carCamera.Follow = target;
                    _carCamera.LookAt = target;
                }
            }
#endif
        }
    }
}
