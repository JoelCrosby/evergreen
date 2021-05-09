using System;
using System.Collections.Generic;
using System.IO;

using Evergreen.Lib.Events;
using Evergreen.Lib.Git;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

using LibGit2Sharp;

namespace Evergreen.Widgets
{
    public class StagedFiles : TreeWidget, IDisposable
    {
        private IEnumerable<StatusEntry> changes;
        private TreeStore store;

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
            changes = Git.GetStagedFiles();

            store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in changes)
            {
                store.AppendValues(
                    GetFileLabel(change),
                    change.FilePath
                );
            }

            View.Model = store;

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

        protected virtual void OnFilesSelected(FilesSelectedEventArgs e) => FilesSelected?.Invoke(this, e);

        protected virtual void OnFilesUnStaged(FilesSelectedEventArgs e) => FilesUnStaged?.Invoke(this, e);

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
