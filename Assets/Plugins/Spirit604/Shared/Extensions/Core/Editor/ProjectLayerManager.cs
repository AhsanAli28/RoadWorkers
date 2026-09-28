#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.Extensions
{
    public static class ProjectLayerManager
    {
        private static SerializedObject serializedObject;
        private static SerializedProperty layers;

        public static void AddLayerAt(int index, string layerName, bool overrideLayer = false, Action<int, string> onDuplicateLayer = null, bool tryOtherIndex = false, Action<int, string> onNewIndexAssigned = null)
        {
            if (serializedObject == null || layers == null)
            {
                Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

                if (asset != null && asset.Length > 0)
                {
                    serializedObject = new SerializedObject(asset[0]);
                    serializedObject.Update();
                    layers = serializedObject.FindProperty("layers");
                }
            }

            if (layers != null)
            {
                serializedObject.Update();
                AddLayerAt(layers, index, layerName, overrideLayer, onDuplicateLayer, tryOtherIndex, onNewIndexAssigned);
            }
        }

        private static void AddLayerAt(SerializedProperty layers, int index, string layerName, bool overrideLayer = false, Action<int, string> onDuplicateLayer = null, bool tryOtherIndex = false, Action<int, string> onNewIndexAssigned = null)
        {
            // Skip if a layer with the name already exists.
            for (int i = 0; i < layers.arraySize; ++i)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    Debug.Log("Skipping layer '" + layerName + "' because it already exists.");
                    return;
                }
            }

            // Extend layers if necessary
            if (index >= layers.arraySize)
                layers.arraySize = index + 1;

            var existLayerIndex = LayerMask.NameToLayer(layerName);

            if (existLayerIndex > 0 && onDuplicateLayer != null)
            {
                onDuplicateLayer.Invoke(index, layerName);
                return;
            }

            // set layer name at index
            var element = layers.GetArrayElementAtIndex(index);

            if (string.IsNullOrEmpty(element.stringValue) || overrideLayer)
            {
                element.stringValue = layerName;
                Debug.Log("Added layer '" + layerName + "' at index " + index + ".");
            }
            else
            {
                Debug.LogWarning("Could not add layer at index " + index + " because there already is another layer '" + element.stringValue + "'.");

                if (tryOtherIndex)
                {
                    // Go up in layer indices and try to find an empty spot.
                    for (int i = index + 1; i < 32; ++i)
                    {
                        // Extend layers if necessary
                        if (i >= layers.arraySize)
                            layers.arraySize = i + 1;

                        element = layers.GetArrayElementAtIndex(i);
                        if (string.IsNullOrEmpty(element.stringValue))
                        {
                            element.stringValue = layerName;
                            onNewIndexAssigned?.Invoke(i, layerName);

                            Debug.Log("Added layer '" + layerName + "' at index " + i + " instead of " + index + ".");
                            return;
                        }
                    }

                    Debug.LogError("Could not add layer " + layerName + " because there is no space left in the layers array.");
                }
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}
#endif