using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class EnableIfAttribute : EnableFieldBaseAttribute
    {
        public EnableIfAttribute(string condition) : base(condition)
        {
        }
    }
}