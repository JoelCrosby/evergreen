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
        private TreeStore _store;

        private IEnumerable<StatusEntry> changes;

        public event EventHandler<FilesSelectedEventArgs> FilesSelected;

        public StagedFiles(TreeView view, GitService git) : base(view, git)
        {
            _view.CursorChanged += OnCursorChanged;

            var nameColumn = Columns.Create("Staged", 0);
            var pathColumn = Columns.Create("Path", 0, null, true);

            _view.AppendColumn(nameColumn);
            _view.AppendColumn(pathColumn);
        }

        public bool Update()
        {
            changes = _git.GetStagedFiles();

            _store = new TreeStore(
                typeof(string),
                typeof(string)
            );

            foreach (var change in changes)
            {
                _store.AppendValues(
                    GetFileLabel(change),
                    change.FilePath
                );
            }

            _view.Model = _store;

            return true;
        }

        public bool Clear()
        {
            _view.Model = null;

            return true;
        }

        private void OnCursorChanged(object sender, EventArgs args)
        {
            var selectedFiles = new List<string>();

            _view.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedPath = (string)model.GetValue(iter, 1);

                if (string.IsNullOrEmpty(selectedPath))
                {
                    return;
                }

                selectedFiles.Add(selectedPath);
            });

            OnFilesSelected(new FilesSelectedEventArgs
            {
                Paths = selectedFiles,
                CommitChanges = _git.GetChangedFiles(),
            });
        }

        protected virtual void OnFilesSelected(FilesSelectedEventArgs e)
        {
            var handler = FilesSelected;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        private static string GetFileLabel(StatusEntry change)
        {
            var name = Path.GetFileName(change.FilePath);
            var prefix = change.State switch
            {
                FileStatus.NewInIndex => "[A]",
                FileStatus.Conflicted => "[CF]",
                FileStatus.DeletedFromWorkdir => "[D]",
                FileStatus.Ignored => "[I]",
                FileStatus.ModifiedInIndex => "[M]",
                FileStatus.RenamedInIndex => "[R]",
                FileStatus.TypeChangeInIndex => "[TC]",
                FileStatus.Unaltered => "[UA]",
                FileStatus.Unreadable => "[UR]",
                FileStatus.Nonexistent => "[NE]",
                _ => "[Unknown]",
            };

            return $"{prefix} {name}";
        }

        public void Dispose()
        {
            _view.CursorChanged -= OnCursorChanged;
        }
    }
}

