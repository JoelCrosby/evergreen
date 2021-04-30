using System;
using System.Linq;

using Evergreen.Dialogs;
using Evergreen.Lib.Configuration;
using Evergreen.Lib.Events;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;
using Evergreen.Utils;
using Evergreen.Widgets;

using Gtk;

using GtkSource;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
#pragma warning disable 0649

        [UI] private readonly TreeView branchTree;
        [UI] private readonly TreeView commitList;
        [UI] private readonly TreeView stagedList;
        [UI] private readonly TreeView changedList;
        [UI] private readonly Button openRepo;
        [UI] private readonly Button fetch;
        [UI] private readonly Button pull;
        [UI] private readonly Button push;
        [UI] private readonly Button btnCreateBranch;
        [UI] private readonly Button search;
        [UI] private readonly Button about;
        [UI] private readonly TreeView commitFiles;
        [UI] private readonly Label commitShaLabel;
        [UI] private readonly Label commitFileLabel;
        [UI] private readonly Label commitAuthorLabel;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly InfoBar infoBar;
        [UI] private readonly Label infoMessage;
        [UI] private readonly SearchBar searchBar;
        [UI] private readonly Paned commitFilesDiffPanned;
        [UI] private readonly Paned commitsListView;
        [UI] private readonly Paned changesListView;
        [UI] private readonly Box changesSourceBox;
        [UI] private readonly Spinner spinner;
        [UI] private readonly Stack changesViewStack;

