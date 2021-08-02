using System.Collections.Generic;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTree(
        IEnumerable<BranchTreeItem> Local,
        IEnumerable<BranchTreeItem> Remote
    );
}
