using System.Text;

namespace Kraig.Roslyn
{
    internal static class Utils
    {
        public static string ToPascalCase(this string value)
        {
            var result = new StringBuilder();
            for(var i = 0; i < value.Length; i++)
            {
                if (value[i] == '_')
                    result.Append(char.ToUpper(value[++i]));
                else result.Append(value[i]);
            }
            return result.ToString();
        }
    }
}
