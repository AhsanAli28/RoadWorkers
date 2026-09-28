namespace Spirit604.AnimationBaker
{
    public static class Constans
    {
        public const string AssetRootPath = "Spirit604/Animation Baker/";

        public const int LODLevelCount = 3;

        // Shader textures
        public const string MainTexture = "_MainTex";
        public const string AnimationTexture = "_AnimTex";
        public const string NormalTexture = "_NormalTex";

        // Shader params
        public const string GlobalTime = "_GlobalTime";
        public const string PlaybackTime = "_PlaybackTime";
        public const string ClipLengthParam = "_ClipLength";
        public const string VertexCountParam = "_VertexCount";
        public const string FrameStepInvParam = "_FrameStepInv";
        public const string FrameCountParam = "_FrameCount2";
        public const string FrameOffsetParam = "_FrameOffset";
        public const string TargetPlaybackTime = "_TargetPlaybackTime";
        public const string TransitionTime = "_TransitionTime";
        public const string TargetFrameStepInvParam = "_TargetFrameStepInv";
        public const string TargetFrameCountParam = "_TargetFrameCount";
        public const string TargetFrameOffsetParam = "_TargetFrameOffset";
        public const string InterpolateParam = "_Interpolate";
        public const string ManualAnimation = "_ManualAnimation";
        public const string Transition = "_Transition";
    }
}
