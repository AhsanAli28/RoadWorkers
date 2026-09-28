using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Core")]
[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Editor")]

namespace Spirit604.AnimationBaker.Utils
{
    internal static class CollectionExtension
    {
        public static T GetRandomElement<T>(this IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return default(T);
            }

            return collection[UnityEngine.Random.Range(0, collection.Count)];
        }

        public static T GetRandomElement<T>(this Array collection)
        {
            if (collection == null || collection.Length == 0)
            {
                return default(T);
            }

            return (T)collection.GetValue(UnityEngine.Random.Range(0, collection.Length));
        }

        public static int GetRandomIndex<T>(this IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return -1;
            }

            return UnityEngine.Random.Range(0, collection.Count);
        }

        public static bool TryToAdd<T>(this IList<T> collection, T element, bool autoCreateCollection = false)
        {
            if (collection == null)
            {
                if (!autoCreateCollection)
                {
                    return false;
                }
                else
                {
                    collection = new List<T>();
                }
            }

            if (element != null && !collection.Contains(element))
            {
                collection.Add(element);
                return true;
            }

            return false;
        }

        public static bool TryToAdd<T>(this IList<T> collection, IList<T> newCollection, bool autoCreateCollection = false)
        {
            if (collection == null)
            {
                if (!autoCreateCollection)
                {
                    return false;
                }
                else
                {
                    collection = new List<T>();
                }
            }

            if (newCollection != null)
            {
                for (int i = 0; i < newCollection.Count; i++)
                {
                    collection.TryToAdd(newCollection[i]);
                }

                return true;
            }

            return false;
        }

        public static bool IsEmpty<T>(this IList<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static bool TryToRemove<T>(this IList<T> collection, T element)
        {
            if (collection.IsEmpty())
            {
                return false;
            }

            if (element != null && collection.Contains(element))
            {
                collection.Remove(element);
                return true;
            }

            return false;
        }
    }
}
