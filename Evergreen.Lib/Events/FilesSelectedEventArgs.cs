using System;
using System.Collections.Generic;

namespace Evergreen.Lib.Events
{
    public class FilesSelectedEventArgs : EventArgs
    {
        public IEnumerable<string> Paths { get; init; } = Array.Empty<string>();
    }
}
