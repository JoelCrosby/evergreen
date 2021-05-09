using System.Collections.Generic;

using Evergreen.Lib.Helpers;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTree
    {
        public IEnumerable<TreeItem<BranchTreeItem>> Local { get; init; }

        public IEnumerable<TreeItem<BranchTreeItem>> Remote { get; init; }
    }
}
