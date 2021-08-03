using System;
using System.IO;

using Evergreen.Core.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class CommitFiles : TreeWidget, IDisposable
    {
        private TreeChanges _commitChanges;
        private string _commitId;
        private TreeStore _store;

        public CommitFiles(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += CommitFilesCursorChanged;

            var nameColumn = Columns.Create("Filename", 0, isFixed: true);
            var pathColumn = Columns.Create("Path", 0, null, true, true);

            View.AppendColumn(nameColumn);
            View.AppendColumn(pathColumn);

            View.FixedHeightMode = true;
        }

        public void Dispose() => View.CursorChanged -= CommitFilesCursorChanged;

        public event EventHandler<CommitFileSelectedEventArgs> CommitFileSelected;

        public bool Update(string commitOid)
        {
            if (_commitId == commitOid)
            {
                return false;
            }

            _commitChanges = Git.GetCommitFiles(commitOid);

            if (_commitChanges is null)
            {
                return false;
            }

            _store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in _commitChanges)
            {
                _store.AppendValues(
                    GetFileLabel(change),
                    change.Path
                );
            }

            View.Model = _store;
            _commitId = commitOid;

            return true;
        }

        public bool Clear()
        {
            View.Model = null;

            return true;
        }

        private void CommitFilesCursorChanged(object sender, EventArgs args)
        {
            if (_commitId is null)
            {
                return;
            }

            var selectedPath = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            OnCommitFileSelected(
                new CommitFileSelectedEventArgs
                {
                    CommitId = _commitId,
                    Path = selectedPath,
                    CommitChanges = _commitChanges,
                }
            );
        }

        protected virtual void OnCommitFileSelected(CommitFileSelectedEventArgs e) =>
            CommitFileSelected?.Invoke(this, e);

        private static string GetFileLabel(TreeEntryChanges change)
        {
            var name = Path.GetFileName(change.Path);

            var prefix = change.Status switch
            {
                ChangeKind.Added => "(A)",
                ChangeKind.Conflicted => "(CF)",
                ChangeKind.Copied => "(C)",
                ChangeKind.Deleted => "(D)",
                ChangeKind.Ignored => "(I)",
                ChangeKind.Modified => "(M)",
                ChangeKind.Renamed => "(R)",
                ChangeKind.TypeChanged => "(TC)",
                ChangeKind.Unmodified => "(UM)",
                ChangeKind.Unreadable => "(UR)",
                ChangeKind.Untracked => "(UT)",
                _ => "(Unknown)",
            };

            return $"{prefix} {name}";
        }
    }

    public class CommitFileSelectedEventArgs : EventArgs
    {
        public string CommitId { get; init; }

        public TreeChanges CommitChanges { get; init; }

        public string Path { get; init; }
    }
}
