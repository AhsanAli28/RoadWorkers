using System;

#if UNITY_EDITOR

namespace Spirit604.CityEditor
{
    public class FilteredVariableData
    {
        public string SceneViewName;
        public string SceneViewShortName;
        public Enum EnumValue;
        public float FloatValue;
        public bool BoolValue;
        public Type Type;
        public bool HasRange;
        public float MinValue;
        public float MaxValue;

        public bool NumberValue => Type == typeof(int) || Type == typeof(float) || Type == typeof(double) || Type == typeof(byte) || Type == typeof(short);
        public float LerpedValue => 1 - (MaxValue - FloatValue) / MaxValue;
        public object Value
        {
            get
            {
                if (NumberValue)
                {
                    return FloatValue;
                }
                if (Type == typeof(bool))
                {
                    return BoolValue;
                }
                if (Type == typeof(Enum))
                {
                    return EnumValue;
                }

                return null;
            }
        }
    }
}

#endif
