using Spirit604.Collections.Dictionary;
using System;
using System.Collections.Generic;

namespace Spirit604.MainMenu.Model
{
    [Serializable]
    public class PlayerMenuSettings
    {
        [Serializable]
        public class ParamDataDictionary : AbstractSerializableDictionary<string, ParamData> { }

        public int SelectedSceneIndex;
        public int SelectedToolbarIndex;

        public ParamDataDictionary Params = new ParamDataDictionary();
        public List<string> ConfigKeys = new List<string>();

        public ParamData TryToGetParam(string configName, string paramName)
        {
            var key = GetKey(configName, paramName);

            if (Params.TryGetValue(key, out var data))
            {
                return data;
            }

            return null;
        }

        public void SaveData(ParamData paramData)
        {
            var key = GetKey(paramData);

            if (!ConfigKeys.Contains(paramData.ConfigName))
            {
                ConfigKeys.Add(paramData.ConfigName);
            }

            if (!HasParam(key))
            {
                Params.Add(key, paramData);
            }
            else
            {
                Params[key] = paramData;
            }
        }

        public void RemoveKey(string configName, string paramName, bool fullRemove = true)
        {
            var key = GetKey(configName, paramName);

            if (Params.ContainsKey(key))
            {
                Params.Remove(key);
            }

            if (fullRemove)
            {
                if (ConfigKeys.Contains(configName))
                {
                    ConfigKeys.Remove(configName);
                }
            }
            else
            {
                bool hasKey = false;

                foreach (var item in Params)
                {
                    if (item.Key.Contains(configName))
                    {
                        hasKey = true;
                        break;
                    }
                }

                if (!hasKey)
                {
                    if (ConfigKeys.Contains(configName))
                    {
                        ConfigKeys.Remove(configName);
                    }
                }
            }
        }

        public bool HasParam(string configName, string paramName) => HasParam(GetKey(configName, paramName));

        public bool HasParam(string key) => Params.ContainsKey(key);

        public bool HasConfig(string configName) => ConfigKeys.Contains(configName);

        public string GetKey(ParamData paramData) => GetKey(paramData.ConfigName, paramData.ParamName);

        public string GetKey(string configName, string paramName) => $"{configName}:{paramName}";
    }
}