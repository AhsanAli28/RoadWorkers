using System.Runtime.CompilerServices;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public static class AnimUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StringToHash(string name) => !string.IsNullOrEmpty(name) ? Animator.StringToHash(name) : -1;
    }
}
