using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using Gtk;
using System.IO;
using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class CommitFiles
    {
        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }
        private TreeView View { get; set; }
        private TreeStore store;

        public event EventHandler<CommitFileSelectedEventArgs> CommitFileSelected;

        private string CommitId;

        public CommitFiles(TreeView view, GitService git)
        {
            View = view;
            Git = git;
            ActiveSession = Git.Session;
        }

        public CommitFiles Build()
        {
            View.CursorChanged += CommitFilesCursorChanged;

            if (View.Columns.Length == 0)
            {
                var nameColumn = Columns.Create("Filename", 0);
                var pathColumn = Columns.Create("Path", 0, null, true);

                View.AppendColumn(nameColumn);
                View.AppendColumn(pathColumn);
            }

            return this;
        }

        public CommitFiles Update(string commitId)
        {
            var commitChanges = Git.GetCommitFiles(commitId);

            store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in commitChanges)
            {
                store.AppendValues(
                    getFileLabel(change),
                    change.Path
                );
            }

            View.Model = store;
            CommitId = commitId;

            return this;
        }

        private void CommitFilesCursorChanged(object sender, EventArgs args)
        {
            View.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedPath = (string)model.GetValue(iter, 1);

                if (string.IsNullOrEmpty(selectedPath))
                {
                    return;
                }

                if (CommitId is null)
                {
                    return;
                }

                OnCommitFileSelected(new CommitFileSelectedEventArgs
                {
                    CommitId = CommitId,
                    Path = selectedPath,
                });
            });
        }

        protected virtual void OnCommitFileSelected(CommitFileSelectedEventArgs e)
        {
            EventHandler<CommitFileSelectedEventArgs> handler = CommitFileSelected;

            if (handler is null) return;

            handler(this, e);
        }

        private static string getFileLabel(TreeEntryChanges change)
        {
            var name = Path.GetFileName(change.Path);
            var prefix = change.Status switch
            {
                ChangeKind.Added => "[A]",
                ChangeKind.Conflicted => "[CF]",
                ChangeKind.Copied => "[C]",
                ChangeKind.Deleted => "[D]",
                ChangeKind.Ignored => "[I]",
                ChangeKind.Modified => "[M]",
                ChangeKind.Renamed => "[R]",
                ChangeKind.TypeChanged => "[TC]",
                ChangeKind.Unmodified => "[UM]",
                ChangeKind.Unreadable => "[UR]",
                ChangeKind.Untracked => "[UT]",
                _ => "[Unknown]",
            };

            return $"{prefix} {name}";
        }
    }

    public class CommitFileSelectedEventArgs : EventArgs
    {
        public string CommitId { get; set; }

        public string Path { get; set; }
    }
}

