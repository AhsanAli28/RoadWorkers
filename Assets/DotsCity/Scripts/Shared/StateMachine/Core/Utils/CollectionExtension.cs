using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.StateMachine.Utils
{
    public static class CollectionExtension
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

        public static void Add<T>(ref T[] array, T t)
        {
            if (array == null)
            {
                array = new T[0];
            }

            int newSize = array.Length + 1;
            Array.Resize(ref array, newSize);
            array[newSize - 1] = t;
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

        public static void ClearNullObjects<T>(this List<T> collection) where T : class
        {
            if (collection == null)
            {
                return;
            }

            //collection = collection.Where(item => item != null).ToList();

            int index = 0;

            if (collection.Count > index)
            {
                var t = collection[index];

                if (t != null)
                {
                    index++;
                }
                else
                {
                    collection.RemoveAt(index);
                }
            }
        }

        public static void DestroyGameObjects<T>(this IList<T> collection, bool recordUndo = false) where T : Component
        {
            if (collection == null)
            {
                return;
            }

            while (collection.Count > 0)
            {
                if (collection[0] != null)
                {
                    if (!recordUndo)
                    {
                        GameObject.DestroyImmediate(collection[0].gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.DestroyObjectImmediate(collection[0].gameObject);
#endif
                    }
                }

                collection.RemoveAt(0);
            }

            collection.Clear();
        }

        public static void DestroyGameObjects(this IList<GameObject> collection, bool recordUndo = false)
        {
            if (collection == null)
            {
                return;
            }

            while (collection.Count > 0)
            {
                if (collection[0] != null)
                {
                    if (!recordUndo)
                    {
                        GameObject.DestroyImmediate(collection[0].gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.DestroyObjectImmediate(collection[0].gameObject);
#endif
                    }
                }
                else
                {
                    collection.RemoveAt(0);
                }
            }

            collection.Clear();
        }
    }
}
