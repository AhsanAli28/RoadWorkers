using System;

namespace Spirit604.DotsCity.EditorTools
{
    [Flags]
    public enum WheelSearchType
    {
        ByTextPattern = 1 << 0,
        ByPosition = 1 << 1
    }
}
