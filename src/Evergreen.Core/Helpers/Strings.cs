using System.Globalization;

namespace Evergreen.Core.Helpers
{
    public static class Strings
    {
        public static string ToTitleCase(this string input) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
    }
}
