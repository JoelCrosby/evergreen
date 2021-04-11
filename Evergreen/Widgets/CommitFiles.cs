using System;

using Evergreen.Utils;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using Gtk;

namespace Evergreen.Widgets
{
    public class CommitFiles
    {
        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }
        private TreeView View { get; set; }
        private TreeStore store;

        public CommitFiles(TreeView view, GitService git)
        {
            View = view;
            Git = git;
            ActiveSession = Git.Session;
        }

        public CommitFiles Build()
        {
            View.CursorChanged += CommitFilesCursorChanged;

            if (View.Columns.Length == 0)
            {
                var nameColumn = Columns.Create("Filename", 0);

                View.AppendColumn(nameColumn);
            }

            return this;
        }

        public CommitFiles Update(string commitId)
        {
            var commitChanges = Git.GetCommitFiles(commitId);

            store = new TreeStore(
                typeof(string)
            );

            foreach (var change in commitChanges)
            {
                store.AppendValues(
                    System.IO.Path.GetFileName(change.Path)
                );
            }

            View.Model = store;

            return this;
        }

        private void CommitFilesCursorChanged(object sender, EventArgs args)
        {
            View.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedPath = (string)model.GetValue(iter, 1);

                if (string.IsNullOrEmpty(selectedPath))
                {
                    return;
                }
            });
        }
    }
}

