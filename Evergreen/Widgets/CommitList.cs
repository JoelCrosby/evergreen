using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Evergreen.Core.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class CommitList : TreeWidget, IDisposable
    {
        private TreeStore _store;

        private enum Column
        {
            Message,
            Author,
            Sha,
            Date,
            Id,
        }

        public CommitList(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += CommitListCursorChanged;

            var messageColumn = Columns.Create("Message", (int)Column.Message, 800, isFixed: true);
            var authorColumn = Columns.Create("Author", (int)Column.Author, isFixed: true);
            var shaColumn = Columns.Create("Sha", (int)Column.Sha, isFixed: true);
            var dateColumn = Columns.Create("Date", (int)Column.Date, 20, isFixed: true);
            var idColumn = Columns.Create("ID", (int)Column.Id, 20, isFixed: true, isHidden: true);

            foreach (var column in View.Columns)
            {
                View.RemoveColumn(column);
            }

            View.AppendColumn(messageColumn);
            View.AppendColumn(authorColumn);
            View.AppendColumn(shaColumn);
            View.AppendColumn(dateColumn);
            View.AppendColumn(idColumn);
        }

        public void Dispose() => View.CursorChanged -= CommitListCursorChanged;

        public event EventHandler<CommitSelectedEventArgs> CommitSelected;

        public void Refresh()
        {
            var sw = Stopwatch.StartNew();

            var commits = Git.GetCommits();

            Debug.WriteLine("GetCommits {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();

            var heads = Git.GetBranchHeadCommits();

            Console.WriteLine("GetBranchHeadCommits {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();

            var headDict = heads.Aggregate(
                new Dictionary<string, List<string>>(), (a, c) =>
                {
                    if (a.TryGetValue(c.Sha, out var item))
                    {
                        item.Add(c.FriendlyName);
                    }
                    else
                    {
                        a.Add(
                            c.Sha, new List<string>
                            {
                                c.FriendlyName,
                            }
                        );
                    }

                    return a;
                }
            );

            Debug.WriteLine("build headDict {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();

            _store = new TreeStore(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            sw.Start();

            static string CommitMessageShort(string s, Commit commit)
            {
                return s is { } ? $"{commit.MessageShort} {s}" : commit.MessageShort;
            }

            static string BranchLabel(bool hasValue, IEnumerable<string> value)
            {
                return hasValue ? string.Join(' ', value.Select(b => $"({b})")) : null;
            }

            foreach (var commit in commits)
            {
                var hasValue = headDict.TryGetValue(commit.Sha, out var branches);
                var branchLabel = BranchLabel(hasValue, branches);

                var commitDate = commit.Author.When.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
                var author = commit.Author.Name;
                var message = CommitMessageShort(branchLabel, commit);
                var sha = commit.Sha[..7];
                var id = commit.Id.Sha;

                _store.AppendValues(
                    message,
                    author,
                    sha,
                    commitDate,
                    id
                );
            }

            Debug.WriteLine("store AppendValues {0}ms", sw.ElapsedMilliseconds);

            View.Model = _store;
        }

        private void CommitListCursorChanged(object sender, EventArgs args)
        {
            var selectedId = View.GetSelected<string>(4);

            if (string.IsNullOrEmpty(selectedId))
            {
                return;
            }

            OnCommitSelected(
                new CommitSelectedEventArgs
                {
                    CommitId = selectedId,
                }
            );
        }

        private void OnCommitSelected(CommitSelectedEventArgs e) => CommitSelected?.Invoke(this, e);
    }

    public class CommitSelectedEventArgs : EventArgs
    {
        public string CommitId { get; init; }
    }
}
