using System;
using System.Collections.Generic;

namespace Evergreen.Core.Events
{
    public class FilesSelectedEventArgs : EventArgs
    {
        public IEnumerable<string> Paths { get; init; } = Array.Empty<string>();
    }
}
