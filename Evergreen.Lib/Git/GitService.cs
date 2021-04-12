using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

using LibGit2Sharp;

namespace Evergreen.Lib.Git
{
    public class GitService
    {
        public RepositorySession Session { get; }

        private readonly Repository repository;

        public GitService(RepositorySession repo)
        {
            Session = repo;
            repository = new Repository(repo.Path);
        }

        public string GetHeadCanonicalName() => repository.Head.CanonicalName;
        public string GetHeadFriendlyName() => repository.Head.FriendlyName;
        public IEnumerable<Commit> GetCommits() => repository.Commits;

        public IEnumerable<TreeItem<BranchTreeItem>> GetBranchTree()
        {
            var branches = repository.Branches.ToList();
            var items = new List<BranchTreeItem>();

            foreach (var branch in branches)
            {
                var branchLevels = branch.CanonicalName.Split('/').Skip(1).ToList();

                for (var i = 0; i < branchLevels.Count; i++)
                {
                    var branchLevel = new BranchTreeItem
                    {
                        Name  = branch.FriendlyName,
                        Label = branchLevels.ElementAtOrDefault(i),
                        Parent = branchLevels.ElementAtOrDefault(i - 1) ?? "Repository",
                        IsRemote = branch.IsRemote,
                    };

                    var exists = items.Any(item =>
                        item.Label == branchLevel.Label
                        && item.Parent == branchLevel.Parent
                        && item.IsRemote == branchLevel.IsRemote
                    );

                    if (!exists)
                    {
                        items.Add(branchLevel);
                    }
                }
            }

            return items
                .GenerateTree(c => c.Label, c => c.Parent, "Repository");
        }

        public TreeChanges GetCommitFiles(string commitId)
        {
        var commit = repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();

            if (prevCommit is null)
            {
                return repository.Diff.Compare<TreeChanges>(commit.Tree, commit.Tree);
            }

            return repository.Diff.Compare<TreeChanges>(prevCommit.Tree, commit.Tree);
        }

        public Patch GetCommitPatch(string commitId)
        {
            var commit = repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();

            if (prevCommit is null)
            {
                return repository.Diff.Compare<Patch>(commit.Tree, commit.Tree);
            }

            return repository.Diff.Compare<Patch>(prevCommit.Tree, commit.Tree);
        }

        public DiffPaneModel GetCommitDiff(string commitId, string path)
        {
            var commit = repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();
            var content = GetFileContent(path, commit.Sha);
            var diffBuilder = new InlineDiffBuilder(new Differ());

            if (prevCommit is null)
            {
                return diffBuilder.BuildDiffModel(content, content);
            }

            var prevContent = GetFileContent(path, prevCommit.Sha);

            return diffBuilder.BuildDiffModel(prevContent ?? string.Empty, content ?? string.Empty);
        }

        public void Checkout(string branch)
        {
            Commands.Checkout(repository, branch);
        }

        public string GetCommitAuthor(string commitId)
        {
            var commit = repository.Lookup<Commit>(commitId);
            return $"{commit.Author.Name} {commit.Author.Email}";
        }

        public string GetFileContent(string path, string commitId)
        {
            var commit = repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var treeEntry = commit[path];

            if (treeEntry is null)
            {
                return null;
            }

            Debug.Assert(treeEntry.TargetType == TreeEntryTargetType.Blob);
            var blob = (Blob)treeEntry.Target;

            using var contentStream = blob.GetContentStream();
            using var sr = new StreamReader(contentStream, Encoding.UTF8);

            return sr.ReadToEnd();
        }
    }
}
