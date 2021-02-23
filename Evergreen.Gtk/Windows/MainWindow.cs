using System;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Gtk.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree = null;
        [UI] private readonly TreeView commitList = null;
        [UI] private readonly HeaderBar headerBar = null;
        [UI] private readonly Button openRepo = null;

        private TreeStore branchTreeStore;
        private ListStore commitListStore;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            Titlebar = headerBar;

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepo_Clicked;

            RenderSession(RestoreSession.LoadSession());
        }

        public RepositorySession ActiveSession { get; set; }

        public GitService Git { get; private set; }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(ActiveSession);

            Application.Quit();
        }

        private void OpenRepo_Clicked(object sender, EventArgs a)
        {
            var filechooser = new FileChooserDialog(
                "Open Reposiory",
                this, FileChooserAction.SelectFolder,
                "Cancel", ResponseType.Cancel,
                "Open", ResponseType.Accept
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

        private void RenderSession(RepositorySession session)
        {
            if (session is null)
            {
                return;
            }

            ActiveSession = session;

            Git = new GitService(session);

            BuildBranchTree();
            BuildCommitList();

            headerBar.Title = $"{session.RepositoryFriendlyName} - Evergreen.Gtk";
        }

        private void BuildBranchTree()
        {
            var branches = Git.GetBranchTree();

            // Init cells
            var cellName = new CellRendererText();

            if (branchTree.Columns.Length == 0)
            {
                // Init columns
                var columeSections = new TreeViewColumn
                {
                    Title = "Branches",
                };

                columeSections.PackStart(cellName, true);
                columeSections.AddAttribute(cellName, "text", 0);

                branchTree.AppendColumn(columeSections);
            }

            // Init treeview
            branchTreeStore = new TreeStore(typeof(string));
            branchTree.Model = branchTreeStore;

            void AddTreeItems(TreeIter parentIter, TreeItem<BranchTreeItem> item)
            {
                var treeIter = branchTreeStore.AppendValues(parentIter, item.Item.Label);

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
                var messageColumn = CreateColumn("Message", 0);
                var authorColumn = CreateColumn("Author", 1);
                var shaColumn = CreateColumn("Sha", 2);
                var dateColumn = CreateColumn("Date", 3, 20);

                messageColumn.Resizable = true;
                authorColumn.Resizable = true;
                shaColumn.Resizable = true;
                dateColumn.Resizable = true;

                commitList.AppendColumn(messageColumn);
                commitList.AppendColumn(authorColumn);
                commitList.AppendColumn(shaColumn);
                commitList.AppendColumn(dateColumn);
            }

            commitListStore = new ListStore(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
            );

            commitList.Model = commitListStore;

            foreach (var commit in commits)
            {
                var commitDate = $"{commit.Author.When:dd MMM yyyy HH:mm}";
                var author = commit.Author.Name;
                var message = commit.MessageShort;
                var sha = commit.Sha.Substring(0, 7);

                commitListStore.AppendValues(
                    message,
                    author,
                    sha,
                    commitDate
                );
            }
        }

        private static TreeViewColumn CreateColumn(string title, int index, int? maxWidth = null)
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
