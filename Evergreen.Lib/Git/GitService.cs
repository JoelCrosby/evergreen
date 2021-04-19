using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
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
using System;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

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

        public string GetPath() => repository.Info.WorkingDirectory;

        public IEnumerable<Commit> GetCommits()
        {
            return repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = repository.Refs,
            });
        }

        public BranchTree GetBranchTree()
        {
            var branches = repository.Branches.ToList();

            static string getBranchLabel(string name, int ahead, int behind)
            {
                if (ahead == 0 && behind == 0)
                {
                    return name;
                }

                return (ahead, behind) switch
                {
                    (int, int) when ahead != 0 && behind == 0 => $"{name} ↑{ahead}",
                    (int, int) when ahead == 0 && behind != 0 => $"{name} ↓{behind}",
                    (int, int) when ahead != 0 && behind != 0 => $"{name} ↑{ahead} ↓{behind}",
                    _ => name,
                };
            }

            static IEnumerable<TreeItem<BranchTreeItem>> getBranchTree(List<Branch> branches, bool isLocal)
            {
                var items = new List<BranchTreeItem>();
                var root = isLocal ? "Branches" : "Remotes";

                foreach (var branch in branches.Where(b => b.IsRemote != isLocal))
                {
                    var branchLevels = branch.CanonicalName.Split('/').Skip(2).ToList();
                    var ahead = branch.TrackingDetails.AheadBy ?? 0;
                    var behind = branch.TrackingDetails.BehindBy ?? 0;

                    for (var i = 0; i < branchLevels.Count; i++)
                    {
                        var label = getBranchLabel(branchLevels.ElementAtOrDefault(i), ahead, behind);

                        var branchLevel = new BranchTreeItem
                        {
                            Name  = branch.CanonicalName,
                            Label = label,
                            Parent = branchLevels.ElementAtOrDefault(i - 1) ?? root,
                            Ahead = ahead,
                            Behind = behind,
                            IsRemote = branch.IsRemote,
                        };

                        var exists = items.Any(i =>
                            i.Label == branchLevel.Label
                            && i.Parent == branchLevel.Parent
                        );

                        if (exists)
                        {
                            continue;
                        }

                        items.Add(branchLevel);
                    }
                }

                return items.GenerateTree(c => c.Label, c => c.Parent, root);
            }

            var local = getBranchTree(branches, true);
            var remote = getBranchTree(branches, false);

            return new BranchTree
            {
                Local = local,
                Remote = remote,
            };
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

        public Task<Result<ExecResult>> FastForwad(string branch)
        {
            var repoBranch = repository
                .Branches
                .FirstOrDefault(b => !b.IsRemote && b.CanonicalName == branch);

            var remote = repoBranch.TrackedBranch.RemoteName;
            var shortBranchName = branch[(branch.LastIndexOf('/') + 1)..];

            return ExecAsync($"fetch {remote} {shortBranchName}:{shortBranchName}");
        }

        public Task<Result<ExecResult>> Fetch()
        {
            return ExecAsync("fetch --prune");
        }

        public Task<Result<ExecResult>> Pull()
        {
            return ExecAsync("pull");
        }

        public Task<Result<ExecResult>> Push()
        {
            return ExecAsync("push");
        }

        public Task<Result<ExecResult>> DeleteBranch(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var repoBranch = repository
                .Branches
                .FirstOrDefault(b => !b.IsRemote && b.CanonicalName == name);

            return ExecAsync($"branch -d {repoBranch.FriendlyName}");
        }

        public Result<Branch> CreateBranch(string name, bool checkout)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var newBranch = repository.CreateBranch(name);

            if (checkout)
            {
                Commands.Checkout(repository, newBranch.CanonicalName);
            }

            return Result<Branch>.Success(newBranch);
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

            var blob = (Blob)treeEntry.Target;

            using var contentStream = blob.GetContentStream();
            using var sr = new StreamReader(contentStream, Encoding.UTF8);

            return sr.ReadToEnd();
        }

        private async Task<Result<ExecResult>> ExecAsync(string args)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    Arguments = args,
                    FileName = "git",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    WorkingDirectory = repository.Info.WorkingDirectory,
                });

                await proc.WaitForExitAsync().ConfigureAwait(false);

                Debug.Assert(proc.ExitCode == 0);

                if (proc.ExitCode != 0)
                {
                    var stdOut = await proc.StandardOutput.ReadToEndAsync();

                    Debug.WriteLine(stdOut);

                    return Result<ExecResult>.Failed(stdOut);
                }

                return Result<ExecResult>.Success();
            }
            catch (Exception ex)
            {
                return Result<ExecResult>.Failed($"Git exec failed. {ex.Message}");
            }
        }
    }
}
