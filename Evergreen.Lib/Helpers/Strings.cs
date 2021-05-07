using System.Globalization;

namespace Evergreen.Lib.Helpers
{
    public static class Strings
    {
        public static string ToTitleCase(this string input) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
    }
}
