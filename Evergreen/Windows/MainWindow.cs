using System;

using Evergreen.Utils;
using Evergreen.Widgets;
using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using Gtk;
using GtkSource;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree = null;
        [UI] private readonly TreeView commitList = null;
        [UI] private readonly Button openRepo = null;
        [UI] private readonly Button fetch = null;
        [UI] private readonly Button pull = null;
        [UI] private readonly Button push = null;
        [UI] private readonly Button createBranch = null;
        [UI] private readonly Button search = null;
        [UI] private readonly Button about = null;
        [UI] private readonly AboutDialog aboutDialog = null;
        [UI] private readonly TreeView commitFiles = null;
        [UI] private readonly Label commitShaLabel = null;
        [UI] private readonly Label commitFileLabel = null;
        [UI] private readonly Label commitAuthorLabel = null;
        [UI] private readonly HeaderBar headerBar = null;
        [UI] private readonly InfoBar infoBar = null;
        [UI] private readonly Label infoMessage = null;
        [UI] private readonly SearchBar searchBar = null;

        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }

        private BranchTree BranchTreeWidget;
        private CommitList CommitListWidget;
        private CommitFiles CommitFilesWidget;
        private CommitFileChanges CommitFileChangesWidget;
        private MessageBar MessageBarWidget;

        private SourceView CommitFileSourceView;

        public MainWindow() : this(new Builder("MainWindow.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            BuildDiffView(builder);

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepoClicked;
            fetch.Clicked += FetchClicked;
            pull.Clicked += PullClicked;
            push.Clicked += PushClicked;
            search.Clicked += SearchClicked;
            about.Clicked += AboutClicked;
            aboutDialog.ButtonPressEvent += AboutClose;
            createBranch.Clicked += CreateBranchClicked;

            Titlebar = headerBar;

            RenderSession(RestoreSession.LoadSession());
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(ActiveSession);
            Application.Quit();
        }

        private void RenderSession(RepositorySession session)
        {
            ActiveSession = session;

            Git = new GitService(session);

            Title = $"{session.RepositoryFriendlyName} - Evergreen";
            headerBar.Title = $"{session.RepositoryFriendlyName} - Evergreen";

            commitShaLabel.Text = string.Empty;
            commitFileLabel.Text = string.Empty;

            BranchTreeWidget = new BranchTree(branchTree, Git).Build();
            CommitListWidget = new CommitList(commitList, Git).Build();
            CommitFilesWidget = new CommitFiles(commitFiles, Git).Build();
            CommitFileChangesWidget = new CommitFileChanges(CommitFileSourceView, Git).Build();
            MessageBarWidget = new MessageBar(infoBar, infoMessage).Build();

            CommitListWidget.CommitSelected += CommitSelected;
            BranchTreeWidget.CheckoutClicked += CheckoutClicked;
            BranchTreeWidget.FastForwardClicked += FastforwardClicked;
            BranchTreeWidget.DeleteClicked += DeleteBranchClicked;
            CommitFilesWidget.CommitFileSelected += CommitFileSelected;

            RestoreSession.SaveSession(ActiveSession);
        }

        private void BuildDiffView(Builder builder)
        {
            var paned = builder.GetObject("commitFilesDiffPanned") as Paned;

            var (sourceView, scroller) = SourceViews.Create();

            CommitFileSourceView = sourceView;
            paned.Pack2(scroller, true, true);
        }

        private void OpenRepoClicked(object sender, EventArgs _)
        {
            var (response, dialog) = Dialogs.Open(this, "Open Reposiory", FileChooserAction.SelectFolder);

            if (response == ResponseType.Accept)
            {
                var session = new RepositorySession
                {
                    Path = dialog.Filename,
                };

                RenderSession(session);
            }

            dialog.Dispose();
        }

        private async void FetchClicked(object sender, EventArgs _)
        {
            var result = await Git.Fetch();

            if (!result.IsSuccess)
            {
                await MessageBarWidget.Open(result.Message);
            }
            else
            {
                await MessageBarWidget.Open("Fetch complete.");
            }

            Refresh();
            CommitListWidget.Refresh();
        }

        private async void PullClicked(object sender, EventArgs _)
        {
            await Git.Pull();

            Refresh();
            CommitListWidget.Refresh();
        }

        private void SearchClicked(object sender, EventArgs _)
        {
            searchBar.SearchModeEnabled = true;
        }

        private async void PushClicked(object sender, EventArgs _)
        {
            await Git.Push();

            Refresh();
            CommitListWidget.Refresh();
        }

        private void AboutClicked(object sender, EventArgs _)
        {
            aboutDialog.Show();
        }

        private void AboutClose(object sender, EventArgs _)
        {
            aboutDialog.Hide();
        }

        private void CreateBranchClicked(object sender, EventArgs _)
        {

        }

        private void CheckoutClicked(object sender, BranchClickedEventArgs e)
        {
            Git.Checkout(e.Branch);
            Refresh();
        }

        private void FastforwardClicked(object sender, BranchClickedEventArgs e)
        {
            Git.FastForwad(e.Branch);
            Refresh();
            CommitListWidget.Refresh();
        }

        private async void DeleteBranchClicked(object sender, BranchClickedEventArgs e)
        {
            await Git.DeleteBranch(e.Branch);

            Refresh();
            CommitListWidget.Refresh();
        }

        private void CommitSelected(object sender, CommitSelectedEventArgs e)
        {
            if (CommitFilesWidget.Update(e.CommitId))
            {
                commitShaLabel.Text = $"CommitId: {e.CommitId}";
                commitFileLabel.Text = string.Empty;
                commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void CommitFileSelected(object sender, CommitFileSelectedEventArgs e)
        {
            if (CommitFileChangesWidget.Render(e.CommitChanges, e.CommitId, e.Path))
            {
                commitShaLabel.Text = $"CommitId: {e.CommitId}";
                commitFileLabel.Text = $"File: {System.IO.Path.GetFileName(e.Path)}";
                commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void Refresh()
        {
            BranchTreeWidget.Refresh();
        }
    }
}
