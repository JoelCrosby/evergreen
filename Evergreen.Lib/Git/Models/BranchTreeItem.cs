namespace Evergreen.Lib.Git.Models
{
    public class BranchTreeItem
    {
        public string Label { get; init; }

        public string Name { get; init; }

        public string Parent { get; init; }

        public bool IsRemote { get; init; }

        public int Ahead { get; init; }

        public int Behind { get; init; }
    }
}
