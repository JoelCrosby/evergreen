namespace Evergreen.Lib.Git.Models
{
    public class BranchTreeItem
    {
        public string Label { get; set; }

        public string Name { get; set; }

        public string Parent { get; set; }

        public bool IsRemote { get; set; }
    }
}
