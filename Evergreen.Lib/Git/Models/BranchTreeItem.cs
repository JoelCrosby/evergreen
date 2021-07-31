using System.Collections.Generic;

using Avalonia.Media;

namespace Evergreen.Lib.Git.Models
{
    public record BranchTreeItem
    {
        public string? Label { get; init; }

        public string? Name { get; init; }

        public string? Parent { get; init; }

        public bool IsRemote { get; init; }

        public int Ahead { get; init; }

        public int Behind { get; init; }

        public bool IsHead { get; init; }

        public FontWeight FontWeight { get; init; } = FontWeight.Regular;

        public IEnumerable<BranchTreeItem> Children { get; private set; }

        public BranchTreeItem(string label, FontWeight weight)
        {
            Label = label;
            Name = label.ToLowerInvariant();
            FontWeight = weight;
            Children = new List<BranchTreeItem>();
        }

        public BranchTreeItem() => Children = new List<BranchTreeItem>();

        public BranchTreeItem SetChildren(IEnumerable<BranchTreeItem> children)
        {
            Children = children;

            return this;
        }
    }
}
