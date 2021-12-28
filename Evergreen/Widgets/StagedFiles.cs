using System;
using System.Collections.Generic;
using System.IO;

using Evergreen.Core.Events;
using Evergreen.Core.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class StagedFiles : TreeWidget, IDisposable
    {
        private IEnumerable<StatusEntry> _changes;
        private TreeStore _store;

        public StagedFiles(TreeView view, GitService git) : base(view, git)
        {
            View.CursorChanged += OnCursorChanged;
            View.RowActivated += OnRowActivated;

            var nameColumn = Columns.Create("Staged", 0, isFixed: true);
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
        public event EventHandler<FilesSelectedEventArgs> FilesUnStaged;

        public bool Update()
        {
            _changes = Git.GetStagedFiles();

            _store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in _changes)
            {
                _store.AppendValues(
                    GetFileLabel(change),
                    change.FilePath
                );
            }

            View.Model = _store;

            return true;
        }

        public bool Clear()
        {
            View.Model = null;

            return true;
        }

        private void OnCursorChanged(object sender, EventArgs args)
        {
            var selectedFiles = View.GetAllSelected<string>(1);

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

            OnFilesUnStaged(
                new FilesSelectedEventArgs
                {
                    Paths = selectedFiles,
                }
            );
        }

        private void OnFilesSelected(FilesSelectedEventArgs e) => FilesSelected?.Invoke(this, e);

        private void OnFilesUnStaged(FilesSelectedEventArgs e) => FilesUnStaged?.Invoke(this, e);

        private static string GetFileLabel(StatusEntry change)
        {
            var name = Path.GetFileName(change.FilePath);

            var prefix = change.State switch
            {
                FileStatus.NewInIndex => "(A)",
                FileStatus.Conflicted => "(CF)",
                FileStatus.DeletedFromWorkdir => "(D)",
                FileStatus.Ignored => "(I)",
                FileStatus.ModifiedInIndex => "(M)",
                FileStatus.RenamedInIndex => "(R)",
                FileStatus.DeletedFromIndex => "(D)",
                FileStatus.TypeChangeInIndex => "(TC)",
                FileStatus.Unaltered => "(UA)",
                FileStatus.Unreadable => "(UR)",
                FileStatus.Nonexistent => "(NE)",
                _ => "(Unknown)",
            };

            return $"{prefix} {name}";
        }
    }
}
