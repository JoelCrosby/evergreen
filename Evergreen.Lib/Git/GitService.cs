using System.Collections.Generic;
using System.IO;
using System.Linq;

using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

using LibGit2Sharp;

namespace Evergreen.Lib.Git
{
    public class GitService
    {
        public readonly RepositorySession Repo;

        private readonly Repository repository;

        public GitService(RepositorySession repo)
        {
            Repo = repo;
            repository = new Repository(repo.Path);
        }

        public List<Commit> GetCommits() => repository.Commits.ToList();

        public List<Branch> GetBranches()
        {
            GetBranchTree();

            return repository.Branches.ToList();
        }

        public IEnumerable<TreeItem<BranchTreeItem>> GetBranchTree()
        {
            var branches = repository.Branches.ToList();
            var items = new List<BranchTreeItem>();

            foreach (var branch in branches)
            {
                var branchLevels = branch.CanonicalName.Split('/').Skip(1).ToList();

                for (var i = 0; i < branchLevels.Count; i++)
                {
                    var item = new BranchTreeItem
                    {
                        Label = branchLevels.ElementAtOrDefault(i),
                        Parent = branchLevels.ElementAtOrDefault(i - 1) ?? "Repository",
                    };

                    var exists = items.Any(x => x.Label == item.Label && x.Parent == item.Parent);

                    if (!exists)
                    {
                        items.Add(item);
                    }
                }
            }

            return items
                .GenerateTree(c => c.Label, c => c.Parent, "Repository");
        }

        public TreeChanges GetCommitFiles(string shortCommitHash)
        {
            var commit =  repository.Commits.FirstOrDefault(c => c.Sha.StartsWith(shortCommitHash));
            var prevCommit = commit.Parents.FirstOrDefault();

            if (prevCommit is null)
            {
                return repository.Diff.Compare<TreeChanges>(commit.Tree, commit.Tree);
            }

            return repository.Diff.Compare<TreeChanges>(prevCommit.Tree, commit.Tree);
        }
    }
}
