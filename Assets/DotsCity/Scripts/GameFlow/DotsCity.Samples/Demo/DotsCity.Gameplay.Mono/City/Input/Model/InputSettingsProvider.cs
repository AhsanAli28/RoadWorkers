using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.InputService
{
    [CreateAssetMenu(fileName = "InputSettingsProvider", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_EDITOR_CONFIG_OTHER_PATH + "InputSettingsProvider")]
    public class InputSettingsProvider : ScriptableObject
    {
        [SerializeField] private bool forceMobileInput;

        private IInputSettings m_InputSettings;

        public IInputSettings GetInputSettings()
        {
            if (m_InputSettings == null)
            {
                bool isMobile = IsMobile();

                m_InputSettings = new InputSettings(IsMobilePlatform(), isMobile);
            }

            return m_InputSettings;
        }

        public bool IsMobile() => IsMobilePlatform() || forceMobileInput;

        private bool IsMobilePlatform()
        {
            bool isMobilePlatform = false;

#if UNITY_ANDROID || UNITY_IOS

            // Application.isMobilePlatform on mobile device can be false at start?
            isMobilePlatform = true;
#endif
            return isMobilePlatform;
        }
    }
}