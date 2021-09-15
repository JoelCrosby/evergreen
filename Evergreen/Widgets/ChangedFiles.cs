using System;
using System.IO;

using Evergreen.Core.Events;
using Evergreen.Core.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class ChangedFiles : TreeWidget, IDisposable
    {
        private TreeChanges _changes;
        private TreeStore _store;

        public ChangedFiles(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += OnCursorChanged;
            View.RowActivated += OnRowActivated;

            var nameColumn = Columns.Create("Changes", 0, isFixed: true);
            var pathColumn = Columns.Create("Path", 0, null, true, true);

            View.AppendColumn(nameColumn);
            View.AppendColumn(pathColumn);

            View.HeadersVisible = false;
            View.FixedHeightMode = true;
            View.Selection.Mode = SelectionMode.Multiple;
        }

        public void Dispose()
        {
            View.CursorChanged -= OnCursorChanged;
            View.RowActivated -= OnRowActivated;
        }

        public event EventHandler<FilesSelectedEventArgs> FilesSelected;
        public event EventHandler<FilesSelectedEventArgs> FilesStaged;

        public bool Update()
        {
            UpdateList();
            SelectFirst();

            return true;
        }

        private void UpdateList()
        {
            _changes = Git.GetChangedFiles();

            _store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in _changes)
            {
                _store.AppendValues(
                    GetFileLabel(change),
                    change.Path
                );
            }

            View.Model = _store;
        }

        private void SelectFirst()
        {
            _store.GetIterFirst(out var iter);

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

            OnFilesSelected(
                new FilesSelectedEventArgs
                {
                    Paths = selectedFiles,
                }
            );
        }

        private void OnRowActivated(object sender, RowActivatedArgs args)
        {
            var selectedFiles = View.GetAllSelected<string>(1);

            if (selectedFiles.Count == 0)
            {
                return;
            }

            OnFilesStaged(
                new FilesSelectedEventArgs
                {
                    Paths = selectedFiles,
                }
            );
        }

        private void OnFilesSelected(FilesSelectedEventArgs e) => FilesSelected?.Invoke(this, e);

        private void OnFilesStaged(FilesSelectedEventArgs e) => FilesStaged?.Invoke(this, e);

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
}
