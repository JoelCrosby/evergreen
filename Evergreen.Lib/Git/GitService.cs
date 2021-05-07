using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

using LibGit2Sharp;

namespace Evergreen.Lib.Git
{
    public class GitService : IDisposable
    {
        private readonly Repository repository;

        public GitService(string path) => repository = new Repository(path);

        public void Dispose() => repository.Dispose();

        public string GetHeadCanonicalName() => repository.Head.CanonicalName;

        public string GetHeadFriendlyName() => repository.Head.FriendlyName;

        public string GetPath() => repository.Info.WorkingDirectory;

        public string GetFriendlyPath() => repository.Info.WorkingDirectory.SubHomePath();

        public static bool IsRepository(string path) => Repository.IsValid(path);

        public string GetRepositoryFriendlyName()
        {
            var dirInfo = new DirectoryInfo(repository.Info.WorkingDirectory);
            return dirInfo.Name.ToTitleCase();
        }

        public Signature GetSignature()
        {
            var config = repository.Config;

            var name = config.FirstOrDefault(v => v.Key == "user.name")?.Value;
            var email = config.FirstOrDefault(v => v.Key == "user.email")?.Value;

            if (name is null || email is null)
            {
                throw new Exception("user.name or user.email is not configured.");
            }

            return new Signature(name, email, DateTimeOffset.UtcNow);
        }

        public IEnumerable<Commit> GetCommits() => repository.Commits.QueryBy(
            new CommitFilter
            {
                IncludeReachableFrom = repository.Refs,
            }
        );

        public Commit GetHeadCommit() => repository.Head.Commits.FirstOrDefault();

        public IEnumerable<(string sha, string label)> GetBranchHeadCommits() =>
            repository.Branches.Select(b => (b.Commits.FirstOrDefault()?.Sha, b.FriendlyName));

        public BranchTree GetBranchTree()
        {
            var branches = repository.Branches.ToList();

            static string GetBranchLabel(string name, int ahead, int behind)
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

            static IEnumerable<TreeItem<BranchTreeItem>> Tree(IEnumerable<Branch> branches, bool isLocal)
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
                        var label = GetBranchLabel(branchLevels.ElementAtOrDefault(i), ahead, behind);

                        var branchLevel = new BranchTreeItem
                        {
                            Name = branch.CanonicalName,
                            Label = label,
                            Parent = branchLevels.ElementAtOrDefault(i - 1) ?? root,
                            Ahead = ahead,
                            Behind = behind,
                            IsRemote = branch.IsRemote,
                        };

                        var exists = items.Any(
                            i =>
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

            var local = Tree(branches, true);
            var remote = Tree(branches, false);

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

        public IEnumerable<StatusEntry> GetStagedFiles()
        {
            var status = repository.RetrieveStatus();

            return status.Staged
                .Concat(status.Removed);
        }

        public TreeChanges GetChangedFiles()
        {
            var paths = new[]
            {
                repository.Info.WorkingDirectory,
            };

            return repository.Diff.Compare<TreeChanges>(paths);
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

        public async Task<DiffPaneModel> GetChangesDiff(string path)
        {
            var commit = GetHeadCommit();

            if (commit is null)
            {
                return null;
            }

            var absPath = Path.Join(repository.Info.WorkingDirectory, path);
            var prevCommit = GetHeadCommit();
            var content = await FileUtils.ReadToString(absPath);
            var diffBuilder = new InlineDiffBuilder(new Differ());

            if (prevCommit is null)
            {
                return diffBuilder.BuildDiffModel(content, content);
            }

            var prevContent = GetFileContent(path, prevCommit.Sha);

            return diffBuilder.BuildDiffModel(prevContent ?? string.Empty, content ?? string.Empty);
        }

        public void Stage(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                Commands.Stage(repository, path);
            }
        }

        public void UnStage(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                Commands.Unstage(repository, path);
            }
        }

        public void Checkout(string branch) => Commands.Checkout(repository, branch);

        public void Commit(string message)
        {
            var sig = GetSignature();

            repository.Commit(
                message, sig, sig, new CommitOptions
                {
                    PrettifyMessage = true,
                }
            );
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

        public Result MergeBranch(string branch)
        {
            var repoBranch = repository
                .Branches
                .FirstOrDefault(b => !b.IsRemote && b.CanonicalName == branch);

            var sig = GetSignature();

            var options = new MergeOptions
            {
                FileConflictStrategy = CheckoutFileConflictStrategy.Merge,
            };

            repository.Merge(repoBranch, sig, options);

            if (repository.Index.Conflicts.Any())
            {
                return Result.Failed("Merge resulted in conflicts.");
            }

            return Result.Success();
        }

        public Task<Result<ExecResult>> Fetch() => ExecAsync("fetch --prune");

        public Task<Result<ExecResult>> Pull() => ExecAsync("pull");

        public Task<Result<ExecResult>> Push() => ExecAsync("push");

        public Task<Result<ExecResult>> DeleteBranch(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var repoBranch = repository
                .Branches
                .FirstOrDefault(b => !b.IsRemote && b.CanonicalName == name);

            if (repoBranch is null)
            {
                return Task.FromResult(Result<ExecResult>.Failed("Branch not found."));
            }

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

        public string GetFileContent(string relPath, string commitId)
        {
            var commit = repository.Lookup<Commit>(commitId);

            var treeEntry = commit?[relPath];

            if (treeEntry is null)
            {
                return null;
            }

            var blob = (Blob)treeEntry.Target;

            using var contentStream = blob.GetContentStream();
            using var sr = new StreamReader(contentStream, Encoding.UTF8);

            return sr.ReadToEnd();
        }

        public int GetHeadDiffCount()
        {
            var changes = GetChangedFiles();
            var staged = GetStagedFiles();

            return changes.Count + staged.Count();
        }

        private async Task<Result<ExecResult>> ExecAsync(string args)
        {
            try
            {
                var proc = Process.Start(
                    new ProcessStartInfo
                    {
                        Arguments = args,
                        FileName = "git",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        WorkingDirectory = repository.Info.WorkingDirectory,
                    }
                );

                await proc.WaitForExitAsync().ConfigureAwait(false);

                Debug.Assert(proc.ExitCode == 0);

                if (proc.ExitCode != 0)
                {
                    var stdOut = await proc.StandardOutput
                        .ReadToEndAsync()
                        .ConfigureAwait(false);

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
