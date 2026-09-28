using UnityEngine;

namespace Spirit604.DotsCity.Samples.CustomTrain
{
    public class LocomotiveEngine : MonoBehaviour
    {
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float deacceleration = 3;
        [SerializeField] private float throttle;

        public float Speed { get; private set; }

        public float Throttle { get => throttle; set => throttle = value; }

        private void FixedUpdate()
        {
            if (throttle > 0)
            {
                Speed += acceleration * Time.fixedDeltaTime;
                Speed = Mathf.Clamp(Speed, 0, maxSpeed);
            }
            else
            {
                Speed -= deacceleration * Time.fixedDeltaTime;
                Speed = Mathf.Clamp(Speed, 0, maxSpeed);
            }
        }
    }
}
