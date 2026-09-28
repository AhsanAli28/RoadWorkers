using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CreateAssetMenu(fileName = "RoadSegmentPlacerConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_EDITOR_ROAD_PATH + "RoadSegmentPlacerConfig")]
    public class RoadSegmentPlacerConfig : ScriptableObject
    {
        [SerializeField] private RoadSegmentCreator roadSegmentCreatorPrefab;
        [SerializeField] private Texture rotateButtonTextureLeft;
        [SerializeField] private Texture rotateButtonTextureRight;
        [SerializeField] private Texture deleteButtonTexture;

        public RoadSegmentCreator RoadSegmentCreatorPrefab { get => roadSegmentCreatorPrefab; }
        public Texture RotateButtonTextureLeft { get => rotateButtonTextureLeft; }
        public Texture RotateButtonTextureRight { get => rotateButtonTextureRight; }
        public Texture DeleteButtonTexture { get => deleteButtonTexture; }
    }
}