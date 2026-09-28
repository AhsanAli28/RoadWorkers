using UnityEngine;

namespace Spirit604.Collections.Dictionary
{
    [System.Serializable]
    public class HotkeyDictionary : AbstractSerializableDictionary<string, KeyCode> { }

    public static class HotkeyDictionaryExtension
    {
        public static KeyCode GetKey(this HotkeyDictionary hotkeyDictionary, string dictKey, KeyCode defaultKey)
        {
            if (hotkeyDictionary != null && hotkeyDictionary.TryGetValue(dictKey, out var keyCode))
            {
                return keyCode;
            }

            return defaultKey;
        }
    }
}
