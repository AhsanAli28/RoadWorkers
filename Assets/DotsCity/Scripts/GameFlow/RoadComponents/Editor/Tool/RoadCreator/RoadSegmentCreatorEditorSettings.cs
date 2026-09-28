#if UNITY_EDITOR
namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor
    {
        [System.Serializable]
        public struct RoadSegmentCreatorEditorSettings
        {
            public bool PrefabsFoldOut;
            public bool GeneralSettingsFoldOut;
            public bool CustomSettingsFoldOut;
            public bool PathSettingsFoldOut;
            public bool SnapNodeSettingsFoldOut;
            public bool SnapSurfaceSettingsFoldOut;
            public bool LightSettingsFoldOut;
            public bool LightAngleOffsetSettingsFoldOut;
            public bool SegmentHandlerSettingsFoldOut;
            public bool OtherSettingsFoldOut;
            public bool ParkingBuilderSettingsFoldOut;
            public bool TurnConnectionSettingsFoldOut;
            public bool UTurnConnectionSettingsFoldOut;

            public bool RoadSettingsSubFoldOut;
            public bool PathRoadSettingsSubFoldOut;
            public bool CustomSettingsSubFoldOut;
            public bool PedestrianNodeSubFoldOut;

            public static RoadSegmentCreatorEditorSettings GetDefault()
            {
                return new RoadSegmentCreatorEditorSettings()
                {
                    PrefabsFoldOut = false,
                    GeneralSettingsFoldOut = true,
                    CustomSettingsFoldOut = true,
                    PathSettingsFoldOut = true,
                    SnapNodeSettingsFoldOut = true,
                    SnapSurfaceSettingsFoldOut = true,
                    LightSettingsFoldOut = true,
                    LightAngleOffsetSettingsFoldOut = true,
                    SegmentHandlerSettingsFoldOut = true,
                    OtherSettingsFoldOut = true,
                    ParkingBuilderSettingsFoldOut = false,
                    TurnConnectionSettingsFoldOut = true,
                    UTurnConnectionSettingsFoldOut = true,
                    RoadSettingsSubFoldOut = true,
                    PathRoadSettingsSubFoldOut = true,
                    CustomSettingsSubFoldOut = true,
                    PedestrianNodeSubFoldOut = true,
                };
            }
        }
    }
}
#endif