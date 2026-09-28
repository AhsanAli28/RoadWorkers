using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.Collections.Dictionary.Editor
{
    [CustomPropertyDrawer(typeof(BaseSerializableDictionary), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        private const float LabelValueMaxWidth = 100f;
        private const int MaxValidateEntryLimit = 100;
        private const float HelpBoxHeight = 45f;
        private const float RelativeWidthLabelMultiplier = 0.75f;
        private const float ValuePadding = 30f;
        private const float RemoveEntryButtonSize = 20f;

        private const string k_newKey = "newKey";
        private const string k_Keys = "keys";
        private const string k_Values = "values";
        private const string k_showContent = "showContent";
        private const string k_initPages = "initPages";
        private const string k_showPages = "showPages";
        private const string k_dirty = "dirty";

        private const string k_keyName = "keyName";
        private const string k_valueName = "valueName";
        private const string k_newKeyName = "newKeyName";

        private static GUIContent c_add = new GUIContent("\u2732", "Add");
        private static GUIContent c_remove = new GUIContent("\u2573");//, "Remove"); //removed the tooltip since it usually obscures the button

        private static Color color_background = Color.white;

        private static Dictionary<string, bool> hasPropertyDrawerCache = new Dictionary<string, bool>();

        //incase you don't have the skin package you can use these styles instead, they're pretty close
        public readonly GUIStyle s_bigButton = "LargeButton";
        public readonly GUIStyle s_card = "sv_iconselector_labelselection";
        public readonly GUIStyle s_flatBox = "IN BigTitle";
        public readonly GUIStyle s_footerBackground = "InnerShadowBg";
        public readonly GUIStyle s_leftButton = "ButtonLeft";
        public readonly GUIStyle s_rightButton = "ButtonRight";
        public readonly GUIStyle s_title = "PreOverlayLabel";

        private GUIContent c_key = new GUIContent(string.Empty);
        private GUIContent c_val = new GUIContent(string.Empty);
        private GUIContent c_newKey = new GUIContent(string.Empty);

        private SerializedProperty p_keys;
        private SerializedProperty p_values;
        private SerializedProperty p_newKey;
        private SerializedProperty p_showContent;
        private SerializedProperty p_initPages;
        private SerializedProperty p_showPages;
        private SerializedProperty p_dirtyProp;

        private SerializedProperty p_keyName;
        private SerializedProperty p_valueName;
        private SerializedProperty p_newKeyName;

        private int selectedIndex = -1;
        private int removeIndex = -1;
        private float pagesSliderHeight = 0;
        private float rowHeight = 0;
        private float dictionaryHeight = 0;
        private int selectedPageIndex;
        private bool labelSizeInit = false;
        private static float currentLabelSize;
        private bool keyDuplicateConflict;
        private bool init = false;
        private int initHash;
        private bool keyIsEnum = false;
        private string[] availableEnumDisplayNames;

        private ReorderableList reorderableList;
        private IList objectPage;
        private int maxPageSize = 0;

        private int ItemsPerPage => SerializableDictionaryConstans.ItemsPerPageCount;

        private bool ShowPages => p_showPages.boolValue;

        private bool RuntimeValidation => p_keys != null && p_keys.arraySize < MaxValidateEntryLimit;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            pagesSliderHeight = 0;
            rowHeight = EditorGUIUtility.singleLineHeight + 8f;// height per row
            dictionaryHeight = 0;

            float topHeight = rowHeight;
            float addRowHeight = 0;
            float endMargin = 5f; // pixel margin to separate this propertydrawer from the next

            //heights for each row
            if (p_showContent != null && p_showContent.boolValue)
            {
                p_keys = property.FindPropertyRelative(k_Keys);
                p_values = property.FindPropertyRelative(k_Values);
                p_showPages = property.FindPropertyRelative(k_showPages);
                p_dirtyProp = property.FindPropertyRelative(k_dirty);

                if (p_values?.arraySize > 0)
                {
                    int minIndex = 0;
                    int maxIndex = p_values.arraySize;

                    if (ShowPages)
                    {
                        minIndex = selectedPageIndex * ItemsPerPage;
                        maxIndex = Mathf.Min(minIndex + ItemsPerPage, p_values.arraySize);
                    }

                    for (int i = minIndex; i < maxIndex; i++)
                    {
                        var keyProp = p_keys.GetArrayElementAtIndex(i);
                        var valueProp = p_values.GetArrayElementAtIndex(i);

                        if (valueProp != null)
                        {
                            var dictionaryRowHeight = GetValuePropSize(keyProp, valueProp) + 2f;
                            dictionaryHeight += dictionaryRowHeight;
                        }
                    }
                }

                currentLabelSize = LabelValueMaxWidth;

                if (!labelSizeInit && p_values.arraySize > 0)
                {
                    var valueProp = p_values.GetArrayElementAtIndex(0);

                    var currentProperty = valueProp.Copy();
                    var nextElement = valueProp.Copy();

                    currentLabelSize = 0;
                    bool generic = false;
                    int index = 0;

                    nextElement.NextVisible(false);

                    while (currentProperty.NextVisible(true))
                    {
                        if ((SerializedProperty.EqualContents(currentProperty, nextElement)))
                        {
                            break;
                        }

                        if (valueProp.propertyType == SerializedPropertyType.Generic)
                        {
                            generic = true;
                        }

                        if (!generic)
                        {
                            var labelSize = EditorStyles.label.CalcSize(new GUIContent(valueProp.displayName)).x + 5f;
                            currentLabelSize = Mathf.Max(labelSize, currentLabelSize);
                        }

                        index++;
                    }

                    currentLabelSize = Mathf.Min(currentLabelSize, LabelValueMaxWidth);
                    labelSizeInit = true;
                }

                topHeight -= 3;
                addRowHeight = rowHeight;

                if (ShowPages)
                {
                    pagesSliderHeight += 25f;// add pages slider height
                }
            }

            float helpBox = 0;

            if (keyDuplicateConflict)
            {
                helpBox = HelpBoxHeight;
            }

            return topHeight + dictionaryHeight + addRowHeight + pagesSliderHeight + endMargin + helpBox;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Color backColor = GUI.backgroundColor;
            GUI.backgroundColor = color_background;
            removeIndex = -1;

            float width = position.width - 10f;

            Rect r_drawSelection = new Rect(position.xMin + 5, position.yMin, rowHeight, rowHeight);
            Rect r_expanded = new Rect(r_drawSelection.xMax, position.yMin, width - rowHeight, rowHeight);
            Rect r_list = new Rect(position.xMin + 5, r_expanded.yMax, width, dictionaryHeight);
            Rect r_addRow = new Rect(r_list.xMin, r_list.yMax, width, rowHeight);
            Rect r_pageSlider = new Rect(r_list.xMin, r_addRow.yMax + 5f, width, pagesSliderHeight);
            Rect r_Helpbox = new Rect(r_list.xMin, r_pageSlider.yMax + 5f, width, HelpBoxHeight);

            if (ShowPages && p_showContent != null && p_showContent.boolValue)
            {
                if (p_keys.arraySize > 0)
                {
                    maxPageSize = CalcMaxPageSize(p_keys.arraySize);
                }

                GUI.enabled = maxPageSize != 0;

                EditorGUI.BeginChangeCheck();

                var currentSelectedPageIndex = EditorGUI.IntSlider(r_pageSlider, "Page", selectedPageIndex, 0, maxPageSize);

                if (EditorGUI.EndChangeCheck())
                {
                    if (currentSelectedPageIndex != selectedPageIndex)
                    {
                        selectedPageIndex = currentSelectedPageIndex;
                        TryToInitList(true, false);
                    }
                }

                GUI.enabled = true;
            }

            CheckForReset();

            EditorGUI.BeginChangeCheck();

            p_showPages.boolValue = GUI.Toggle(r_drawSelection, p_showPages.boolValue, "P", s_leftButton);

            if (EditorGUI.EndChangeCheck())
            {
                TryToInitList(true);
                p_showPages.serializedObject.ApplyModifiedProperties();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            {
                if (reorderableList.index >= 0 && p_keys.arraySize > reorderableList.index)
                {
                    removeIndex = GetGlobalObjectIndexFromLocalPage(reorderableList.index);
                }
            }

            string headerText = string.Empty;
            string headerName = label.text;

            if (!ShowPages || p_keys.arraySize == 0)
            {
                headerText = headerName + " (" + p_keys.arraySize + " items)";
            }
            else
            {
                int minIndex = selectedPageIndex * ItemsPerPage + 1;
                int maxIndex = Mathf.Min(minIndex + ItemsPerPage - 1, p_values.arraySize);

                var pageText = $"{minIndex}-{maxIndex}/";
                headerText = headerName + $" ({pageText}" + p_keys.arraySize + $" items) [{selectedPageIndex}/{maxPageSize}]";
            }

            var c_header = new GUIContent(headerText, label.image, label.tooltip);

            if (GUI.Button(r_expanded, c_header, s_rightButton)) p_showContent.boolValue = !p_showContent.boolValue;

            if (p_showContent.boolValue)
            {
                GUI.Box(r_list, GUIContent.none, s_flatBox);

                c_key.text = p_keyName.stringValue;
                c_val.text = p_valueName.stringValue;
                c_newKey.text = p_newKeyName.stringValue;

                p_values.arraySize = p_keys.arraySize;

                reorderableList.DoList(r_list);

                OnDrawAddRow(r_addRow, property);

                if (removeIndex > -1)
                {
                    if (selectedIndex == removeIndex)
                        selectedIndex = -1;
                    else if (selectedIndex > removeIndex)
                        selectedIndex--;

                    var previousPageSize = CalcMaxPageSize(p_keys.arraySize);
                    p_keys.DeleteArrayElementAtIndex(removeIndex);
                    p_values.DeleteArrayElementAtIndex(removeIndex);
                    var currentPageSize = CalcMaxPageSize(p_keys.arraySize);

                    if (ShowPages && currentPageSize < previousPageSize && selectedPageIndex == previousPageSize)
                    {
                        selectedPageIndex = currentPageSize;
                    }

                    MakeDirty();
                    InitEnumDisplayNames();
                }

                if (keyDuplicateConflict)
                {
                    EditorGUI.HelpBox(r_Helpbox, "Duplicate key found!", MessageType.Warning);
                }
            }

            EditorGUI.indentLevel = oldIndent;
            GUI.backgroundColor = backColor;
        }

        private void Init(SerializedProperty property)
        {
            p_keys = property.FindPropertyRelative(k_Keys);
            p_showContent = property.FindPropertyRelative(k_showContent);
            p_initPages = property.FindPropertyRelative(k_initPages);
            p_showPages = property.FindPropertyRelative(k_showPages);

            p_values = property.FindPropertyRelative(k_Values);
            p_newKey = property.FindPropertyRelative(k_newKey);

            p_keyName = property.FindPropertyRelative(k_keyName);
            p_valueName = property.FindPropertyRelative(k_valueName);
            p_newKeyName = property.FindPropertyRelative(k_newKeyName);

            var hash = property.GetHashCode();
            var diffHash = hash != initHash;
            bool forceInit = false;

            if (!init || diffHash)
            {
                forceInit = diffHash;
                init = true;
                initHash = hash;
                keyIsEnum = p_newKey.propertyType == SerializedPropertyType.Enum;

                if (keyIsEnum)
                {
                    InitEnumDisplayNames();
                }
            }

            if (!p_initPages.boolValue)
            {
                p_initPages.boolValue = true;

                if (p_keys.arraySize >= MaxValidateEntryLimit)
                {
                    p_showPages.boolValue = true;
                }
                else
                {
                    p_showPages.boolValue = SerializableDictionaryConstans.ShowPages;
                }

                EditorUtility.SetDirty(p_showPages.serializedObject.targetObject);
                p_showPages.serializedObject.ApplyModifiedProperties();
            }

            TryToInitList(forceInit, false);
        }

        private void InitEnumDisplayNames()
        {
            if (!keyIsEnum)
            {
                return;
            }

            if (p_keys.arraySize > 0)
            {
                List<string> takenNames = new List<string>();

                for (int i = 0; i < p_keys.arraySize; i++)
                {
                    var key = p_keys.GetArrayElementAtIndex(i);

                    if (p_newKey.enumDisplayNames.Length > key.enumValueIndex && key.enumValueIndex >= 0)
                        takenNames.Add(p_newKey.enumDisplayNames[key.enumValueIndex]);
                }

                availableEnumDisplayNames = p_newKey.enumDisplayNames.Except(takenNames).ToArray();
            }
            else
            {
                availableEnumDisplayNames = p_newKey.enumDisplayNames;
            }
        }

        private void CheckForReset()
        {
            if (ShowPages && reorderableList != null)
            {
                if (reorderableList.count == 0 && p_keys.arraySize > 0)
                {
                    selectedPageIndex = 0;
                }

                if ((reorderableList.count > ItemsPerPage || (ItemsPerPage > reorderableList.count && reorderableList.count < p_keys.arraySize)))
                {
                    TryToInitList(true);
                }
            }

            if (p_showPages.boolValue != ShowPages)
            {
                p_showPages.boolValue = ShowPages;
                TryToInitList(true);
            }
        }

        private float GetValuePropSize(SerializedProperty keyProp, SerializedProperty valueProp)
        {
            if (valueProp == null)
            {
                return 0;
            }

            bool isGeneric = false;

            if (valueProp.propertyType == SerializedPropertyType.Generic)
            {
                isGeneric = !HasCustomPropertyDrawer(valueProp);
            }

            if (!isGeneric)
                return EditorGUI.GetPropertyHeight(valueProp) + 8;

            SerializedProperty currentProperty = valueProp.Copy();
            var propCount = currentProperty.CountInProperty();
            currentProperty = valueProp.Copy();

            if (propCount > 1)
            {
                float height = EditorGUI.GetPropertyHeight(keyProp, true);
                height += 10;

                SerializedProperty nextElement = valueProp.Copy();

                currentProperty.NextVisible(true);
                nextElement.NextVisible(false);

                while (currentProperty.NextVisible(true))
                {
                    if ((SerializedProperty.EqualContents(currentProperty, nextElement)))
                    {
                        break;
                    }
                    else
                    {
                        height += EditorGUI.GetPropertyHeight(currentProperty, true) + EditorGUIUtility.standardVerticalSpacing * 2;
                    }
                }

                return height;
            }

            return EditorGUIUtility.singleLineHeight + 8f;
        }

        private void InitPage()
        {
            List<object> list = new List<object>();

            var startIndex = selectedPageIndex * ItemsPerPage;
            var index = startIndex;

            while (startIndex + ItemsPerPage > index)
            {
                if (index < p_keys.arraySize)
                {
                    var key = p_keys.GetArrayElementAtIndex(index);
                    var keyValue = key.GetPropertyValue();

                    list.Add(keyValue);
                }
                else
                {
                    break;
                }

                index++;
            }

            objectPage = list;
        }

        private void TryToInitList(bool forceInit = false, bool makeDirty = true)
        {
            if (reorderableList != null && !forceInit)
            {
                return;
            }

            if (!ShowPages)
            {
                reorderableList = new ReorderableList(p_keys.serializedObject, p_keys, true, false, false, false);
            }
            else
            {
                InitPage();
                Type type = default;

                if (p_keys.arraySize > 0)
                {
                    type = p_keys.GetArrayElementAtIndex(0).GetType();
                }

                reorderableList = new ReorderableList(objectPage, type, true, false, false, false);
            }

            reorderableList.onReorderCallbackWithDetails += (list, oldIndex, newIndex) =>
            {
                if (ShowPages)
                {
                    oldIndex = GetGlobalObjectIndexFromLocalPage(oldIndex);
                    newIndex = GetGlobalObjectIndexFromLocalPage(newIndex);

                    p_keys.MoveArrayElement(oldIndex, newIndex);
                }

                p_values.MoveArrayElement(oldIndex, newIndex);
                MakeDirty();
            };

            reorderableList.elementHeightCallback += (index) =>
            {
                if (p_keys.arraySize > 0)
                {
                    var globalIndex = GetGlobalObjectIndexFromLocalPage(index);

                    if (globalIndex < p_keys.arraySize)
                    {
                        var keyProp = p_keys.GetArrayElementAtIndex(globalIndex);
                        var valueProp = p_values.GetArrayElementAtIndex(globalIndex);
                        return GetValuePropSize(keyProp, valueProp);
                    }
                }

                return 0;
            };

            reorderableList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                var keyIndex = GetGlobalObjectIndexFromLocalPage(index);

                if (p_keys.arraySize > keyIndex && keyIndex >= 0)
                {
                    SerializedProperty keyProp = p_keys.GetArrayElementAtIndex(keyIndex);
                    SerializedProperty valueProp = p_values.GetArrayElementAtIndex(keyIndex);

                    var r = rect;
                    r.y += 1.0f;
                    r.x += 10.0f;
                    var sourceRowWidth = r.width;

                    r.width = sourceRowWidth / 2 - currentLabelSize * RelativeWidthLabelMultiplier - 10f;

                    DrawKey(keyProp, r, checkForChange: true);

                    r.width = sourceRowWidth;

                    r.width += currentLabelSize * RelativeWidthLabelMultiplier;
                    r.x -= currentLabelSize * RelativeWidthLabelMultiplier;

                    r.x += sourceRowWidth / 2;

                    float currentWidth = (sourceRowWidth / 2 + ValuePadding);
                    r.width -= currentWidth;

                    DrawValueProperty(valueProp, r);

                    r.y -= 2;
                    r.x += r.width + 2f;
                    r.width = RemoveEntryButtonSize;
                    r.height = RemoveEntryButtonSize;

                    if (GUI.Button(r, c_remove))
                    {
                        removeIndex = keyIndex;
                    }
                }
            };

            InitEnumDisplayNames();

            if (makeDirty)
            {
                MakeDirty();
            }
        }

        private int GetGlobalObjectIndexFromLocalPage(int index)
        {
            if (ShowPages)
            {
                index = ItemsPerPage * selectedPageIndex + index;
            }

            return index;
        }

        private void DrawKey(SerializedProperty property, Rect rect, GUIContent gUIContent = null, bool checkForChange = false, bool addKeyField = false)
        {
            if (checkForChange)
            {
                EditorGUI.BeginChangeCheck();
            }

            GUIContent currentGUIContent = GUIContent.none;

            if (gUIContent != null)
            {
                currentGUIContent = gUIContent;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Enum:
                    {
                        var enumNames = property.enumDisplayNames;

                        string selectedEnumValue = string.Empty;

                        if (enumNames.Length > property.enumValueIndex && property.enumValueIndex >= 0)
                        {
                            selectedEnumValue = enumNames[property.enumValueIndex];
                        }

                        var selectedEnumLabel = selectedEnumValue;

                        if (addKeyField && availableEnumDisplayNames.Length == 0)
                        {
                            selectedEnumLabel = string.Empty;
                        }

                        //Draw button with dropdown style
                        if (GUI.Button(rect, selectedEnumLabel, EditorStyles.layerMaskField))
                        {
                            GenericMenu menu = new GenericMenu();

                            for (int i = 0; i < enumNames.Length; i++)
                            {
                                int nameIndex = i;

                                //If the value is being used, show it disabled

                                var isDisabled = !availableEnumDisplayNames.Contains(enumNames[nameIndex]) && (!enumNames[nameIndex].Equals(selectedEnumValue) || addKeyField);

                                if (isDisabled)
                                {
                                    menu.AddDisabledItem(new GUIContent(enumNames[nameIndex]));
                                }
                                else
                                {
                                    var selected = selectedEnumValue == enumNames[nameIndex];

                                    menu.AddItem(new GUIContent(enumNames[nameIndex]), selected, () =>
                                    {
                                        property.enumValueIndex = nameIndex;
                                        property.serializedObject.ApplyModifiedProperties();
                                        InitEnumDisplayNames();
                                    });
                                }
                            }

                            //Show menu under mouse position
                            menu.ShowAsContext();

                            Event.current.Use();
                        }

                        break;
                    }
                default:
                    {
                        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property, currentGUIContent, true);
                        break;
                    }
            }

            if (checkForChange && EditorGUI.EndChangeCheck())
            {
                int hasValueCounter = 0;
                var hasDuplicateKey = false;

                for (int i = 0; i < p_keys.arraySize; i++)
                {
                    var val = p_keys.GetArrayElementAtIndex(i);

                    if (val.IsEqual(property))
                    {
                        hasValueCounter++;

                        if (hasValueCounter >= 2)
                        {
                            hasDuplicateKey = true;
                            break;
                        }
                    }
                }

                if (hasDuplicateKey)
                {
                    keyDuplicateConflict = true;
                }
                else
                {
                    keyDuplicateConflict = false;
                    MakeDirty();
                }
            }
        }

        private void DrawValueProperty(SerializedProperty property, Rect rect)
        {
            EditorGUI.BeginChangeCheck();

            bool drawGeneric = false;

            if (property.propertyType == SerializedPropertyType.Generic)
            {
                drawGeneric = !HasCustomPropertyDrawer(property);
            }

            if (drawGeneric)
            {
                var previousLabelWidth = EditorGUIUtility.labelWidth;
                property.isExpanded = true;

                bool first = true;

                EditorGUIUtility.labelWidth = currentLabelSize;

                var nextElement = property.Copy();
                bool hasNextElement = nextElement.NextVisible(false);
                if (!hasNextElement)
                {
                    nextElement = null;
                }

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
                    {
                        break;
                    }
                    else
                    {
                        if (!first)
                        {
                            rect.y += (EditorGUIUtility.singleLineHeight + 4);
                        }
                        else
                        {
                            first = false;
                        }
                    }

                    if ((SerializedProperty.EqualContents(property, nextElement)))
                    {
                        break;
                    }

                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), property, false);
                }

                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
            else
            {
                var height = EditorGUI.GetPropertyHeight(property);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, height), property, GUIContent.none, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                MakeDirty();
            }
        }

        private static bool HasCustomPropertyDrawer(SerializedProperty property)
        {
            if (hasPropertyDrawerCache.ContainsKey(property.propertyPath))
                return hasPropertyDrawerCache[property.propertyPath];

            var getHandler = Type.GetType("UnityEditor.ScriptAttributeUtility, UnityEditor").GetMethod("GetHandler", BindingFlags.NonPublic | BindingFlags.Static);

            var result = getHandler.Invoke(null, new object[] { property });

            var field = result.GetType().GetProperty("hasPropertyDrawer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var hasPropertyDrawer = (bool)field.GetValue(result);

            hasPropertyDrawerCache.Add(property.propertyPath, hasPropertyDrawer);

            return hasPropertyDrawer;
        }

        private void OnDrawAddRow(Rect rect, SerializedProperty property)
        {
            Rect r_backrow = new Rect(rect.xMin, rect.yMin, rect.width, rect.height);

            if (Event.current.type == EventType.Repaint)
                s_footerBackground.Draw(r_backrow, GUIContent.none, false, false, false, false);

            Rect r_key = new Rect(rect.xMin + 5, rect.yMin + 2, rect.width - 35f, rect.height - 8);
            Rect r_newKey = new Rect(r_key.xMax + 5, rect.yMin + 2, 20f, rect.height - 8);

            bool isInvalid = RuntimeValidation && p_keys.Contains(p_newKey);

            if (!isInvalid && keyIsEnum && (availableEnumDisplayNames == null || availableEnumDisplayNames.Length == 0))
            {
                isInvalid = true;
            }

            DrawKey(p_newKey, r_key, gUIContent: c_newKey, addKeyField: true);

            EditorGUI.BeginDisabledGroup(isInvalid);

            if (GUI.Button(r_newKey, c_add))
            {
                bool canAdd = true;

                if (!RuntimeValidation)
                {
                    canAdd = !p_keys.Contains(p_newKey);
                }

                if (canAdd)
                {
                    var previousPageSize = CalcMaxPageSize(p_keys.arraySize);
                    p_keys.arraySize++;
                    p_values.arraySize = p_keys.arraySize;

                    p_keys.GetArrayElementAtIndex(p_keys.arraySize - 1).SetPropertyValue(p_newKey);
                    p_values.GetArrayElementAtIndex(p_keys.arraySize - 1).SetPropertyValue(null);
                    p_newKey.SetPropertyValue(null);

                    if (ShowPages)
                    {
                        var currentPageSize = CalcMaxPageSize(p_keys.arraySize);

                        if (currentPageSize > previousPageSize && selectedPageIndex == previousPageSize)
                        {
                            selectedPageIndex = currentPageSize;
                        }
                    }

                    TryToInitList(true);
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private int CalcMaxPageSize(int arraySize)
        {
            var maxPageSize = (Mathf.FloorToInt((float)(arraySize - 1) / ItemsPerPage));
            return maxPageSize;
        }

        private void MakeDirty()
        {
            if (p_dirtyProp != null)
            {
                p_dirtyProp.boolValue = true;
            }
        }
    }
}