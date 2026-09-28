using UnityEngine;

namespace Spirit604.Gameplay.UI
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private Canvas inputCanvas;
        [SerializeField] private JoystickDecorator leftJoystick;
        [SerializeField] private JoystickDecorator rightJoystick;
        [SerializeField] private BrakeView brakeView;

        public static InputManager Instance { get; private set; }

        public bool Brake => brakeView?.Pressed ?? false;

        private void Awake()
        {
            Instance = this;
        }

        public void SwitchEnabledState(bool isEnabled)
        {
            inputCanvas.enabled = isEnabled;
        }

        public Vector3 GetMove(int index)
        {
            switch (index)
            {
                case 0: { return leftJoystick.MoveVector; }
                case 1: { return rightJoystick.MoveVector; }
            }

            return default;
        }

        public void SetRelativeCamera(int index, bool isRelative)
        {
            switch (index)
            {
                case 0: { leftJoystick.RelativeCamera = isRelative; break; }
                case 1: { rightJoystick.RelativeCamera = isRelative; break; }
            }
        }

        public JoystickDecorator GetJoystick(int index)
        {
            switch (index)
            {
                case 0: { return leftJoystick; }
                case 1: { return rightJoystick; }
            }

            return null;
        }
    }
}
