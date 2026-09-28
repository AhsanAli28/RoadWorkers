using Spirit604.AnimationBaker;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianGPUAnimationsConstans
    {
        public const string Idle_Anim_Name = "Idle";
        public const string Walking_Anim_Name = "Walking";
        public const string Running_Anim_Name = "Running";
        public const string Talking_Anim1_Name = "Talking1";
        public const string Talking_Anim2_Name = "Talking2";
        public const string Talking_Anim3_Name = "Talking3";
        public const string SittingIn_Anim_Name = "StandToSit";
        public const string SittingIdle_Anim_Name = "SittingIdle";
        public const string SittingOut_Anim_Name = "SitToStand";

        public static int Idle_Anim_Hash = AnimUtils.StringToHash(Idle_Anim_Name);
        public static int Walking_Anim_Hash = AnimUtils.StringToHash(Walking_Anim_Name);
        public static int Running_Anim_Hash = AnimUtils.StringToHash(Running_Anim_Name);
        public static int Talking_Anim1_Hash = AnimUtils.StringToHash(Talking_Anim1_Name);
        public static int Talking_Anim2_Hash = AnimUtils.StringToHash(Talking_Anim2_Name);
        public static int Talking_Anim3_Hash = AnimUtils.StringToHash(Talking_Anim3_Name);
        public static int SittingIn_Anim_Hash = AnimUtils.StringToHash(SittingIn_Anim_Name);
        public static int SittingIdle_Anim_Hash = AnimUtils.StringToHash(SittingIdle_Anim_Name);
        public static int SittingOut_Anim_Hash = AnimUtils.StringToHash(SittingOut_Anim_Name);

        public static int GetAnimationHash(MovementState movementState)
        {
            switch (movementState)
            {
                case MovementState.Idle:
                    return PedestrianGPUAnimationsConstans.Idle_Anim_Hash;
                case MovementState.Walking:
                    return PedestrianGPUAnimationsConstans.Walking_Anim_Hash;
                case MovementState.Running:
                    return PedestrianGPUAnimationsConstans.Running_Anim_Hash;
            }

            return 0;
        }

        public static NativeArray<int> GetMovementHashes(Allocator allocator = Allocator.Persistent)
        {
            NativeArray<int> hashes = new NativeArray<int>(3, allocator);

            hashes[0] = Idle_Anim_Hash;
            hashes[1] = Walking_Anim_Hash;
            hashes[2] = Running_Anim_Hash;

            return hashes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMovementAnimHash(in NativeArray<int> hashes, MovementState movementState)
        {
            var index = (int)movementState - 1;

            if (index >= 0)
            {
                return hashes[index];
            }

            return -1;
        }
    }
}