#pragma warning restore 064

        private RepositorySession Session { get; set; }
        private GitService Git { get; set; }

        private BranchTree branchTreeWidget;
        private StagedFiles stagedFilesWidget;
        private ChangedFiles changedFilesWidget;
        private CommitList commitListWidget;
        private CommitFiles commitFilesWidget;
        private CommitFileChanges commitFileChangesWidget;
        private CommitFileChanges changesFileChangesWidget;
        private MessageBar messageBarWidget;
        private CreateBranchDialog createBranchDialog;
        private Dialogs.AboutDialog aboutDialog;

        private readonly SourceView commitFileSourceView;
        private readonly SourceView changesFileSourceView;

        public MainWindow() : this(new Builder("main.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("main").Handle)
        {
            builder.Autoconnect(this);

            commitFileSourceView = BuildDiffView(commitFilesDiffPanned);
            changesFileSourceView = BuildDiffView(changesSourceBox);

            // Gtk widget events
            DeleteEvent += WindowDeleteEvent;
            FocusInEvent += WindowFocusGrabbed;
            openRepo.Clicked += OpenRepoClicked;
            fetch.Clicked += FetchClicked;
            pull.Clicked += PullClicked;
            push.Clicked += PushClicked;
            search.Clicked += SearchClicked;
            about.Clicked += AboutClicked;
            btnCreateBranch.Clicked += CreateBranchClicked;

            // Set the clientside headerbar
            Titlebar = headerBar;

            RenderSession(RestoreSession.LoadSession());
        }

        private void WindowDeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(Session);
            Application.Quit();
        }

        private void WindowFocusGrabbed(object sender, FocusInEventArgs a)
        {
            Console.WriteLine("Focused");
        }

        private void RenderSession(RepositorySession session)
        {
            RestoreSession.SaveSession(session);

            Session = session;

            Git = new GitService(session);

            // Cleanup widgets
            commitFilesWidget?.Dispose();
            branchTreeWidget?.Dispose();
            stagedFilesWidget?.Dispose();
            changedFilesWidget?.Dispose();
            commitListWidget?.Dispose();
            messageBarWidget?.Dispose();
            createBranchDialog?.Dispose();
            aboutDialog?.Dispose();

            // Evergreen widgets
            branchTreeWidget = new BranchTree(branchTree, Git);
            commitListWidget = new CommitList(commitList, Git);
            stagedFilesWidget = new StagedFiles(stagedList, Git);
            changedFilesWidget = new ChangedFiles(changedList, Git);
            commitFilesWidget = new CommitFiles(commitFiles, Git);
            commitFileChangesWidget = new CommitFileChanges(commitFileSourceView);
            changesFileChangesWidget = new CommitFileChanges(changesFileSourceView);
            messageBarWidget = new MessageBar(infoBar, infoMessage);
            createBranchDialog = new CreateBranchDialog(Git);
            aboutDialog = new Dialogs.AboutDialog();

            // Evergreen widget events
            commitListWidget.CommitSelected += CommitSelected;
            branchTreeWidget.CheckoutClicked += CheckoutClicked;
            branchTreeWidget.FastForwardClicked += FastforwardClicked;
            branchTreeWidget.DeleteClicked += DeleteBranchClicked;
            branchTreeWidget.ChangesSelected += ChangesSelected;
            branchTreeWidget.BranchSelected += BranchSelected;
            commitFilesWidget.CommitFileSelected += CommitFileSelected;
            createBranchDialog.BranchCreated += BranchCreated;
            changedFilesWidget.FilesSelected += ChangedFileSelected;
            changedFilesWidget.FilesStaged += ChangedFilesStaged;
            stagedFilesWidget.FilesUnStaged += FilesUnStaged;

            // Update titles
            Title = $"{session.RepositoryFriendlyName} - Evergreen";
            headerBar.Title = $"{session.RepositoryFriendlyName} - Evergreen";
            headerBar.Subtitle = Git.GetFreindlyPath();

            commitShaLabel.Text = string.Empty;
            commitFileLabel.Text = string.Empty;

            branchTreeWidget.Refresh();
            commitListWidget.Refresh();
            commitFilesWidget.Clear();
            commitFileChangesWidget.Clear();
            changesFileChangesWidget.Clear();

            SetPanedPosition(commitsListView, 3);
            SetPanedPosition(changesListView, 6);
        }

        private void SetPanedPosition(Paned paned, int ratio)
        {
            GetSize(out var _, out var height);

            paned.Position = height - (height / ratio);
        }

        private static SourceView BuildDiffView(Widget parent)
        {
            var (sourceView, scroller) = SourceViews.Create();

            if (parent is Paned panned)
            {
                panned.Pack2(scroller, true, true);
            }
            else if (parent is Box box)
            {
                box.PackStart(scroller, true, true, 0);
            }

            return sourceView;
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
            ShowSpinner();

            var result = await Git.Fetch();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Open(result.Message);
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();

                await messageBarWidget.Open("Fetch complete.");
            }

            HideSpinner();
        }

        private async void PullClicked(object sender, EventArgs _)
        {
            ShowSpinner();

            var result = await Git.Pull();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Pull failed. {result.Message}");
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();

                await messageBarWidget.Open("Pull complete.");
            }

            HideSpinner();
        }

        private void SearchClicked(object sender, EventArgs _)
        {
            searchBar.SearchModeEnabled = true;
        }

        private async void PushClicked(object sender, EventArgs _)
        {
            ShowSpinner();

            var result = await Git.Push();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Push failed. {result.Message}");
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();

                await messageBarWidget.Open("Push complete.");
            }

            HideSpinner();
        }

        private void AboutClicked(object sender, EventArgs _)
        {
            aboutDialog.Show();
        }

        private void CreateBranchClicked(object sender, EventArgs _)
        {
            createBranchDialog.Show();
        }

        private void CheckoutClicked(object sender, BranchSelectedEventArgs e)
        {
            Git.Checkout(e.Branch);
            RefreshBranchTree();
        }

        private async void FastforwardClicked(object sender, BranchSelectedEventArgs e)
        {
            ShowSpinner();

            var result = await Git.FastForwad(e.Branch);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error("Failed to delete branch.");
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();
            }

            HideSpinner();
        }

        private async void DeleteBranchClicked(object sender, BranchSelectedEventArgs e)
        {
            var result = await Git.DeleteBranch(e.Branch);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Failed to delete branch {e.Branch}.");
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();

                await messageBarWidget.Open($"Branch {e.Branch} deleted.");
            }
        }

        private void ChangesSelected(object sender, EventArgs e)
        {
            ChangeView(ChangesView.ChangesList);
        }

        private void BranchSelected(object sender, EventArgs e)
        {
            ChangeView(ChangesView.CommitList);
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
            var diff = Git.GetCommitDiff(e.CommitId, e.Path);

            if (commitFileChangesWidget.Render(diff, e.CommitId, e.Path))
            {
                commitShaLabel.Text = $"CommitId: {e.CommitId}";
                commitFileLabel.Text = $"File: {System.IO.Path.GetFileName(e.Path)}";
                commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void ChangedFileSelected(object sender, FilesSelectedEventArgs e)
        {
            var headCommit = Git.GetHeadCommit().Sha;
            var diff = Git.GetChangesDiff(e.Paths.FirstOrDefault());

            changesFileChangesWidget.Render(diff, headCommit, e.Paths.FirstOrDefault());
        }

        private void ChangedFilesStaged(object sender, FilesSelectedEventArgs e)
        {
            Git.Stage(e.Paths);

            changedFilesWidget.Update();
            stagedFilesWidget.Update();
        }

        private void FilesUnStaged(object sender, FilesSelectedEventArgs e)
        {
            Git.UnStage(e.Paths);

            changedFilesWidget.Update();
            stagedFilesWidget.Update();
        }

        private async void BranchCreated(object sender, CreateBranchEventArgs e)
        {
            var result = Git.CreateBranch(e.Name, e.Checkout);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error("Failed to create new branch.");
            }
            else
            {
                RefreshBranchTree();
                RefreshCommitList();

                await messageBarWidget.Open("Branch created.");
            }
        }

        private void RefreshBranchTree()
        {
            branchTreeWidget.Refresh();
        }

        private void RefreshCommitList()
        {
            commitListWidget.Refresh();
        }

        private void ShowSpinner()
        {
            spinner.Active = true;
            spinner.Show();
        }

        private void HideSpinner()
        {
            spinner.Hide();
        }

        public void ChangeView(ChangesView view)
        {
            if (view == ChangesView.ChangesList)
            {
                stagedFilesWidget.Update();
                changedFilesWidget.Update();

                changesViewStack.SetVisibleChildFull("changesViewContainer", StackTransitionType.OverRight);

                return;
            }

            changesViewStack.SetVisibleChildFull("commitsViewContainer", StackTransitionType.OverLeft);
        }
    }

    public enum ChangesView
    {
        CommitList = 0,
        ChangesList = 1,
    }
}
