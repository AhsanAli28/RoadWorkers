using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public abstract class VisibleFieldBaseAttribute : AttributeBase
    {
        public string Condition { get; private set; }
        public bool Inverted { get; protected set; }

        public VisibleFieldBaseAttribute(string condition)
        {
            this.Condition = condition;
        }
    }
}