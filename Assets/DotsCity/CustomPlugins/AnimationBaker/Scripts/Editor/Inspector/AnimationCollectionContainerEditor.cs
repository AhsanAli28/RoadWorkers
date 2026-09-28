using UnityEditor;
using UnityEditorInternal;

namespace Spirit604.AnimationBaker
{
    [CustomEditor(typeof(AnimationCollectionContainer))]
    public class AnimationCollectionContainerEditor : Editor
    {
        private const float LabelMargin = 2f;

        private ReorderableList reorderableList;
        private AnimationCollectionContainer animationCollectionContainer;
        private SerializedProperty showGuidsProp;
        private SerializedProperty targetListProp;

        private bool ShowGuids => showGuidsProp != null && showGuidsProp.boolValue;

        private void OnEnable()
        {
            Init();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (reorderableList == null)
            {
                Init();
                return;
            }

            EditorGUILayout.PropertyField(showGuidsProp);

            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void Init()
        {
            animationCollectionContainer = target as AnimationCollectionContainer;
            showGuidsProp = serializedObject.FindProperty("showGuids");
            targetListProp = serializedObject.FindProperty("animations");

            reorderableList = new ReorderableList(serializedObject, targetListProp, true, false, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var arrayElement = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.height = GetFieldSize();

                    EditorGUI.PropertyField(rect, arrayElement.FindPropertyRelative("Name"));

                    var uniqueAnimationProp = arrayElement.FindPropertyRelative("UniqueAnimation");

                    rect.y += GetFieldOffset();

                    EditorGUI.PropertyField(rect, uniqueAnimationProp);

                    if (uniqueAnimationProp.boolValue)
                    {
                        rect.y += GetFieldOffset();
                        EditorGUI.PropertyField(rect, arrayElement.FindPropertyRelative("AllowDuplicate"));

                        rect.y += GetFieldOffset();
                        EditorGUI.PropertyField(rect, arrayElement.FindPropertyRelative("InstanceCount"));
                    }

                    if (ShowGuids)
                    {
                        rect.y += GetFieldOffset();
                        EditorGUI.PropertyField(rect, arrayElement.FindPropertyRelative("Guid"));
                    }

                    rect.y += GetFieldOffset();
                    EditorGUI.PropertyField(rect, arrayElement.FindPropertyRelative("AnimationType"));

                },
                onRemoveCallback = (list) =>
                {
                    var removedIndex = list.index;
                    targetListProp.DeleteArrayElementAtIndex(removedIndex);
                },
                onAddCallback = (list) =>
                {
                    targetListProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                    animationCollectionContainer.AddGuid(targetListProp.arraySize - 1);
                },
                elementHeightCallback = (index) =>
                {
                    var arrayElement = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    var uniqueAnimationProp = arrayElement.FindPropertyRelative("UniqueAnimation");

                    int fieldsCount = 3;

                    if (ShowGuids)
                    {
                        fieldsCount++;
                    }

                    if (uniqueAnimationProp.boolValue)
                    {
                        fieldsCount += 2;
                    }

                    float height = GetFieldOffset() * fieldsCount;

                    return height;
                },
            };
        }

        private float GetFieldSize() => EditorGUIUtility.singleLineHeight;

        private float GetFieldOffset() => GetFieldSize() + LabelMargin;
    }
}