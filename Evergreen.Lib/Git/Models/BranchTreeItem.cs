using System.Collections.Generic;

using Avalonia.Media;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTreeItem
    {
        public string Label { get; init; }

        public string Name { get; init; }

        public string Parent { get; init; }

        public bool IsRemote { get; init; }

        public int Ahead { get; init; }

        public int Behind { get; init; }

        public bool IsHead { get; init; }

        public FontWeight FontWeight { get; init; }

        public IEnumerable<BranchTreeItem> Children { get; init; }
    }
}
