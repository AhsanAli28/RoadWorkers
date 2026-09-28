using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class HideIfAttribute : VisibleFieldBaseAttribute
    {
        public HideIfAttribute(string condition) : base(condition)
        {
            Inverted = true;
        }
    }
}