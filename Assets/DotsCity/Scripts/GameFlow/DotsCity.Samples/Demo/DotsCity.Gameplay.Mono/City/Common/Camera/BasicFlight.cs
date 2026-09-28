using Spirit604.Extensions;
using Spirit604.Gameplay.InputService;
using UnityEngine;

namespace Spirit604.Gameplay.Player
{
    public class BasicFlight : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float liftSpeed = 10f;
        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float boostSpeed = 20f;
        [SerializeField] private float bankSpeed = 10f;

        private IMotionInput input;

        private void Update()
        {
            bool shifted = false;

            if (InputExtension.GetKey(KeyCode.LeftShift))
            {
                shifted = true;
            }

            float movingSpeed = !shifted ? moveSpeed : boostSpeed;

            if (InputExtension.GetKey(KeyCode.W))
                transform.Translate(Vector3.forward * movingSpeed * Time.deltaTime); // As it says forward speed

            if (InputExtension.GetKey(KeyCode.S))
                transform.Translate(-Vector3.forward * movingSpeed * Time.deltaTime); // Backward speed

            if (InputExtension.GetKey(KeyCode.Space))
                transform.Translate(Vector3.up * movingSpeed * Time.deltaTime);

            if (InputExtension.GetKey(KeyCode.A))
                transform.Rotate(0f, -turnSpeed * Time.deltaTime, 0f, Space.World); // Left turn in relation to world not object

            if (InputExtension.GetKey(KeyCode.D))
                transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f, Space.World); // As above but right

            if (InputExtension.GetKey(KeyCode.UpArrow))
                transform.Rotate(liftSpeed * Time.deltaTime, 0f, 0f); // Descends object Same as actual plane joy stick forward is down

            if (InputExtension.GetKey(KeyCode.DownArrow))
                transform.Rotate(-liftSpeed * Time.deltaTime, 0f, 0f); // Raises object

            if (InputExtension.GetKey(KeyCode.LeftArrow))
                transform.Rotate(0f, 0f, bankSpeed * Time.deltaTime); // Bank left

            if (InputExtension.GetKey(KeyCode.RightArrow))
                transform.Rotate(0f, 0f, -bankSpeed * Time.deltaTime); // Bank right

            if (input.MovementInput != Vector3.zero)
            {
                var horizontal = input.MovementInput.x;
                var vertical = input.MovementInput.z;

                transform.position += transform.forward * vertical * movingSpeed * Time.deltaTime;
                transform.position += transform.right * horizontal * movingSpeed * Time.deltaTime;
            }

            if (Application.isMobilePlatform)
            {
                if (input.FireInput != Vector3.zero)
                {
                    var horizontal = input.FireInput.x;
                    var vertical = input.FireInput.z;

                    float xRot = turnSpeed * vertical * Time.deltaTime;
                    float yRot = turnSpeed * horizontal * Time.deltaTime;

                    transform.Rotate(-xRot, yRot, 0.0f);
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
                }
            }
        }

        public void Initialize(IMotionInput input)
        {
            this.input = input;
        }
    }
}