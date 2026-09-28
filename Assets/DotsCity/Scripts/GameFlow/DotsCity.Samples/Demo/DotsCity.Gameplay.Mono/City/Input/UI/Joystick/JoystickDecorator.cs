using UnityEngine;

namespace Spirit604.Gameplay.UI
{
    public class JoystickDecorator : MonoBehaviour
    {
        [SerializeField] private FixedJoystick joystick;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private bool relativeCamera;

        public Vector3 MoveVector { get; private set; }
        public FixedJoystick Joystick { get => joystick; }
        public bool RelativeCamera { get => relativeCamera; set => relativeCamera = value; }

        private void Update()
        {
            if (relativeCamera)
            {
                MoveVector = Quaternion.Euler(0, cameraTransform.rotation.eulerAngles.y, 0) * (Vector3.right * joystick.Horizontal + Vector3.forward * joystick.Vertical);
            }
            else
            {
                MoveVector = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
            }
        }
    }
}
