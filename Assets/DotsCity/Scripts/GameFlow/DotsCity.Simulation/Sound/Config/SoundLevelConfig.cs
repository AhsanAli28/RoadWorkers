using Spirit604.Attributes;
using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [CreateAssetMenu(fileName = "SoundLevelConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_OTHER_PATH + "SoundLevelConfig")]
    public class SoundLevelConfig : ScriptableObject
    {
        [SerializeField] private bool hasSounds = true;

        [Tooltip("Custom audio listener will follow the player")]
        [ShowIf(nameof(hasSounds))]
        [SerializeField] private bool customAudioListener = true;

        [ShowIf(nameof(hasSounds))]
        [SerializeField] private bool crowdSound = true;

        [Tooltip("If this option is enabled, sound logic should be provided by the user")]
        [ShowIf(nameof(hasSounds))]
        [SerializeField] private bool forceCustomTrafficSound;

        [ShowIf(nameof(hasSounds))]
        [SerializeField] private bool randomHornsSound = true;

        public bool HasSounds => hasSounds;
        public bool CustomAudioListener => customAudioListener && hasSounds;
        public bool CrowdSound => crowdSound && hasSounds;
        public bool ForceCustomTrafficSound => forceCustomTrafficSound;
        public bool RandomHornsSound => randomHornsSound && hasSounds;
    }
}
