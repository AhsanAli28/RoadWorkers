using System;
using System.Collections.Generic;
using System.Linq;

namespace Spirit604.Extensions
{
    public static class EnumExtension
    {
        public static IEnumerable<T> GetUniqueFlags<T>(this T flags) where T : Enum
        {
            foreach (Enum value in Enum.GetValues(flags.GetType()))
                if (flags.HasFlag(value))
                    yield return (T)value;
        }

        public static IEnumerable<T> GetAllFlags<T>() where T : Enum
        {
            CheckIsEnum<T>(true);
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static bool IsFlagSet<T>(this T value, T flag) where T : struct
        {
            CheckIsEnum<T>(true);
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
        {
            CheckIsEnum<T>(true);
            foreach (T flag in Enum.GetValues(typeof(T)).Cast<T>())
            {
                if (value.IsFlagSet(flag))
                    yield return flag;
            }
        }

        public static T SetFlags<T>(this T value, T flags, bool on) where T : struct
        {
            CheckIsEnum<T>(true);
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flags);
            if (on)
            {
                lValue |= lFlag;
            }
            else
            {
                lValue &= (~lFlag);
            }
            return (T)Enum.ToObject(typeof(T), lValue);
        }

        public static T SetFlags<T>(this T value, T flags) where T : struct
        {
            return value.SetFlags(flags, true);
        }

        public static T ClearFlags<T>(this T value, T flags) where T : struct
        {
            return value.SetFlags(flags, false);
        }

        private static void CheckIsEnum<T>(bool withFlags)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
            if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
        }
    }
}
