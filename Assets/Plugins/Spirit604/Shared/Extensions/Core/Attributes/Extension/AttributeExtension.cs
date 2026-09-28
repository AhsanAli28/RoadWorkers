using System;
using System.Linq;
using System.Reflection;

namespace Spirit604.Attributes
{
    public static class AttributeExtension
    {
        public static T GetAttribute<T>(FieldInfo fieldInfo) where T : Attribute
        {
            if (fieldInfo != null)
            {
                var attr = fieldInfo.GetCustomAttributes(typeof(T), true).FirstOrDefault();

                if (attr != null)
                {
                    return attr as T;
                }
            }

            return null;
        }

        public static T GetAttribute<T>(MethodInfo methodInfo) where T : Attribute
        {
            if (methodInfo != null)
            {
                var attr = methodInfo.GetCustomAttributes(typeof(T), true).FirstOrDefault();

                if (attr != null)
                {
                    return attr as T;
                }
            }

            return null;
        }
    }
}
