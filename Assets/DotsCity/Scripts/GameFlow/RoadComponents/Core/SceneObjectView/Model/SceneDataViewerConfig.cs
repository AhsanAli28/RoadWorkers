using Spirit604.Collections.Dictionary;
using System;
using UnityEngine;

namespace Spirit604.CityEditor
{
    [CreateAssetMenu(fileName = "SceneDataViewerConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_EDITOR_CONFIG_OTHER_PATH + "SceneDataViewerConfig")]
    public class SceneDataViewerConfig : ScriptableObject
    {
        public enum VariableType { Integer, Float, Boolean, Enum }

        [Serializable]
        public class VariableDataDictionary : AbstractSerializableDictionary<string, VariableData> { }

        [Serializable]
        public class VariableData
        {
            public string SerializedParamName;
            public string ViewParamName;
            public string ViewParamShortName;
            public VariableType VariableType;

            public Type Type
            {
                get
                {
                    switch (VariableType)
                    {
                        case VariableType.Integer:
                            return typeof(int);
                        case VariableType.Float:
                            return typeof(float);
                        case VariableType.Boolean:
                            return typeof(bool);
                        case VariableType.Enum:
                            return typeof(Enum);
                    }

                    return default;
                }
            }
        }

        [SerializeField]
        private VariableDataDictionary variableDataDict = new VariableDataDictionary();

        public VariableDataDictionary VariableDataDict { get => variableDataDict; }
    }
}