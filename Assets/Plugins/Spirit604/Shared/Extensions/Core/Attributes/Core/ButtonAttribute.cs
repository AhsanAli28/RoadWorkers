using System;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute
#if !ODIN_INSPECTOR
        : AttributeBase
#else
        : Sirenix.OdinInspector.ButtonAttribute
#endif
    {
#if !ODIN_INSPECTOR
        public string Name { get; private set; }

        public ButtonAttribute(string label)
        {
            Name = label;
        }

        public ButtonAttribute()
        {
            Name = string.Empty;
        }
#else
        public ButtonAttribute() : base() { }
        public ButtonAttribute(string label) : base(label) { }
#endif
    }
}