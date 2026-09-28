using System;
using System.Linq;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class ObjectUtils
    {
        public static T FindObjectOfType<T>(bool includeInactive = false) where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return GameObject.FindAnyObjectByType<T>(!includeInactive ? FindObjectsInactive.Exclude : FindObjectsInactive.Include);

#else
            return GameObject.FindObjectOfType<T>(includeInactive);
#endif
        }

        public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return GameObject.FindObjectsByType<T>(!includeInactive ? FindObjectsInactive.Exclude : FindObjectsInactive.Include, FindObjectsSortMode.None);

#else
            return GameObject.FindObjectsOfType<T>(includeInactive);
#endif
        }

        public static Component FindObjectOfType(Type type, bool includeInactive = false)
        {
            UnityEngine.Object obj = null;

#if UNITY_2023_1_OR_NEWER
            obj = GameObject.FindAnyObjectByType(type, !includeInactive ? FindObjectsInactive.Exclude : FindObjectsInactive.Include);

#else
            obj = GameObject.FindObjectOfType(type, includeInactive);
#endif

            if (obj != null)
            {
                return obj as Component;
            }

            return null;
        }

        public static Component[] FindObjectsOfType(Type type, bool includeInactive = false)
        {
#if UNITY_2023_1_OR_NEWER
            return GameObject.FindObjectsByType(type, !includeInactive ? FindObjectsInactive.Exclude : FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().ToArray();

#else
            return GameObject.FindObjectsOfType(type, includeInactive).Cast<Component>().ToArray();
#endif
        }
    }
}
