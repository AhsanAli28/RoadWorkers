#if UNITY_EDITOR
namespace Spirit604.CityEditor.Road
{
    [System.Serializable]
    public struct PathSharedEditorSettings
    {
        // Path tabs
        public bool ShowCached;
        public bool ShowSettings;
        public bool ShowVisual;

        // Path settings
        public bool ShowWaypoints;
        public bool ShowAdditionalInfo;
        public bool ShowHandles;
        public bool ShowEditButtons;

        public static PathSharedEditorSettings GetDefault()
        {
            return new PathSharedEditorSettings()
            {
                ShowCached = false,
                ShowSettings = true,
                ShowVisual = true,
                ShowWaypoints = true,
                ShowAdditionalInfo = false,
                ShowHandles = true,
                ShowEditButtons = true,
            };
        }
    }
}
#endif