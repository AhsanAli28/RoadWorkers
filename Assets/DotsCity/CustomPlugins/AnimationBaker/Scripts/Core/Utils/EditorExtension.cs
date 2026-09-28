#if UNITY_EDITOR
namespace Spirit604.AnimationBaker.EditorInternal
{
    internal static class EditorExtension
    {
        public static string ClearPathSlashes(this string sourceCamelString, bool autoTrim = true)
        {
            if (autoTrim)
            {
                sourceCamelString = sourceCamelString.Trim();
            }

            if (sourceCamelString[0] == '/')
            {
                sourceCamelString = sourceCamelString.Substring(1, sourceCamelString.Length - 1);
            }

            if (sourceCamelString[sourceCamelString.Length - 1] == '/')
            {
                sourceCamelString = sourceCamelString.Substring(0, sourceCamelString.Length - 1);
            }

            return sourceCamelString;
        }
    }
}
#endif