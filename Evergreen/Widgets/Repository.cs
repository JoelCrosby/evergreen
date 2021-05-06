using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Evergreen.Dialogs;
using Evergreen.Lib.Events;
using Evergreen.Lib.Git;
using Evergreen.Utils;

using Gtk;

using GtkSource;

using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Widgets
{
    public class Repository : Box
    {
#pragma warning disable 0649

        [UI] private readonly TreeView branchTree;
        [UI] private readonly TreeView commitList;
        [UI] private readonly TreeView stagedList;
        [UI] private readonly TreeView changedList;
        [UI] private readonly Button commit;
        [UI] private readonly Entry commitMessage;
        [UI] private readonly TreeView commitFiles;
        [UI] private readonly Label commitShaLabel;
        [UI] private readonly Label commitFileLabel;
        [UI] private readonly Label commitAuthorLabel;
        [UI] private readonly InfoBar infoBar;
        [UI] private readonly Label infoMessage;
        [UI] private readonly Paned commitFilesDiffPanned;
        [UI] private readonly Paned commitsListView;
        [UI] private readonly Paned changesListView;
        [UI] private readonly Box changesSourceBox;
        [UI] private readonly Stack changesViewStack;

#pragma warning restore 064

        public string Path { get; }
        public GitService Git { get; set; }

        private BranchTree branchTreeWidget;
        private StagedFiles stagedFilesWidget;
        private ChangedFiles changedFilesWidget;
        private CommitList commitListWidget;
        private CommitFiles commitFilesWidget;
        private CommitFileChanges commitFileChangesWidget;
        private CommitFileChanges changesFileChangesWidget;
        private MessageBar messageBarWidget;
        private CreateBranchDialog createBranchDialog;

        private readonly SourceView commitFileSourceView;
        private readonly SourceView changesFileSourceView;

        private Stopwatch focusedTimer;

        public Repository(string path) : this(path, new Builder("repository.ui")) { }

        private Repository(string path, Builder builder) : base(builder.GetObject("repository").Handle)
        {
            builder.Autoconnect(this);

            Path = path;

            commitFileSourceView = BuildDiffView(commitFilesDiffPanned);
            changesFileSourceView = BuildDiffView(changesSourceBox);

            commit.Clicked += CommitClicked;

            RenderSession(path);
        }

        public async void OnFocus()
        {
            if (focusedTimer?.ElapsedMilliseconds < 30 * 1000)
            {
                focusedTimer.Restart();

                return;
            }
            else
            {
                focusedTimer = new Stopwatch();
            }

            await Refresh();

            stagedFilesWidget.Update();
            changedFilesWidget.Update();
        }

        private void RenderSession(string path)
        {
            Git = new GitService(path);

            // Cleanup widgets
            createBranchDialog?.Dispose();
            commitFilesWidget?.Dispose();
            branchTreeWidget?.Dispose();
            stagedFilesWidget?.Dispose();
            changedFilesWidget?.Dispose();
            commitListWidget?.Dispose();
            messageBarWidget?.Dispose();

            // Evergreen widgets
            createBranchDialog = new CreateBranchDialog(Git);
            branchTreeWidget = new BranchTree(branchTree, Git);
            commitListWidget = new CommitList(commitList, Git);
            stagedFilesWidget = new StagedFiles(stagedList, Git);
            changedFilesWidget = new ChangedFiles(changedList, Git);
            commitFilesWidget = new CommitFiles(commitFiles, Git);
            commitFileChangesWidget = new CommitFileChanges(commitFileSourceView);
            changesFileChangesWidget = new CommitFileChanges(changesFileSourceView);
            messageBarWidget = new MessageBar(infoBar, infoMessage);

            // Evergreen widget events
            createBranchDialog.BranchCreated += BranchCreated;
            commitListWidget.CommitSelected += CommitSelected;
            branchTreeWidget.CheckoutClicked += CheckoutClicked;
            branchTreeWidget.FastForwardClicked += FastforwardClicked;
            branchTreeWidget.DeleteClicked += DeleteBranchClicked;
            branchTreeWidget.ChangesSelected += ChangesSelected;
            branchTreeWidget.BranchSelected += BranchSelected;
            branchTreeWidget.MergeClicked += MergeClicked;
            commitFilesWidget.CommitFileSelected += CommitFileSelected;
            changedFilesWidget.FilesSelected += ChangedFileSelected;
            changedFilesWidget.FilesStaged += ChangedFilesStaged;
            stagedFilesWidget.FilesUnStaged += FilesUnStaged;

            commitShaLabel.Text = string.Empty;
            commitFileLabel.Text = string.Empty;

            commitFilesWidget.Clear();
            commitFileChangesWidget.Clear();
            changesFileChangesWidget.Clear();
        }

        public void SetPanedPosition(int height)
        {
            changesListView.Position = height - (height / 6);
            commitsListView.Position = height - (height / 3);
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

        private async void CommitClicked(object sender, EventArgs _)
        {
            var message = commitMessage.Text;

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Git.Commit(message);

            commitMessage.Text = string.Empty;

            stagedFilesWidget.Update();
            changedFilesWidget.Update();

            await Refresh();
        }

        private void CheckoutClicked(object sender, BranchSelectedEventArgs e)
        {
            Git.Checkout(e.Branch);
            RefreshBranchTree();
        }

        private async void FastforwardClicked(object sender, BranchSelectedEventArgs e)
        {
            var result = await Git.FastForwad(e.Branch);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error("Failed to delete branch.");
            }
            else
            {
                await Refresh();
            }
        }

        private async void MergeClicked(object sender, BranchSelectedEventArgs e)
        {
            var result = await Git.FastForwad(e.Branch);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error("Failed to delete branch.");
            }
            else
            {
                await Refresh();
            }
        }

        private async void DeleteBranchClicked(object sender, BranchSelectedEventArgs e)
        {
            var confirm = ConfirmationDialog.Open(
                Program.Window,
                "Merge branch",
                $"Are you sure you want to merge the branch {e.Branch}",
                "Merge"
            );

            if (!confirm)
            {
                return;
            }

            var result = Git.MergeBranch(e.Branch);

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Failed to delete branch {e.Branch}.");
            }
            else
            {
                await Refresh();

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

        private async void ChangedFileSelected(object sender, FilesSelectedEventArgs e)
        {
            var headCommit = Git.GetHeadCommit().Sha;
            var diff = await Git.GetChangesDiff(e.Paths.FirstOrDefault());

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
                await Refresh();

                await messageBarWidget.Open("Branch created.");
            }
        }

        public async void PushClicked(object sender, EventArgs _)
        {
            var result = await Git.Push();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Push failed. {result.Message}");
            }
            else
            {
                await Refresh();

                await messageBarWidget.Open("Push complete.");
            }
        }

        public async void FetchClicked(object sender, EventArgs _)
        {
            var result = await Git.Fetch();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Open(result.Message);
            }
            else
            {
                await Refresh();

                await messageBarWidget.Open("Fetch complete.");
            }
        }

        public async void PullClicked(object sender, EventArgs _)
        {
            var result = await Git.Pull();

            if (!result.IsSuccess)
            {
                await messageBarWidget.Error($"Pull failed. {result.Message}");
            }
            else
            {
                await Refresh();

                await messageBarWidget.Open("Pull complete.");
            }
        }

        public void CreateBranchClicked(object sender, EventArgs _)
        {
            createBranchDialog.Show();
        }

        private Task Refresh() => Task.WhenAll(RefreshBranchTree(), RefreshCommitList());
        private Task RefreshBranchTree() => Task.Run(branchTreeWidget.Refresh);
        private Task RefreshCommitList() => Task.Run(commitListWidget.Refresh);

        private void ChangeView(ChangesView view)
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
