using System;
using System.Collections.Generic;
using System.Linq;

using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
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

            var messageColumn = Columns.CreateLane("Message", 0, 800);
            var authorColumn = Columns.Create("Author", 2);
            var shaColumn = Columns.Create("Sha", 3);
            var dateColumn = Columns.Create("Date", 4, 20);
            var idColumn = Columns.Create("ID", 5, 20);

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
                typeof(CommitModel),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            foreach (var commit in commits)
            {
                var hasValue = headDict.TryGetValue(commit.Data.Sha, out var branches);
                var branchLabel = hasValue ? string.Join(' ', branches.Select(b => $"({b})")) : null;

                var commitDate = $"{commit.Data.Author.When:dd MMM yyyy HH:mm}";
                var author = commit.Data.Author.Name;
                var message = branchLabel is { } ? $"{commit.Data.MessageShort} {branchLabel}" : commit.Data.MessageShort;
                var sha = commit.Data.Sha.Substring(0, 7);
                var id = commit.Data.Id.Sha;

                store.AppendValues(
                    message,
                    commit,
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
            var selectedId = View.GetSelected<string>(5);

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
        public string CommitId { get; set; }
    }
}
