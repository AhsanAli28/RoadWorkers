using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    public abstract class KeyContainerBase
    {
        public List<KeyCode> Keys = new List<KeyCode>();
        public Dictionary<KeyCode, List<IKeyListener>> KeyListeners = new Dictionary<KeyCode, List<IKeyListener>>();

        public void Update()
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                KeyCode key = Keys[i];

                if (Fire(key))
                {
                    foreach (var item in KeyListeners[key])
                    {
                        item.Raise(key);
                    }
                }
            }
        }

        public abstract bool Fire(KeyCode key);

        public void AddListener(IKeyListener listener, KeyCode key)
        {
            if (!Keys.Contains(key))
            {
                Keys.Add(key);
            }

            if (!KeyListeners.ContainsKey(key))
            {
                KeyListeners.Add(key, new List<IKeyListener>());
            }

            KeyListeners[key].Add(listener);
        }

        public void RemoveListener(IKeyListener listener, KeyCode key)
        {
            KeyListeners[key].Remove(listener);

            if (KeyListeners[key].Count == 0)
            {
                Keys.Remove(key);
            }
        }
    }
}