namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct LegacyAnimationDataComponent
    {
        public int StateLayer;
        public int StateNameHash;
        public AnimParamType ParamType1;
        public AnimParamType ParamType2;
        public int ParamNameHash1;
        public int ParamNameHash2;
        public float Value1;
        public float Value2;

        public AnimParamType ExitParamType;
        public int ExitParamNameHash;
        public float ExitParamValue;

        public LegacyAnimationDataComponent(LegacyAnimationData legacyAnimationData, IAnimationHashProvider hashProvider)
        {
            StateLayer = legacyAnimationData.StateLayer;
            StateNameHash = hashProvider.GetHash(legacyAnimationData.StateName);
            ParamType1 = legacyAnimationData.ParamType1;
            ParamType2 = legacyAnimationData.ParamType2;
            ParamNameHash1 = hashProvider.GetHash(legacyAnimationData.ParamName1);
            ParamNameHash2 = hashProvider.GetHash(legacyAnimationData.ParamName2);
            Value1 = legacyAnimationData.Value1;
            Value2 = legacyAnimationData.Value2;

            ExitParamType = legacyAnimationData.ExitParamType;
            ExitParamNameHash = hashProvider.GetHash(legacyAnimationData.ExitParamName);
            ExitParamValue = legacyAnimationData.ExitValue;
        }
    }
}