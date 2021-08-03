using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Media;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

using Evergreen.Core.Git.Models;
using Evergreen.Core.Helpers;
using Evergreen.Core.Models;
using Evergreen.Core.Models.Common;

using LibGit2Sharp;

using GitCommands = LibGit2Sharp.Commands;

namespace Evergreen.Core.Git
{
    public class GitService : IDisposable
    {
        private readonly Repository _repository;

        public GitService(string path)
        {
            _repository = new Repository(path);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

        public string GetHeadCanonicalName()
        {
            return _repository.Head.CanonicalName;
        }

        public string GetHeadFriendlyName()
        {
            return _repository.Head.FriendlyName;
        }

        public string GetPath()
        {
            return _repository.Info.WorkingDirectory;
        }

        public string GetFriendlyPath()
        {
            return _repository.Info.WorkingDirectory.SubHomePath();
        }

        public static bool IsRepository(string path)
        {
            return Repository.IsValid(path);
        }

        public string GetRepositoryFriendlyName()
        {
            var dirInfo = new DirectoryInfo(_repository.Info.WorkingDirectory);
            return dirInfo.Name.ToTitleCase();
        }

        public Signature GetSignature()
        {
            var config = _repository.Config;

            var name = config.FirstOrDefault(v => v.Key == "user.name")?.Value;
            var email = config.FirstOrDefault(v => v.Key == "user.email")?.Value;

            if (name is null || email is null)
            {
                throw new GitException("user.name or user.email is not configured.");
            }

            return new Signature(name, email, DateTimeOffset.UtcNow);
        }

        public IEnumerable<Commit> GetCommits()
        {
            return _repository.Commits.QueryBy(
                new CommitFilter {IncludeReachableFrom = _repository.Refs,}
            );
        }

        public Commit? GetHeadCommit()
        {
            return _repository.Head.Commits.FirstOrDefault();
        }

        public IEnumerable<(string Sha, string FriendlyName)> GetBranchHeadCommits()
        {
            return _repository.Branches.Select(b => (b.Commits.First().Sha, b.FriendlyName));
        }

        public BranchTree GetBranchTree()
        {
            var branchList = _repository.Branches.ToList();

            static string GetBranchLabel(string name, int ahead, int behind)
            {
                var label = name[(name.LastIndexOf('/') + 1)..];

                if (ahead == 0 && behind == 0)
                {
                    return label;
                }

                return (ahead, behind) switch
                {
                    (_, _) when ahead != 0 && behind == 0 => $"{label} ↑{ahead}",
                    (_, _) when ahead == 0 && behind != 0 => $"{label} ↓{behind}",
                    (_, _) when ahead != 0 && behind != 0 => $"{label} ↑{ahead} ↓{behind}",
                    _ => label,
                };
            }


            IEnumerable<BranchTreeItem> Tree(IEnumerable<Branch> branches, bool isLocal)
            {
                const string rootLocal = "Branches";
                const string rootRemote = "Remotes";

                var items = new List<BranchTreeItem>();
                var root = isLocal ? rootLocal : rootRemote;

                var parents = new HashSet<string>();
                var activeBranch = GetHeadFriendlyName();

                foreach (var branch in branches.Where(b => b.IsRemote != isLocal))
                {
                    var cName = branch.CanonicalName;
                    var branchLevels = branch.CanonicalName.Split('/');
                    var ahead = branch.TrackingDetails.AheadBy ?? 0;
                    var behind = branch.TrackingDetails.BehindBy ?? 0;
                    var label = GetBranchLabel(branch.CanonicalName, ahead, behind);

                    var name = string.Join('/', branchLevels.Skip(2));
                    var hasChild = name.Contains('/');
                    var parent = hasChild ? cName[..cName.LastIndexOf('/')] : root;
                    var isHead = activeBranch == name;

                    var branchLevel = new BranchTreeItem
                    {
                        Name = name,
                        Label = label,
                        Parent = parent,
                        Ahead = ahead,
                        Behind = behind,
                        IsRemote = !isLocal,
                        FontWeight = isHead ? FontWeight.ExtraBold : FontWeight.Regular,
                        IsHead = isHead,
                    };

                    parents.Add(parent);
                    items.Add(branchLevel);
                }

                var parentBranches = new HashSet<string>();

                foreach (var parent in parents)
                {
                    foreach (var _ in parent.Split('/').Skip(2))
                    {
                        parentBranches.Add(parent);
                    }
                }

                foreach (var parentBranch in parentBranches)
                {
                    var name = parentBranch;
                    var hasChild = name.Contains('/');

                    var label = hasChild ? name[(name.LastIndexOf('/') + 1)..] : name;
                    var parent = hasChild ? name[..name.LastIndexOf('/')] : root;

                    const string refsHeads = "refs/heads";
                    const string refsRemotes = "refs/remotes";

                    static string ParentPath(string path)
                    {
                        return path switch
                        {
                            refsHeads => rootLocal,
                            refsRemotes => rootRemote,
                            _ => path,
                        };
                    }

                    items.Add(new BranchTreeItem
                    {
                        Name = name,
                        Label = label,
                        Parent = ParentPath(parent),
                        IsRemote = !isLocal,
                        FontWeight = FontWeight.Regular,
                    });
                }

                return GenerateTree(items, c => c.Name, c => c.Parent, root);
            }

            var local = Tree(branchList, true);
            var remote = Tree(branchList, false);

            return new BranchTree(local, remote);
        }

        private static IEnumerable<BranchTreeItem> GenerateTree(
            IEnumerable<BranchTreeItem> collection,
            Func<BranchTreeItem, string?> idSelector,
            Func<BranchTreeItem, string?> parentIdSelector,
            string? rootId = null)
        {

            var list = collection.ToList();
            var tree = list
                .Where(c => parentIdSelector(c)?.Equals(rootId, StringComparison.Ordinal) ?? false)
                .Select(c => c.SetChildren(GenerateTree(list, idSelector, parentIdSelector, idSelector(c))));

            return tree.OrderBy(l => l.Children.Any());
        }

        public TreeChanges? GetCommitFiles(string commitId)
        {
            var commit = _repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();

            if (prevCommit is null)
            {
                return _repository.Diff.Compare<TreeChanges>(commit.Tree, commit.Tree);
            }

            return _repository.Diff.Compare<TreeChanges>(prevCommit.Tree, commit.Tree);
        }

        public IEnumerable<StatusEntry> GetStagedFiles()
        {
            var status = _repository.RetrieveStatus();

            return status.Staged
                .Concat(status.Removed);
        }

        public TreeChanges GetChangedFiles()
        {
            var paths = new[]
            {
                _repository.Info.WorkingDirectory,
            };

            return _repository.Diff.Compare<TreeChanges>(paths);
        }

        public Patch? GetCommitPatch(string commitId)
        {
            var commit = _repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();

            if (prevCommit is null)
            {
                return _repository.Diff.Compare<Patch>(commit.Tree, commit.Tree);
            }

            return _repository.Diff.Compare<Patch>(prevCommit.Tree, commit.Tree);
        }

        public DiffPaneModel? GetCommitDiff(string commitId, string path)
        {
            var commit = _repository.Lookup<Commit>(commitId);

            if (commit is null)
            {
                return null;
            }

            var prevCommit = commit.Parents.FirstOrDefault();
            var content = GetCommitContent(path, commit.Sha);
            var diffBuilder = new InlineDiffBuilder(new Differ());

            if (prevCommit is null)
            {
                return diffBuilder.BuildDiffModel(content, content);
            }

            var prevContent = GetCommitContent(path, prevCommit.Sha);

            return diffBuilder.BuildDiffModel(prevContent ?? string.Empty, content ?? string.Empty);
        }

        public async Task<DiffPaneModel?> GetChangesDiff(string? path)
        {
            var commit = GetHeadCommit();

            if (commit is null || path is null)
            {
                return null;
            }

            var absPath = Path.Join(_repository.Info.WorkingDirectory, path);
            var prevCommit = GetHeadCommit();
            var content = await FileUtils.ReadToString(absPath);
            var diffBuilder = new InlineDiffBuilder(new Differ());

            if (prevCommit is null)
            {
                return diffBuilder.BuildDiffModel(content, content);
            }

            var prevContent = GetCommitContent(path, prevCommit.Sha);

            return diffBuilder.BuildDiffModel(prevContent ?? string.Empty, content ?? string.Empty);
        }

        public void Stage(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                GitCommands.Stage(_repository, path);
            }
        }

