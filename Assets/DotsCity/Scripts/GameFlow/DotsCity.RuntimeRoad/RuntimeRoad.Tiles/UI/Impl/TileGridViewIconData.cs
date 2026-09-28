using Spirit604.Collections.Dictionary;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [CreateAssetMenu(menuName = "Spirit604/RuntimeDemo/TileGridViewIconData")]
    public class TileGridViewIconData : ScriptableObject
    {
        [Serializable]
        public class IconDictionary : AbstractSerializableDictionary<string, Sprite> { }

        [SerializeField] private IconDictionary data = new IconDictionary();

        public IconDictionary Data { get => data; }

        public Sprite GetIcon(string key)
        {
            if (data.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            return null;
        }
    }
}
