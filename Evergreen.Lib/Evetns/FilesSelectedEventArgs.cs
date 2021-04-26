using System;
using System.Collections.Generic;

using LibGit2Sharp;

namespace Evergreen.Lib.Events
{
    public class FilesSelectedEventArgs : EventArgs
    {
        public TreeChanges CommitChanges { get; set; }

        public List<string> Paths { get; set; }
    }
}


