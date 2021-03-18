using System;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

using Gtk;

using LibGit2Sharp;

using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Gtk.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree = null;
        [UI] private readonly TreeView commitList = null;
        [UI] private readonly HeaderBar headerBar = null;
        [UI] private readonly MenuItem openRepo = null;
        [UI] private readonly TextBuffer diffBuffer = null;
        [UI] private readonly TextView commitFileDiff = null;
        [UI] private readonly TreeView commitFiles = null;

        private TreeStore branchTreeStore;
        private ListStore commitListStore;
        private ListStore commitFilesStore;

        private TreeChanges CommitChanges;

        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            Titlebar = headerBar;

            DeleteEvent += Window_DeleteEvent;
            openRepo.Activated += OpenRepo_Clicked;

            commitList.CursorChanged += CommitListCursorChanged;

            RenderSession(RestoreSession.LoadSession());
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(ActiveSession);

            Application.Quit();
        }

        private void OpenRepo_Clicked(object sender, EventArgs _)
        {
            var filechooser = new FileChooserNative(
                "Open Reposiory",
                this,
                FileChooserAction.SelectFolder,
                "Open",
                "Cancel"
            );

            var response = (ResponseType)filechooser.Run();

            if (response == ResponseType.Accept)
            {
                var session = new RepositorySession
                {
                    Path = filechooser.CurrentFolder,
                };

                RenderSession(session);
            }

            filechooser.Dispose();
        }

        private void CommitListCursorChanged(object sender, EventArgs args)
        {
            commitList.Selection.SelectedForeach((model, _, iter) =>
            {
                var selectedId = (string)model.GetValue(iter, 4);

                if (string.IsNullOrEmpty(selectedId))
                {
                    return;
                }

                CommitChanges = Git.GetCommitFiles(selectedId);

                BuildCommitChangesList();
            });
        }

        private void RenderSession(RepositorySession session)
        {
            if (session is null)
            {
                return;
            }

            ActiveSession = session;

            Git = new GitService(session);

            if (ActiveSession.UseNativeTitleBar)
            {
                Titlebar = null;
            }

            BuildBranchTree();
            BuildCommitList();

            Title = $"{session.RepositoryFriendlyName} - Evergreen";
        }

        private void BuildBranchTree()
        {
            var branches = Git.GetBranchTree();

            // Init cells
            var cellName = new CellRendererText();

            if (branchTree.Columns.Length == 0)
            {
                // Init columns
                var labelColumn = new TreeViewColumn
                {
                    Title = "Branches",
                };

                labelColumn.PackStart(cellName, true);
                labelColumn.AddAttribute(cellName, "text", 0);
                labelColumn.AddAttribute(cellName, "weight", 2);

                branchTree.AppendColumn(labelColumn);

                var nameColumn = new TreeViewColumn
                {
                    Title = "CanonicalName",
                    Visible = false,
                };

                branchTree.AppendColumn(nameColumn);
            }

            // Init treeview
            branchTreeStore = new TreeStore(typeof(string), typeof(string), typeof(int));
            branchTree.Model = branchTreeStore;

            var headCanonicalName = Git.GetHeadCanonicalName();

            void AddTreeItems(TreeIter parentIter, TreeItem<BranchTreeItem> item)
            {
                var weight = item.Item.Name == headCanonicalName ? Pango.Weight.Bold : Pango.Weight.Normal;

                var treeIter = branchTreeStore.AppendValues(
                    parentIter,
                    item.Item.Label,
                    item.Item.Name,
                    weight
                );

                foreach (var child in item.Children)
                {
                    AddTreeItems(treeIter, child);
                }
            }

            var iter = branchTreeStore.AppendValues(ActiveSession.RepositoryFriendlyName);

            foreach (var b in branches)
            {
                AddTreeItems(iter, b);
            }

            branchTree.ExpandAll();

            branchTree.EnableSearch = true;
        }

        private void BuildCommitList()
        {
            var commits = Git.GetCommits();

            if (commitList.Columns.Length == 0)
            {
                var messageColumn = CreateColumn("Message", 0, 800);
                var authorColumn = CreateColumn("Author", 1);
                var shaColumn = CreateColumn("Sha", 2);
                var dateColumn = CreateColumn("Date", 3, 20);
                var idColumn = CreateColumn("ID", 3, 20);

                messageColumn.Resizable = true;
                authorColumn.Resizable = true;
                shaColumn.Resizable = true;
                dateColumn.Resizable = true;

                idColumn.Visible = false;

                commitList.AppendColumn(messageColumn);
                commitList.AppendColumn(authorColumn);
                commitList.AppendColumn(shaColumn);
                commitList.AppendColumn(dateColumn);
                commitList.AppendColumn(dateColumn);
            }

            commitListStore = new ListStore(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            foreach (var commit in commits)
            {
                var commitDate = $"{commit.Author.When:dd MMM yyyy HH:mm}";
                var author = commit.Author.Name;
                var message = commit.MessageShort;
                var sha = commit.Sha.Substring(0, 7);
                var id = commit.Id.Sha;

                commitListStore.AppendValues(
                    message,
                    author,
                    sha,
                    commitDate,
                    id
                );
            }

            commitList.Model = commitListStore;
        }

        private void BuildCommitChangesList()
        {
            if (commitFiles.Columns.Length == 0)
            {
                var nameColumn = CreateColumn("Filename", 0);

                commitFiles.AppendColumn(nameColumn);
            }

            commitFilesStore = new ListStore(
                typeof(string)
            );

            foreach (var change in CommitChanges)
            {
                commitFilesStore.AppendValues(
                    System.IO.Path.GetFileName(change.Path)
                );
            }

            commitFiles.Model = commitFilesStore;
        }

        private TreeViewColumn CreateColumn(string title, int index, int? maxWidth = null)
        {
            var cell = new CellRendererText();

            var column = new TreeViewColumn
            {
                Title = title,
            };

            column.PackStart(cell, true);
            column.AddAttribute(cell, "text", index);

            if (maxWidth.HasValue)
            {
                column.MaxWidth = maxWidth.Value;
            }

            return column;
        }
    }
}
