using System;
using UnityEngine;

namespace Spirit604.DotsCity.Installer
{
    /// <summary>
    /// Attribute to indicate in the inspector that the object is manually resolved when DI is disabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ResolveLabelAttribute : PropertyAttribute
    {
        public bool Resolve { get; set; }
        public bool Optional { get; set; }

        public ResolveLabelAttribute(bool resolve = true, bool optional = false)
        {
            Resolve = resolve;
            Optional = optional;
        }
    }
}