using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class TransformExtensions
    {
        public static Vector3 FlatPosition(this Transform transform)
        {
            return new Vector3(transform.position.x, 0, transform.position.z);
        }

        public static Transform GetChild(Transform root, string createPath, bool autoCreateDirectory = true, bool recordUndo = false)
        {
            if (string.IsNullOrEmpty(createPath))
            {
                return root;
            }

            Transform newRoot = null;

            createPath = createPath.Trim();

            if (createPath[createPath.Length - 1] == '/')
            {
                createPath = createPath.Substring(0, createPath.Length - 1);
            }
            if (createPath[0] == '/')
            {
                createPath = createPath.Substring(1, createPath.Length - 1);
            }

            Transform newChild = root.Find(createPath);

            if (!newChild)
            {
                if (autoCreateDirectory)
                {
                    Transform previousChild = root.transform;

                    var childs = createPath.Split('/');

                    for (int i = 0; i < childs.Length; i++)
                    {
                        newChild = previousChild.Find(childs[i]);

                        if (!newChild)
                        {
                            newChild = new GameObject(childs[i]).transform;

                            if (recordUndo)
                            {
#if UNITY_EDITOR
                                Undo.RegisterCreatedObjectUndo(newChild.gameObject, $"Created {newChild.name}");
#endif
                            }

                            newChild.parent = previousChild;
                            newChild.transform.localPosition = default;
                            newChild.transform.localRotation = default;
                        }

                        previousChild = newChild;
                    }

                    newRoot = previousChild;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                newRoot = newChild;
            }

            return newRoot;
        }

        public static void TryToSwitchActiveState(this GameObject gameObject, bool newState)
        {
            if (gameObject.activeSelf != newState)
            {
                gameObject.SetActive(newState);
            }
        }

        public static void ClearChilds(Transform parent)
        {
            while (parent?.childCount > 0)
            {
                GameObject.DestroyImmediate(parent.GetChild(0).gameObject);
            }
        }

        public static bool IsChild(GameObject parent, GameObject childObject)
        {
            if (parent == null)
            {
                return false;
            }

            Transform tempParent = childObject.transform.parent;

            while (tempParent != null)
            {
                if (tempParent == parent.transform)
                {
                    return true;
                }
                else
                {
                    tempParent = tempParent.parent;
                }
            }

            return false;
        }
    }
}