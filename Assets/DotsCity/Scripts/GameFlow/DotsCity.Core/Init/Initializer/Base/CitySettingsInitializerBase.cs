using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class CitySettingsInitializerBase : InitializerBase
    {
        public abstract TSettings GetSettings<TSettings>() where TSettings : GeneralSettingDataCore;

        public bool DOTSSimulation => GetSettings<GeneralSettingDataCore>().DOTSSimulation;

#if UNITY_EDITOR
        private static CitySettingsInitializerBase editorInstance;

        public static CitySettingsInitializerBase EditorInstance
        {
            get
            {
                if (editorInstance == null)
                    editorInstance = ObjectUtils.FindObjectOfType<CitySettingsInitializerBase>();

                return editorInstance;
            }
        }

        public static bool DotsSimulationStatic
        {
            get
            {
                var settings = CitySettingsInitializerBase.EditorInstance;

                if (settings != null)
                {
                    return settings.DOTSSimulation;
                }

                return true;
            }
        }
#else
        public static CitySettingsInitializerBase EditorInstance { get; }
        public static bool DotsSimulationStatic => true;
#endif
    }

    public class CitySettingsInitializerBase<T> : CitySettingsInitializerBase where T : GeneralSettingDataCore
    {
        [SerializeField] private T settings;

        public T Settings { get => settings; set => settings = value; }

        public override TSettings GetSettings<TSettings>() => settings as TSettings;
    }
}