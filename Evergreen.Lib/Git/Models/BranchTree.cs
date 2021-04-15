using System.Collections.Generic;

using Evergreen.Lib.Helpers;

namespace Evergreen.Lib.Git.Models
{
    public class BranchTree
    {
        public IEnumerable<TreeItem<BranchTreeItem>> Local { get; set; }

        public IEnumerable<TreeItem<BranchTreeItem>> Remote { get; set; }
    }
}
