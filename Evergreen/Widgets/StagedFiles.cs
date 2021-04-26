using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;

using Gtk;
using System.IO;
using LibGit2Sharp;
using System.Collections.Generic;
using Evergreen.Lib.Events;

namespace Evergreen.Widgets
{
    public class StagedFiles : IDisposable
    {
        private GitService Git { get; }
        private TreeView View { get; }
        private TreeStore store;

        public event EventHandler<FilesSelectedEventArgs> FilesSelected;

        private IEnumerable<StatusEntry> changes;

        public StagedFiles(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }

        public StagedFiles Build()
        {
            View.CursorChanged += OnCursorChanged;

            if (View.Columns.Length == 0)
            {
                var nameColumn = Columns.Create("Staged", 0);
                var pathColumn = Columns.Create("Path", 0, null, true);

                View.AppendColumn(nameColumn);
                View.AppendColumn(pathColumn);
            }

            return this;
        }

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
            var selectedFiles = new List<string>();

            View.Selection.SelectedForeach((model, _, iter) =>
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
                CommitChanges = Git.GetChangedFiles(),
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
            View.CursorChanged -= OnCursorChanged;
        }
    }
}

