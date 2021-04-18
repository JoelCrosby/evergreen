using System.Linq;

using Evergreen.Lib.Helpers;

namespace Evergreen.Lib.Session
{
    public class RepositorySession
    {
        public bool UseNativeTitleBar { get; set; }

        public string Path { get; init; }

        public string RepositoryFriendlyName => Path.Split('/').LastOrDefault()?.ToTitleCase();
    }
}
