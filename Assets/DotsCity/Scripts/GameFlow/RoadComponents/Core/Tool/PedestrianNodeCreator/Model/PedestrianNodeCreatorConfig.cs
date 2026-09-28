using UnityEngine;

namespace Spirit604.CityEditor.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianNodeCreatorConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_EDITOR_ROAD_PATH + "PedestrianNodeCreatorConfig")]
    public class PedestrianNodeCreatorConfig : ScriptableObject
    {
        [SerializeField] private Texture deleteButtonTexture;

        [Tooltip("If the connect or select button is pressed when the node is selected, the tool will automatically be selected")]
        [SerializeField] private bool autoSelectFromNode = true;

        public Texture DeleteButtonTexture => deleteButtonTexture;

        public bool AutoSelectFromNode => autoSelectFromNode;
    }
}