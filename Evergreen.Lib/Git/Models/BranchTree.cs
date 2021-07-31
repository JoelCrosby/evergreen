using System.Collections.Generic;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTree
    {
        public IEnumerable<BranchTreeItem> Local { get; init; }

        public IEnumerable<BranchTreeItem> Remote { get; init; }
    }
}
