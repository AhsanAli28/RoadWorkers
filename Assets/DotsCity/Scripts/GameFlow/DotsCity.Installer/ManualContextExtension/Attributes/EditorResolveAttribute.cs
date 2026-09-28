using System;

namespace Spirit604.DotsCity.Installer
{
    /// <summary>
    /// Attribute for automatic dependency resolution in the Editor by ManualSceneContext.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EditorResolveAttribute : Attribute
    {
        public bool Optional { get; set; }

        public EditorResolveAttribute(bool optional = false)
        {
            Optional = optional;
        }
    }
}