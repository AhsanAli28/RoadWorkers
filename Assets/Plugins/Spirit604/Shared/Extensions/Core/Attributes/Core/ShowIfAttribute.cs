using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class ShowIfAttribute : VisibleFieldBaseAttribute
    {
        public ShowIfAttribute(string condition) : base(condition)
        {
        }
    }
}