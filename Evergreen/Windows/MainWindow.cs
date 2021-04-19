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
using Evergreen.Dialogs;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree;
        [UI] private readonly TreeView commitList;
        [UI] private readonly Button openRepo;
        [UI] private readonly Button fetch;
        [UI] private readonly Button pull;
        [UI] private readonly Button push;
        [UI] private readonly Button btnCreateBranch;
        [UI] private readonly Button search;
        [UI] private readonly Button about;
        [UI] private readonly AboutDialog aboutDialog;
        [UI] private readonly TreeView commitFiles;
        [UI] private readonly Label commitShaLabel;
        [UI] private readonly Label commitFileLabel;
        [UI] private readonly Label commitAuthorLabel;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly InfoBar infoBar;
        [UI] private readonly Label infoMessage;
        [UI] private readonly SearchBar searchBar;
        [UI] private readonly Paned commitFilesDiffPanned;

        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }

        private BranchTree branchTreeWidget;
        private CommitList commitListWidget;
        private CommitFiles commitFilesWidget;
        private CommitFileChanges commitFileChangesWidget;
        private MessageBar messageBarWidget;
        private CreateBranch createBranchDialog;

        private SourceView commitFileSourceView;

        public MainWindow() : this(new Builder("main.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("main").Handle)
        {
            builder.Autoconnect(this);

            BuildDiffView();

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepoClicked;
            fetch.Clicked += FetchClicked;
            pull.Clicked += PullClicked;
            push.Clicked += PushClicked;
            search.Clicked += SearchClicked;
            about.Clicked += AboutClicked;
            aboutDialog.ButtonPressEvent += AboutClose;
            btnCreateBranch.Clicked += CreateBranchClicked;

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
            headerBar.Subtitle = Git.GetPath();

            commitShaLabel.Text = string.Empty;
            commitFileLabel.Text = string.Empty;

            branchTreeWidget = new BranchTree(branchTree, Git).Build();
            commitListWidget = new CommitList(commitList, Git).Build();
            commitFilesWidget = new CommitFiles(commitFiles, Git).Build();
            commitFileChangesWidget = new CommitFileChanges(commitFileSourceView, Git).Build();
            messageBarWidget = new MessageBar(infoBar, infoMessage).Build();
            createBranchDialog = new CreateBranch().Build(Git);

            commitListWidget.CommitSelected += CommitSelected;
            branchTreeWidget.CheckoutClicked += CheckoutClicked;
            branchTreeWidget.FastForwardClicked += FastforwardClicked;
            branchTreeWidget.DeleteClicked += DeleteBranchClicked;
            commitFilesWidget.CommitFileSelected += CommitFileSelected;

            createBranchDialog.BranchCreated += BranchCreated;

            RestoreSession.SaveSession(ActiveSession);
        }

        private void BuildDiffView()
        {
            var (sourceView, scroller) = SourceViews.Create();

            commitFileSourceView = sourceView;
            commitFilesDiffPanned.Pack2(scroller, true, true);
        }

        private void OpenRepoClicked(object sender, EventArgs _)
        {
            var (response, dialog) = FileChooser.Open(this, "Open Reposiory", FileChooserAction.SelectFolder);

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
                await messageBarWidget.Open(result.Message);
            }
            else
            {
                await messageBarWidget.Open("Fetch complete.");
            }

            RefreshBranchTree();
            RefreshCommitList();
        }

        private async void PullClicked(object sender, EventArgs _)
        {
            await Git.Pull();

            RefreshBranchTree();
            RefreshCommitList();
        }

        private void SearchClicked(object sender, EventArgs _)
        {
            searchBar.SearchModeEnabled = true;
        }

        private async void PushClicked(object sender, EventArgs _)
        {
            await Git.Push();

            RefreshBranchTree();
            RefreshCommitList();
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
            createBranchDialog.Show();
        }

        private void CheckoutClicked(object sender, BranchClickedEventArgs e)
        {
            Git.Checkout(e.Branch);
            RefreshBranchTree();
        }

        private void FastforwardClicked(object sender, BranchClickedEventArgs e)
        {
            Git.FastForwad(e.Branch);

            RefreshBranchTree();
            RefreshCommitList();
        }

        private async void DeleteBranchClicked(object sender, BranchClickedEventArgs e)
        {
            await Git.DeleteBranch(e.Branch);

            RefreshBranchTree();
            RefreshCommitList();
        }

        private void CommitSelected(object sender, CommitSelectedEventArgs e)
        {
            if (commitFilesWidget.Update(e.CommitId))
            {
                commitShaLabel.Text = $"CommitId: {e.CommitId}";
                commitFileLabel.Text = string.Empty;
                commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void CommitFileSelected(object sender, CommitFileSelectedEventArgs e)
        {
            if (commitFileChangesWidget.Render(e.CommitChanges, e.CommitId, e.Path))
            {
                commitShaLabel.Text = $"CommitId: {e.CommitId}";
                commitFileLabel.Text = $"File: {System.IO.Path.GetFileName(e.Path)}";
                commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void BranchCreated(object sender, CreateBranchEventArgs e)
        {
            Git.CreateBranch(e.Name, e.Checkout);

            RefreshBranchTree();
            RefreshCommitList();
        }

        private void RefreshBranchTree()
        {
            branchTreeWidget.Refresh();
        }

        private void RefreshCommitList()
        {
            commitListWidget.Refresh();
        }
    }
}
