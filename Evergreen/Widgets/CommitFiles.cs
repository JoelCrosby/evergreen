using System;
using System.IO;

using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class CommitFiles : TreeWidget, IDisposable
    {
        private TreeStore store;
        private string commitId;
        private TreeChanges commitChanges;

        public event EventHandler<CommitFileSelectedEventArgs> CommitFileSelected;

        public CommitFiles(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += CommitFilesCursorChanged;

            var nameColumn = Columns.Create("Filename", 0);
            var pathColumn = Columns.Create("Path", 0, null, true);

            View.AppendColumn(nameColumn);
            View.AppendColumn(pathColumn);
        }

        public bool Update(string commitOid)
        {
            if (commitId == commitOid)
            {
                return false;
            }

            commitChanges = Git.GetCommitFiles(commitOid);

            store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in commitChanges)
            {
                store.AppendValues(
                    GetFileLabel(change),
                    change.Path
                );
            }

            View.Model = store;
            commitId = commitOid;

            return true;
        }

        public bool Clear()
        {
            View.Model = null;

            return true;
        }

        private void CommitFilesCursorChanged(object sender, EventArgs args)
        {
            if (commitId is null)
            {
                return;
            }

            var selectedPath = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            OnCommitFileSelected(new CommitFileSelectedEventArgs
            {
                CommitId = commitId,
                Path = selectedPath,
                CommitChanges = commitChanges,
            });
        }

        protected virtual void OnCommitFileSelected(CommitFileSelectedEventArgs e)
        {
            CommitFileSelected?.Invoke(this, e);
        }

        private static string GetFileLabel(TreeEntryChanges change)
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

        public void Dispose()
        {
            View.CursorChanged -= CommitFilesCursorChanged;
        }
    }

    public class CommitFileSelectedEventArgs : EventArgs
    {
        public string CommitId { get; set; }

        public TreeChanges CommitChanges { get; set; }

        public string Path { get; set; }
    }
}

