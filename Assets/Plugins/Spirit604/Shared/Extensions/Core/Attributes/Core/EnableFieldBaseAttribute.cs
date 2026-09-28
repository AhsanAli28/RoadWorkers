using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public abstract class EnableFieldBaseAttribute : AttributeBase
    {
        public string Condition { get; private set; }
        public bool Inverted { get; protected set; }

        public EnableFieldBaseAttribute(string condition)
        {
            this.Condition = condition;
        }
    }
}