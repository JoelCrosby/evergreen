using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;

using Gtk;
using System.IO;
using LibGit2Sharp;
using Evergreen.Widgets.Common;

namespace Evergreen.Widgets
{
    public class CommitFiles : TreeWidget, IDisposable
    {
        private TreeStore _store;
        private string _commitId;
        private TreeChanges _commitChanges;

        public event EventHandler<CommitFileSelectedEventArgs> CommitFileSelected;

        public CommitFiles(TreeView view, GitService git) : base(view, git)
        {
        }

        public CommitFiles Build()
        {
            _view.CursorChanged += CommitFilesCursorChanged;

            if (_view.Columns.Length == 0)
            {
                var nameColumn = Columns.Create("Filename", 0);
                var pathColumn = Columns.Create("Path", 0, null, true);

                _view.AppendColumn(nameColumn);
                _view.AppendColumn(pathColumn);
            }

            return this;
        }

        public bool Update(string commitId)
        {
            if (_commitId == commitId)
            {
                return false;
            }

            _commitChanges = _git.GetCommitFiles(commitId);

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

            _view.Model = _store;
            _commitId = commitId;

            return true;
        }

        public bool Clear()
        {
           _view.Model = null;

            return true;
        }

        private void CommitFilesCursorChanged(object sender, EventArgs args)
        {
            _view.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedPath = (string)model.GetValue(iter, 1);

                if (string.IsNullOrEmpty(selectedPath))
                {
                    return;
                }

                if (_commitId is null)
                {
                    return;
                }

                OnCommitFileSelected(new CommitFileSelectedEventArgs
                {
                    CommitId = _commitId,
                    Path = selectedPath,
                    CommitChanges = _commitChanges,
                });
            });
        }

        protected virtual void OnCommitFileSelected(CommitFileSelectedEventArgs e)
        {
            var handler = CommitFileSelected;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
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
            _view.CursorChanged -= CommitFilesCursorChanged;
        }
    }

    public class CommitFileSelectedEventArgs : EventArgs
    {
        public string CommitId { get; set; }

        public TreeChanges CommitChanges { get; set; }

        public string Path { get; set; }
    }
}

