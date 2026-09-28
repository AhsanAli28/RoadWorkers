using System;
using UnityEngine;

namespace Spirit604.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DocLinkerAttribute : PropertyAttribute
    {
        public readonly string Link;
        public readonly float Offset;

        public DocLinkerAttribute(string link, float offset = 0)
        {
            Link = link;
            Offset = offset;
        }
    }
}
