using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

namespace Evergreen.Widgets
{
    public class CommitList : TreeWidget, IDisposable
    {
        private TreeStore _store;

        public event EventHandler<CommitSelectedEventArgs> CommitSelected;

        public CommitList(TreeView view, GitService git) : base(view, git)
        {
            _view.CursorChanged += CommitListCursorChanged;

            var messageColumn = Columns.Create("Message", 0, 800);
            var authorColumn = Columns.Create("Author", 1);
            var shaColumn = Columns.Create("Sha", 2);
            var dateColumn = Columns.Create("Date", 3, 20);
            var idColumn = Columns.Create("ID", 3, 20);

            messageColumn.Resizable = true;
            authorColumn.Resizable = true;
            shaColumn.Resizable = true;
            dateColumn.Resizable = true;

            idColumn.Visible = false;

            foreach (var column in _view.Columns)
            {
                _view.RemoveColumn(column);
            }

            _view.AppendColumn(messageColumn);
            _view.AppendColumn(authorColumn);
            _view.AppendColumn(shaColumn);
            _view.AppendColumn(dateColumn);
            _view.AppendColumn(idColumn);
        }

        public void Refresh()
        {
            var commits = _git.GetCommits();
            var heads = _git.GetBranchHeadCommits();

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

            _store = new TreeStore(
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
                var sha = commit.Sha.Substring(0, 7);
                var id = commit.Id.Sha;

                _store.AppendValues(
                    message,
                    author,
                    sha,
                    commitDate,
                    id
                );
            }

            _view.Model = _store;
        }

        private void CommitListCursorChanged(object sender, EventArgs args)
        {
            var selectedId = _view.GetSelected<string>(4);

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
            _view.CursorChanged -= CommitListCursorChanged;
        }
    }

    public class CommitSelectedEventArgs : EventArgs
    {
        public string CommitId { get; set; }
    }
}
