using System;
using System.Text;

namespace Spirit604.Extensions
{
    public static class StringExtension
    {
        public static string CamelToLabel(this string sourceCamelString)
        {
            StringBuilder output = new StringBuilder();

            sourceCamelString = sourceCamelString.Replace("_", "");

            foreach (char character in sourceCamelString)
            {
                if (char.IsUpper(character))
                {
                    output.Append(' ');
                }

                if (output.Length == 0)
                {
                    // The first letter must be always UpperCase
                    output.Append(Char.ToUpper(character));
                }
                else
                {
                    output.Append(character);
                }
            }

            return output.ToString().Trim();
        }
    }
}
