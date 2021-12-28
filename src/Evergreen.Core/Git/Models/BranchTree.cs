using System.Collections.Generic;

namespace Evergreen.Core.Git.Models
{
    public record BranchTree(
        IEnumerable<BranchTreeItem> Local,
        IEnumerable<BranchTreeItem> Remote
    );
}