        public void UnStage(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                GitCommands.Unstage(_repository, path);
            }
        }

        public void Checkout(string branch)
        {
            GitCommands.Checkout(_repository, branch);
        }

        public void Commit(string message)
        {
            var sig = GetSignature();

            _repository.Commit(
                message, sig, sig, new CommitOptions
                {
                    PrettifyMessage = true,
                }
            );
        }

        public Task<Result<ExecResult>> FastForward(string branch)
        {
            var repoBranch = _repository.Branches[branch];

            var remote = repoBranch.TrackedBranch.RemoteName;
            var shortBranchName = branch[(branch.LastIndexOf('/') + 1)..];

            return ExecAsync($"fetch {remote} {shortBranchName}:{shortBranchName}");
        }

        public Result MergeBranch(string branch)
        {
            var repoBranch = _repository
                .Branches
                .FirstOrDefault(b => !b.IsRemote && b.CanonicalName == branch);

            var sig = GetSignature();

            var options = new MergeOptions
            {
                FileConflictStrategy = CheckoutFileConflictStrategy.Merge,
            };

            _repository.Merge(repoBranch, sig, options);

            if (_repository.Index.Conflicts.Any())
            {
                return Result.Failed("Merge resulted in conflicts.");
            }

            return Result.Success();
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

            var repoBranch = _repository
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

            var newBranch = _repository.CreateBranch(name);

            if (checkout)
            {
                GitCommands.Checkout(_repository, newBranch.CanonicalName);
            }

            return Result<Branch>.Success(newBranch);
        }

