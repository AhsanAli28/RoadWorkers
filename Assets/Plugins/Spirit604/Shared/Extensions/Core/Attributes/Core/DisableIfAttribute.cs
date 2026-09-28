using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class DisableIfAttribute : EnableFieldBaseAttribute
    {
        public DisableIfAttribute(string condition) : base(condition)
        {
            Inverted = true;
        }
    }
}