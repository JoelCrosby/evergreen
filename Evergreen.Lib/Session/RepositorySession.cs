using System.Collections.Generic;

namespace Evergreen.Lib.Session
{
    public class RepositorySession
    {
        public bool UseNativeTitleBar { get; set; }

        public List<string> Paths { get; init; } = new ();
    }
}
