using System.Globalization;

namespace Evergreen.Lib.Helpers
{
    public static class Strings
    {
        public static string ToTitleCase(this string input)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        }
    }
}
