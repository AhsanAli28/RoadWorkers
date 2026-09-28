using System;

namespace Spirit604.DotsCity.Core.Sound
{
    [Serializable]
    public class EventFloatParameter
    {
        public string Name;
        public float DefaultValue;

        [NonSerialized]
        public int RuntimeIndex;
    }
}
