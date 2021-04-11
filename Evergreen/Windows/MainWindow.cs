using System;

using Evergreen.Utils;
using Evergreen.Widgets;
using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using Gtk;
using GtkSource;

using LibGit2Sharp;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree = null;
        [UI] private readonly TreeView commitList = null;
        [UI] private readonly Button openRepo = null;
        [UI] private readonly TextBuffer diffBuffer = null;
        [UI] private readonly TreeView commitFiles = null;
        [UI] private readonly Label branchLabel = null;

        private TreeChanges commitChanges;

        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            BuildDiffView(builder);

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepo_Clicked;

            RenderSession(RestoreSession.LoadSession());
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(ActiveSession);
            Application.Quit();
        }

        private void OpenRepo_Clicked(object sender, EventArgs _)
        {
            var (response, dialog) = Dialogs.Open(this, "Open Reposiory", FileChooserAction.SelectFolder);

            if (response == ResponseType.Accept)
            {
                var session = new RepositorySession
                {
                    Path = dialog.CurrentFolder,
                };

                RenderSession(session);
            }

            dialog.Dispose();
        }

        private void RenderSession(RepositorySession session)
        {
            ActiveSession = session;

            Git = new GitService(session);

            Title = $"{session.RepositoryFriendlyName} - Evergreen";


            var branchTreeWidget = new BranchTree(branchTree, Git).Build();
            var commitListWidget = new CommitList(commitList, Git).Build();
            var commitFilesWidget = new CommitFiles(commitFiles, Git).Build();

            commitListWidget.CommitSelected += (_, e) => commitFilesWidget.Update(e.CommitId);

            branchTreeWidget.CheckoutClicked += (_, e) => {
                Git.Checkout(e.Branch);
                branchTreeWidget.Refresh();
                RefreshStatusBar();
            };

            RefreshStatusBar();
        }

        private void  RefreshStatusBar()
        {
            branchLabel.Text = Git.GetHeadFriendlyName();
        }

        private void BuildDiffView(Builder builder)
        {
            var paned = builder.GetObject("commitFilesDiffPanned") as Paned;
            var sourceView = new GtkSource.SourceView();

            sourceView.TabWidth = 4;
            sourceView.Editable = false;
            sourceView.SetSizeRequest(400, 4000);

            paned.Pack2(sourceView, true, true);
        }
    }
}
