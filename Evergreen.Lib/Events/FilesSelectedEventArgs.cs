using System;
using System.Collections.Generic;

namespace Evergreen.Lib.Events
{
    public class FilesSelectedEventArgs : EventArgs
    {
        public List<string> Paths { get; set; }
    }
}
