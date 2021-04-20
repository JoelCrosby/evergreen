using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;

using Gtk;

namespace Evergreen.Widgets
{
    public class CommitList : IDisposable
    {
        private GitService Git { get; }
        private TreeView View { get; }
        private TreeStore store;

        public event EventHandler<CommitSelectedEventArgs> CommitSelected;

        public CommitList(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }

        public CommitList Build()
        {
            View.CursorChanged += CommitListCursorChanged;

            if (View.Columns.Length == 0)
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

                View.AppendColumn(messageColumn);
                View.AppendColumn(authorColumn);
                View.AppendColumn(shaColumn);
                View.AppendColumn(dateColumn);
                View.AppendColumn(idColumn);
            }

            return this;
        }

        public void Refresh()
        {
            var commits = Git.GetCommits();

            store = new TreeStore(
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
            View.Selection.SelectedForeach((model, _, iter) =>
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
            View.CursorChanged -= CommitListCursorChanged;

            GC.SuppressFinalize(this);
        }
    }

    public class CommitSelectedEventArgs : EventArgs
    {
        public string CommitId { get; set; }
    }
}