        public string GetCommitAuthor(string commitId)
        {
            var commit = _repository.Lookup<Commit>(commitId);
            return $"{commit.Author.Name} {commit.Author.Email}";
        }

        private string? GetCommitContent(string relPath, string commitId)
        {
            var commit = _repository.Lookup<Commit>(commitId);

            var treeEntry = commit?[relPath];

            if (treeEntry is null)
            {
                return null;
            }

            var blob = (Blob)treeEntry.Target;
            using var contentStream = blob.GetContentStream();

            return FileUtils
                .GetFileContent(contentStream)
                .GetAwaiter()
                .GetResult();
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
                        WorkingDirectory = _repository.Info.WorkingDirectory,
                    }
                );

                if (proc is null)
                {
                    throw new InvalidOperationException($"unable to start sub process 'git {args}'");
                }

                await proc.WaitForExitAsync().ConfigureAwait(false);

                Debug.Assert(proc.ExitCode == 0);

                if (proc.ExitCode == 0)
                {
                    return Result<ExecResult>.Success();
                }

                var stdOut = await proc.StandardOutput
                    .ReadToEndAsync()
                    .ConfigureAwait(false);

                Debug.WriteLine(stdOut);

                return Result<ExecResult>.Failed(stdOut);

            }
            catch (Exception ex)
            {
                return Result<ExecResult>.Failed($"exec 'git {args}' failed. {ex.Message}");
            }
        }

        public IEnumerable<CommitListItem> GetCommitListItems()
        {
            var result = new List<CommitListItem>();

            var commits = GetCommits();
            var heads = GetBranchHeadCommits();

            var headDict = heads.Aggregate(
                new Dictionary<string, List<string>>(), (a, c) =>
                {
                    if (a.TryGetValue(c.Sha, out var item))
                    {
                        item.Add(c.FriendlyName);
                    }
                    else
                    {
                        a.Add(c.Sha, new List<string>
                        {
                            c.FriendlyName,
                        });
                    }

                    return a;
                }
            );

            static string CommitMessageShort(string? s, Commit commit)
            {
                return s is { } ? $"{commit.MessageShort} {s}" : commit.MessageShort;
            }

            foreach (var commit in commits)
            {
                var hasValue = headDict.TryGetValue(commit.Sha, out var branches);
                var branchLabel = hasValue ? string.Join(' ', branches!.Select(b => $"({b})")) : null;

                var commitDate = commit.Author.When.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
                var author = commit.Author.Name;
                var message = CommitMessageShort(branchLabel, commit);
                var sha = commit.Sha[..7];
                var id = commit.Id.Sha;

                result.Add(new CommitListItem
                {
                    Message = message,
                    Author = author,
                    Sha = sha,
                    CommitDate = commitDate,
                    Id = id,
                });
            }

            return result;
        }
    }
}
