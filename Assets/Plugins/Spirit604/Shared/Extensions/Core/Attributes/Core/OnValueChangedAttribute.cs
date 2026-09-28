using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OnValueChangedAttribute : AttributeBase
    {
        public string Condition;

        public OnValueChangedAttribute(string condition)
        {
            this.Condition = condition;
        }
    }
}