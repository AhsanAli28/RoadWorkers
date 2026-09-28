#if FCG
using Spirit604.Extensions;
using System;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public static class FCGUtils
    {
        public static Collider[] GetSurfaceColliders(Transform parent = null)
        {
            Func<Collider, bool> predicate = GetGroundPredicate();

            if (parent == null)
            {
                return ObjectUtils.FindObjectsOfType<Collider>().Where(predicate).ToArray();
            }
            else
                return parent.GetComponentsInChildren<Collider>().Where(predicate).ToArray();
        }

        public static Func<Collider, bool> GetGroundPredicate()
        {
            return a => a.name.Contains("Collider") && (a.name.Contains("Road") || a.name.Contains("BD") || a.name.Contains("Quad"));
        }
    }
}
#endif