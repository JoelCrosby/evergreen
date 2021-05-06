using System;
using System.Collections.Generic;
using System.Linq;

using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

namespace Evergreen.Widgets
{
    public class CommitList : TreeWidget, IDisposable
    {
        private TreeStore store;

        public event EventHandler<CommitSelectedEventArgs> CommitSelected;

        public CommitList(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += CommitListCursorChanged;

            var messageColumn = Columns.Create("Message", 0, 800);
            var authorColumn = Columns.Create("Author", 1);
            var shaColumn = Columns.Create("Sha", 2);
            var dateColumn = Columns.Create("Date", 3, 20);
            var idColumn = Columns.Create("ID", 4, 20);

            messageColumn.Resizable = true;
            authorColumn.Resizable = true;
            shaColumn.Resizable = true;
            dateColumn.Resizable = true;

            idColumn.Visible = false;

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

        public void Refresh()
        {
            var commits = Git.GetCommits();
            var heads = Git.GetBranchHeadCommits();

            var headDict = heads.Aggregate(new Dictionary<string, List<string>>(), (a, c) =>
            {
                if (a.TryGetValue(c.sha, out var item))
                {
                    item.Add(c.label);
                }
                else
                {
                    a.Add(c.sha, new List<string> { c.label });
                }

                return a;
            });

            store = new TreeStore(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            foreach (var commit in commits)
            {
                var hasValue = headDict.TryGetValue(commit.Sha, out var branches);
                var branchLabel = hasValue ? string.Join(' ', branches.Select(b => $"({b})")) : null;

                var commitDate = $"{commit.Author.When:dd MMM yyyy HH:mm}";
                var author = commit.Author.Name;
                var message = branchLabel is { } ? $"{commit.MessageShort} {branchLabel}" : commit.MessageShort;
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

            View.Model = store;
        }

        private void CommitListCursorChanged(object sender, EventArgs args)
        {
            var selectedId = View.GetSelected<string>(4);

            if (string.IsNullOrEmpty(selectedId))
            {
                return;
            }

            OnCommitSelected(new CommitSelectedEventArgs
            {
                CommitId = selectedId,
            });
        }

        protected virtual void OnCommitSelected(CommitSelectedEventArgs e)
        {
            CommitSelected?.Invoke(this, e);
        }

        public void Dispose()
        {
            View.CursorChanged -= CommitListCursorChanged;
        }
    }

    public class CommitSelectedEventArgs : EventArgs
    {
        public string CommitId { get; init; }
    }
}
