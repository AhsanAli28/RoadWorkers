using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [Serializable]
    public class LegacyAnimationData
    {
        [Tooltip("State name of the animation in the animator")]
        public string StateName;

        [Tooltip("Number of the layer where the animation is stored in the animator")]
        public int StateLayer;

        [Tooltip("First parameter to start animation in the Animator")]
        public AnimParamType ParamType1;

        [Tooltip("Second parameter to start animation in the Animator [optional]")]
        public AnimParamType ParamType2;

        [Tooltip("First parameter to start animation in the Animator")]
        public string ParamName1;

        [Tooltip("Second parameter to start animation in the Animator [optional]")]
        public string ParamName2;

        public float Value1;
        public float Value2;

        [Tooltip("Parameter to exit current animation in the Animator [optional]")]
        public AnimParamType ExitParamType;

        [Tooltip("Parameter to exit current animation in the Animator [optional]")]
        public string ExitParamName;

        public float ExitValue;
    }
}