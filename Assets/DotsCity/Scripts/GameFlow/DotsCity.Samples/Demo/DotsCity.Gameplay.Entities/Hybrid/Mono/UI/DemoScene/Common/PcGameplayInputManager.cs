using Spirit604.Attributes;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.UI;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class PcGameplayInputManager : MonoBehaviour, IKeyListener
    {
        private const int MinAlphaCode = 49; // KeyCode.Alpha1
        private const int MaxAlphaCode = 57; // KeyCode.Alpha9

        [SerializeField] private PlayerEnterCarStatePresenter playerStatePresenter;
        [SerializeField] private PlayerWeaponPresenter playerWeaponPresenter;
        [SerializeField] private ResetManager resetManager;

        [SerializeField] private KeyCode enterCarCode = KeyCode.E;

        private IKeyboardInputManager keyboardInputManager;

        [InjectWrapper]
        public void Construct(IKeyboardInputManager keyboardInputManager)
        {
            this.keyboardInputManager = keyboardInputManager;
        }

        private void OnEnable()
        {
            keyboardInputManager.AddListener(this, KeyCode.Escape, KeyEventType.Down);
            keyboardInputManager.AddListener(this, enterCarCode, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha1, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha2, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha3, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha4, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha5, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha6, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha7, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha8, KeyEventType.Down);
            keyboardInputManager.AddListener(this, KeyCode.Alpha9, KeyEventType.Down);
        }

        private void OnDisable()
        {
            keyboardInputManager.RemoveListener(this, KeyCode.Escape, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, enterCarCode, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha1, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha2, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha3, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha4, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha5, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha6, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha7, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha8, KeyEventType.Down);
            keyboardInputManager.RemoveListener(this, KeyCode.Alpha9, KeyEventType.Down);
        }

        public void Raise(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Escape)
            {
                resetManager.DoReset();
            }

            if (keyCode == enterCarCode)
            {
                playerStatePresenter.InteractCar();
            }

            int code = (int)keyCode;

            if (code >= MinAlphaCode && code <= MaxAlphaCode)
            {
                var index = code - MinAlphaCode;
                playerWeaponPresenter.TryToSelectWeapon(index);
            }
        }
    }
}