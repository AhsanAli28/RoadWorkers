using Spirit604.Gameplay.InputService;
using UnityEngine;

#if CINEMACHINE
#if !CINEMACHINE_V3
using Cinemachine;
#else
using Unity.Cinemachine;
#endif
#endif

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleOrbitalCamera : MonoBehaviour
    {
#if CINEMACHINE

#pragma warning disable 0618

        [SerializeField] private CinemachineFreeLook cam;

#pragma warning restore 0618

#endif

        [SerializeField] private Transform target;

        [SerializeField] private float speedX = 200f;
        [SerializeField] private float speedY = 5f;

        private ICarMotionInput input;

#if CINEMACHINE
        private void Update()
        {
            cam.m_XAxis.Value += input.FireInput.x * Time.deltaTime * speedX;
            cam.m_YAxis.Value += input.FireInput.z * Time.deltaTime * speedY;
        }
#endif

        public void Initialize(ICarMotionInput input)
        {
            this.input = input;
        }
    }
}
