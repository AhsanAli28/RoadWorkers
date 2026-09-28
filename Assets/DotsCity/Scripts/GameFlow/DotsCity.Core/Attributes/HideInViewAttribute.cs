using System;

namespace Spirit604.DotsCity.Core
{
    /// <summary>
    /// Hides the variable from the main menu view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HideInViewAttribute : Attribute { }
}