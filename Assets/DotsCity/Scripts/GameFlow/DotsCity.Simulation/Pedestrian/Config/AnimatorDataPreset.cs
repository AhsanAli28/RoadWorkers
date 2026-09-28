using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianAnimatorDataPreset", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_FACTORY_PRESETS_PATH + "PedestrianAnimatorDataPreset")]
    public class AnimatorDataPreset : ScriptableObject
    {
        [Serializable]
        public class LegacyAnimationStateDictionary : AbstractSerializableDictionary<AnimationState, LegacyAnimationData> { }

        [Serializable]
        public class PureAnimationStateDictionary : AbstractSerializableDictionary<AnimationState, GPUAnimationData> { }

        [Serializable]
        public class MovementAnimationStateDictionary : AbstractSerializableDictionary<MovementState, AnimationState> { }

        [SerializeField]
        private LegacyAnimationStateDictionary legacyAnimationData = new LegacyAnimationStateDictionary();

        [SerializeField]
        private PureAnimationStateDictionary pureAnimationData = new PureAnimationStateDictionary();

        [SerializeField]
        private MovementAnimationStateDictionary movementAnimationBinding = new MovementAnimationStateDictionary();

        public LegacyAnimationStateDictionary LegacyAnimationData => legacyAnimationData;
        public PureAnimationStateDictionary PureAnimationData => pureAnimationData;
        public MovementAnimationStateDictionary MovementAnimationBinding => movementAnimationBinding;
    }
}
