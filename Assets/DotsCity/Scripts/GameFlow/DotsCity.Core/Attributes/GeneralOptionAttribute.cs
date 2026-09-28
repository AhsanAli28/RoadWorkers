using System;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    /// <summary>
    /// Editor Property Insert from General Settings Config before the attributed field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GeneralOptionAttribute : PropertyAttribute
    {
        public string PropName { get; private set; }

        public GeneralOptionAttribute(string propName)
        {
            PropName = propName;
        }
    }
}