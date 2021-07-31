using System.Collections.Generic;

using Evergreen.Lib.Helpers;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTree
    {
        public IEnumerable<BranchTreeItem> Local { get; init; }

        public IEnumerable<BranchTreeItem> Remote { get; init; }
    }
}
