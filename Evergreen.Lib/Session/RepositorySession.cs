using System.Linq;

namespace Evergreen.Lib.Session
{
    public class RepositorySession
    {
        public string Path { get; init; }

        public string RepositoryFriendlyName => Path.Split('/').LastOrDefault();
    }
}
