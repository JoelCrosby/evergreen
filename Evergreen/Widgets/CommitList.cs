using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class CommitList : TreeWidget, IDisposable
    {
        private TreeStore store;

        private enum Column
        {
            Message = 0,
            Author = 1,
            Sha = 2,
            Date = 3,
            ID = 4,
        }

        public CommitList(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += CommitListCursorChanged;

            var messageColumn = Columns.Create("Message", (int)Column.Message, 800, isFixed: true);
            var authorColumn = Columns.Create("Author", (int)Column.Author, isFixed: true);
            var shaColumn = Columns.Create("Sha", (int)Column.Sha, isFixed: true);
            var dateColumn = Columns.Create("Date", (int)Column.Date, 20, isFixed: true);
            var idColumn = Columns.Create("ID", (int)Column.ID, 20, isFixed: true, isHidden: true);

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

            Console.WriteLine("GetCommits {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();

            var heads = Git.GetBranchHeadCommits();

            Console.WriteLine("GetBranchHeadCommits {0}ms", sw.ElapsedMilliseconds);

            sw.Restart();

            var headDict = heads.Aggregate(
                new Dictionary<string, List<string>>(), (a, c) =>
                {
                    if (a.TryGetValue(c.sha, out var item))
                    {
                        item.Add(c.label);
                    }
                    else
                    {
                        a.Add(
                            c.sha, new List<string>
                            {
                                c.label,
                            }
                        );
                    }

                    return a;
                }
            );

            Console.WriteLine("build headDict {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();

            store = new TreeStore(
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

                var commitDate = commit.Author.When.ToString("dd MMM yyyy HH:mm");
                var author = commit.Author.Name;
                var message = CommitMessageShort(branchLabel, commit);
                var sha = commit.Sha[..7];
                var id = commit.Id.Sha;

                store.AppendValues(
                    message,
                    author,
                    sha,
                    commitDate,
                    id
                );
            }

            Console.WriteLine("store AppendValues {0}ms", sw.ElapsedMilliseconds);

            View.Model = store;
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

        protected virtual void OnCommitSelected(CommitSelectedEventArgs e) => CommitSelected?.Invoke(this, e);
    }

    public class CommitSelectedEventArgs : EventArgs
    {
        public string CommitId { get; init; }
    }
}
