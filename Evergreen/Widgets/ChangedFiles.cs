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
    public class ChangedFiles : IDisposable
    {
        private GitService Git { get; }
        private TreeView View { get; }
        private TreeStore store;

        public event EventHandler<FilesSelectedEventArgs> FilesSelected;

        private TreeChanges changes;

        public ChangedFiles(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }

        public ChangedFiles Build()
        {
            View.CursorChanged += OnCursorChanged;

            if (View.Columns.Length == 0)
            {
                var nameColumn = Columns.Create("Changes", 0);
                var pathColumn = Columns.Create("Path", 0, null, true);

                View.AppendColumn(nameColumn);
                View.AppendColumn(pathColumn);
            }

            return this;
        }

        public bool Update()
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
                CommitChanges = changes,
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
        }
    }
}

