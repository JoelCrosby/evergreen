using System;
using System.IO;

using Evergreen.Lib.Common;
using Evergreen.Lib.Events;
using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class ChangedFiles : TreeWidget, IDisposable
    {
        private TreeStore store;
        private TreeChanges changes;

        public TreeMode Mode { get; }

        public event EventHandler<FilesSelectedEventArgs> FilesSelected;
        public event EventHandler<FilesSelectedEventArgs> FilesStaged;

        public ChangedFiles(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += OnCursorChanged;
            View.RowActivated += OnRowActivated;

            var nameColumn = Columns.Create("Changes", 0);
            var pathColumn = Columns.Create("Path", 0, null, true);

            View.AppendColumn(nameColumn);
            View.AppendColumn(pathColumn);
        }

        public bool Update()
        {
            UpdateList();
            SelectFirst();

            return true;
        }

        private void UpdateList()
        {
            changes = Git.GetChangedFiles();

            store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in changes)
            {
                store.AppendValues(
                    GetFileLabel(change),
                    change.Path
                );
            }

            View.Model = store;
        }

        private void UpdateTree()
        {
            // TODO: Implement a tree view for files
        }

        private void SelectFirst()
        {
            store.GetIterFirst(out var iter);

            var selected = View.GetSelected<string>();

            if (selected is null && iter is { })
            {
                View.Selection.SelectIter(iter);
            }
        }

        public bool Clear()
        {
            View.Model = null;

            return true;
        }

        private void OnCursorChanged(object sender, EventArgs args)
        {
            var selectedFiles = View.GetAllSelected<string>(1);

            if (selectedFiles.Count == 0)
            {
                return;
            }

            OnFilesSelected(new FilesSelectedEventArgs
            {
                Paths = selectedFiles,
            });
        }

        private void OnRowActivated(object sender, RowActivatedArgs args)
        {
            var selectedFiles = View.GetAllSelected<string>(1);

            if (selectedFiles.Count == 0)
            {
                return;
            }

            OnFilesStaged(new FilesSelectedEventArgs
            {
                Paths = selectedFiles,
            });
        }

        protected virtual void OnFilesSelected(FilesSelectedEventArgs e)
        {
            FilesSelected?.Invoke(this, e);
        }

        protected virtual void OnFilesStaged(FilesSelectedEventArgs e)
        {
            FilesStaged?.Invoke(this, e);
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
            View.CursorChanged -= OnCursorChanged;
            View.RowActivated -= OnRowActivated;
        }
    }
}

