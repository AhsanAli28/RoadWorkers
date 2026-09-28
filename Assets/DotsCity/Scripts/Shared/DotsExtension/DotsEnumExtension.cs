using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class DotsEnumExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagUnsafe<TEnum>(TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {
            unsafe
            {
                switch (sizeof(TEnum))
                {
                    case 1:
                        return (*(byte*)(&lhs) & *(byte*)(&rhs)) > 0;
                    case 2:
                        return (*(ushort*)(&lhs) & *(ushort*)(&rhs)) > 0;
                    case 4:
                        return (*(uint*)(&lhs) & *(uint*)(&rhs)) > 0;
                    case 8:
                        return (*(ulong*)(&lhs) & *(ulong*)(&rhs)) > 0;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum AddFlag<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {
            unsafe
            {
                switch (sizeof(TEnum))
                {
                    case 1:
                        {
                            var r = *(byte*)(&lhs) | *(byte*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 2:
                        {
                            var r = *(ushort*)(&lhs) | *(ushort*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 4:
                        {
                            var r = *(uint*)(&lhs) | *(uint*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 8:
                        {
                            var r = *(ulong*)(&lhs) | *(ulong*)(&rhs);
                            return *(TEnum*)&r;
                        }
                }

                return lhs;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum RemoveFlag<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {
            unsafe
            {
                switch (sizeof(TEnum))
                {
                    case 1:
                        {
                            var r = *(byte*)(&lhs) & ~*(byte*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 2:
                        {
                            var r = *(ushort*)(&lhs) & ~*(ushort*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 4:
                        {
                            var r = *(uint*)(&lhs) & ~*(uint*)(&rhs);
                            return *(TEnum*)&r;
                        }
                    case 8:
                        {
                            var r = *(ulong*)(&lhs) & ~*(ulong*)(&rhs);
                            return *(TEnum*)&r;
                        }
                }

                return lhs;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFlag<TEnum>(ref this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {
            unsafe
            {
                fixed (TEnum* lhs1 = &lhs)
                {
                    switch (sizeof(TEnum))
                    {
                        case 1:
                            {
                                var r = *(byte*)(lhs1) | *(byte*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 2:
                            {
                                var r = *(ushort*)(lhs1) | *(ushort*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 4:
                            {
                                var r = *(uint*)(lhs1) | *(uint*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 8:
                            {
                                var r = *(ulong*)(lhs1) | *(ulong*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearFlag<TEnum>(this ref TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
        {
            unsafe
            {
                fixed (TEnum* lhs1 = &lhs)
                {
                    switch (sizeof(TEnum))
                    {
                        case 1:
                            {
                                var r = *(byte*)(lhs1) & ~*(byte*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 2:
                            {
                                var r = *(ushort*)(lhs1) & ~*(ushort*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 4:
                            {
                                var r = *(uint*)(lhs1) & ~*(uint*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                        case 8:
                            {
                                var r = *(ulong*)(lhs1) & ~*(ulong*)(&rhs);
                                *lhs1 = *(TEnum*)&r;
                                return;
                            }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] GetIndexLayers(uint mask)
        {
            List<int> ints = new List<int>(32);

            for (int i = 0; i < 32; i++)
            {
                var layerValue = 1 << i;

                if ((mask & layerValue) != 0)
                {
                    ints.Add(i);
                }

                if (layerValue > mask)
                {
                    break;
                }
            }

            return ints.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LayerToIndex(uint mask) => Mathf.RoundToInt(Mathf.Log(mask, 2));
    }
}