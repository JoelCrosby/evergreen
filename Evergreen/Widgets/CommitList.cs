using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;

using Gtk;
using Evergreen.Widgets.Common;

namespace Evergreen.Widgets
{
    public class CommitList : TreeWidget, IDisposable
    {
        private TreeStore _store;

        public event EventHandler<CommitSelectedEventArgs> CommitSelected;

        public CommitList(TreeView view, GitService git) : base(view, git)
        {
        }

        public CommitList Build()
        {
            _view.CursorChanged += CommitListCursorChanged;

            if (_view.Columns.Length == 0)
            {
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

                _view.AppendColumn(messageColumn);
                _view.AppendColumn(authorColumn);
                _view.AppendColumn(shaColumn);
                _view.AppendColumn(dateColumn);
                _view.AppendColumn(idColumn);
            }

            return this;
        }

        public void Refresh()
        {
            var commits = _git.GetCommits();

            _store = new TreeStore(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            foreach (var commit in commits)
            {
                var commitDate = $"{commit.Author.When:dd MMM yyyy HH:mm}";
                var author = commit.Author.Name;
                var message = commit.MessageShort;
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
            _view.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedId = (string)model.GetValue(iter, 4);

                if (string.IsNullOrEmpty(selectedId))
                {
                    return;
                }

                OnCommitSelected(new CommitSelectedEventArgs
                {
                    CommitId = selectedId,
                });
            });
        }

        protected virtual void OnCommitSelected(CommitSelectedEventArgs e)
        {
            var handler = CommitSelected;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
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
