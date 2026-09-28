using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAttribute
#if !ODIN_INSPECTOR
        : MetaAttributeBase
#else
        : Sirenix.OdinInspector.LabelTextAttribute
#endif
    {

#if !ODIN_INSPECTOR
        public string Label { get; private set; }

        public LabelAttribute(string label)
        {
            this.Label = label;
        }
#else
        public LabelAttribute(string text) : base(text) { }
#endif
    }
}
