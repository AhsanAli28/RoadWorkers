using Spirit604.Attributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public class KeyboardInputManager : MonoBehaviour, IKeyboardInputManager
    {
        private Dictionary<KeyEventType, KeyContainerBase> containers = new Dictionary<KeyEventType, KeyContainerBase>();
        private KeyContainerBase[] containersList;

        private IInputSettings inputSettings;

        [InjectWrapper]
        public void Construct(IInputSettings inputSettings)
        {
            this.inputSettings = inputSettings;

            if (inputSettings.CurrentMobilePlatform)
            {
                enabled = false;
            }

            Initialize();
        }

        private void Update()
        {
            for (int i = 0; i < containersList?.Length; i++)
            {
                KeyContainerBase item = containersList[i];
                item.Update();
            }
        }

        public void AddListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType)
        {
            var container = containers[keyEventType];
            container.AddListener(listener, key);
        }

        public void RemoveListener(IKeyListener listener, KeyCode key, KeyEventType keyEventType)
        {
            var container = containers[keyEventType];
            container.RemoveListener(listener, key);
        }

        private void Initialize()
        {
            containers.Add(KeyEventType.Down, new KeyContainerDown());
            containers.Add(KeyEventType.Up, new KeyContainerUp());
            containers.Add(KeyEventType.Pressing, new KeyContainerPressing());

            containersList = containers.Values.ToArray();
        }
    }
}