using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Evergreen.Dialogs;
using Evergreen.Core.Events;
using Evergreen.Core.Git;
using Evergreen.Utils;

using Gtk;

using GtkSource;

using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Widgets
{
    public class Repository : Box
    {
#pragma warning disable 0649

        [UI] private readonly TreeView _branchTree;
        [UI] private readonly TreeView _commitList;
        [UI] private readonly TreeView _stagedList;
        [UI] private readonly TreeView _changedList;
        [UI] private readonly Button _commit;
        [UI] private readonly Entry _commitMessage;
        [UI] private readonly TreeView _commitFiles;
        [UI] private readonly Label _commitShaLabel;
        [UI] private readonly Label _commitFileLabel;
        [UI] private readonly Label _commitAuthorLabel;
        [UI] private readonly InfoBar _infoBar;
        [UI] private readonly Label _infoMessage;
        [UI] private readonly Paned _commitFilesDiffPanned;
        [UI] private readonly Paned _commitsListView;
        [UI] private readonly Paned _changesListView;
        [UI] private readonly Box _changesSourceBox;
        [UI] private readonly Stack _changesViewStack;

#pragma warning restore 064

        public string WorkingDir { get; }
        public GitService Git { get; set; }

        private BranchTree _branchTreeWidget;
        private StagedFiles _stagedFilesWidget;
        private ChangedFiles _changedFilesWidget;
        private CommitList _commitListWidget;
        private CommitFiles _commitFilesWidget;
        private CommitFileChanges _commitFileChangesWidget;
        private CommitFileChanges _changesFileChangesWidget;
        private MessageBar _messageBarWidget;
        private CreateBranchDialog _createBranchDialog;

        private readonly SourceView _commitFileSourceView;
        private readonly SourceView _changesFileSourceView;

        private Stopwatch _focusedTimer;

        public Repository(string workingDir) : this(workingDir, new Builder("repository.ui")) { }

        private Repository(string workingDir, Builder builder) : base(builder.GetObject("repository").Handle)
        {
            builder.Autoconnect(this);

            WorkingDir = workingDir;

            _commitFileSourceView = BuildDiffView(_commitFilesDiffPanned);
            _changesFileSourceView = BuildDiffView(_changesSourceBox);

            _commit.Clicked += CommitClicked;

            RenderSession(workingDir);
        }

        public async void OnFocus()
        {
            if (_focusedTimer?.ElapsedMilliseconds < 30 * 1000)
            {
                _focusedTimer.Restart();

                return;
            }

            _focusedTimer = new Stopwatch();

            await Refresh();

            _stagedFilesWidget.Update();
            _changedFilesWidget.Update();
        }

        private void RenderSession(string path)
        {
            Git = new GitService(path);

            // Cleanup widgets
            _createBranchDialog?.Dispose();
            _commitFilesWidget?.Dispose();
            _branchTreeWidget?.Dispose();
            _stagedFilesWidget?.Dispose();
            _changedFilesWidget?.Dispose();
            _commitListWidget?.Dispose();
            _messageBarWidget?.Dispose();

            // Evergreen widgets
            _createBranchDialog = new CreateBranchDialog(Git);
            _branchTreeWidget = new BranchTree(_branchTree, Git);
            _commitListWidget = new CommitList(_commitList, Git);
            _stagedFilesWidget = new StagedFiles(_stagedList, Git);
            _changedFilesWidget = new ChangedFiles(_changedList, Git);
            _commitFilesWidget = new CommitFiles(_commitFiles, Git);
            _commitFileChangesWidget = new CommitFileChanges(_commitFileSourceView);
            _changesFileChangesWidget = new CommitFileChanges(_changesFileSourceView);
            _messageBarWidget = new MessageBar(_infoBar, _infoMessage);

            // Evergreen widget events
            _createBranchDialog.BranchCreated += BranchCreated;
            _commitListWidget.CommitSelected += CommitSelected;
            _branchTreeWidget.CheckoutClicked += CheckoutClicked;
            _branchTreeWidget.FastForwardClicked += FastForwardClicked;
            _branchTreeWidget.DeleteClicked += DeleteBranchClicked;
            _branchTreeWidget.ChangesSelected += ChangesSelected;
            _branchTreeWidget.BranchSelected += BranchSelected;
            _branchTreeWidget.MergeClicked += MergeClicked;
            _commitFilesWidget.CommitFileSelected += CommitFileSelected;
            _changedFilesWidget.FilesSelected += ChangedFileSelected;
            _changedFilesWidget.FilesStaged += ChangedFilesStaged;
            _stagedFilesWidget.FilesUnStaged += FilesUnStaged;

            _commitShaLabel.Text = string.Empty;
            _commitFileLabel.Text = string.Empty;

            _commitFilesWidget.Clear();
            _commitFileChangesWidget.Clear();
            _changesFileChangesWidget.Clear();
        }

        public void SetPanedPosition(int height)
        {
            _changesListView.Position = height - (height / 6);
            _commitsListView.Position = height - (height / 3);
        }

        private static SourceView BuildDiffView(Widget parent)
        {
            var (sourceView, scroller) = SourceViews.Create();

            switch (parent)
            {
                case Paned panned:
                    panned.Pack2(scroller, true, true);
                    break;
                case Box box:
                    box.PackStart(scroller, true, true, 0);
                    break;
                default:
                    return sourceView;
            }

            return sourceView;
        }

        private async void CommitClicked(object sender, EventArgs _)
        {
            var message = _commitMessage.Text;

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Git.Commit(message);

            _commitMessage.Text = string.Empty;

            _stagedFilesWidget.Update();
            _changedFilesWidget.Update();

            await Refresh();
        }

        private void CheckoutClicked(object sender, BranchSelectedEventArgs e)
        {
            Git.Checkout(e.Branch);
            RefreshBranchTree();
        }

        private async void FastForwardClicked(object sender, BranchSelectedEventArgs e)
        {
            var result = await Git.FastForward(e.Branch);

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Error($"Fast forward failed. {result.Message}");
            }
            else
            {
                await Refresh();
            }
        }

        private async void MergeClicked(object sender, BranchSelectedEventArgs e)
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
                await _messageBarWidget.Error("Failed to merge branch.");
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
                "Delete branch",
                $"Are you sure you want to delete the branch {e.Branch}",
                "Delete"
            );

            if (!confirm)
            {
                return;
            }

            var result = await Git.DeleteBranch(e.Branch);

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Error($"Failed to delete branch {e.Branch}.");
            }
            else
            {
                await Refresh();

                await _messageBarWidget.Open($"Branch {e.Branch} deleted.");
            }
        }

        private void ChangesSelected(object sender, EventArgs e) => ChangeView(ChangesView.ChangesList);

        private void BranchSelected(object sender, EventArgs e) => ChangeView(ChangesView.CommitList);

        private void CommitSelected(object sender, CommitSelectedEventArgs e)
        {
            if (_commitFilesWidget.Update(e.CommitId))
            {
                _commitShaLabel.Text = $"CommitId: {e.CommitId}";
                _commitFileLabel.Text = string.Empty;
                _commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private void CommitFileSelected(object sender, CommitFileSelectedEventArgs e)
        {
            var diff = Git.GetCommitDiff(e.CommitId, e.Path);

            if (_commitFileChangesWidget.Render(diff, e.CommitId, e.Path))
            {
                _commitShaLabel.Text = $"CommitId: {e.CommitId}";
                _commitFileLabel.Text = $"File: {System.IO.Path.GetFileName(e.Path)}";
                _commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
            }
        }

        private async void ChangedFileSelected(object sender, FilesSelectedEventArgs e)
        {
            var headCommit = Git.GetHeadCommit()?.Sha;
            var diff = await Git.GetChangesDiff(e.Paths.FirstOrDefault());

            _changesFileChangesWidget.Render(diff, headCommit, e.Paths.FirstOrDefault());
        }

        private void ChangedFilesStaged(object sender, FilesSelectedEventArgs e)
        {
            Git.Stage(e.Paths);

            _changedFilesWidget.Update();
            _stagedFilesWidget.Update();
        }

        private void FilesUnStaged(object sender, FilesSelectedEventArgs e)
        {
            Git.UnStage(e.Paths);

            _changedFilesWidget.Update();
            _stagedFilesWidget.Update();
        }

        private async void BranchCreated(object sender, CreateBranchEventArgs e)
        {
            var result = Git.CreateBranch(e.Name, e.Checkout);

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Error("Failed to create new branch.");
            }
            else
            {
                await Refresh();

                await _messageBarWidget.Open("Branch created.");
            }
        }

        public async void PushClicked(object sender, EventArgs _)
        {
            var result = await Git.Push();

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Error($"Push failed. {result.Message}");
            }
            else
            {
                await Refresh();

                await _messageBarWidget.Open("Push complete.");
            }
        }

        public async void FetchClicked(object sender, EventArgs _)
        {
            var result = await Git.Fetch();

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Open(result.Message);
            }
            else
            {
                await Refresh();

                await _messageBarWidget.Open("Fetch complete.");
            }
        }

        public async void PullClicked(object sender, EventArgs _)
        {
            var result = await Git.Pull();

            if (!result.IsSuccess)
            {
                await _messageBarWidget.Error($"Pull failed. {result.Message}");
            }
            else
            {
                await Refresh();

                await _messageBarWidget.Open("Pull complete.");
            }
        }

        public void CreateBranchClicked(object sender, EventArgs _) => _createBranchDialog.Show();

        private Task Refresh() => Task.WhenAll(RefreshBranchTree(), RefreshCommitList());

        private Task RefreshBranchTree() => Task.Run(_branchTreeWidget.Refresh);

        private Task RefreshCommitList() => Task.Run(_commitListWidget.Refresh);

        private void ChangeView(ChangesView view)
        {
            if (view == ChangesView.ChangesList)
            {
                _stagedFilesWidget.Update();
                _changedFilesWidget.Update();

                _changesViewStack.SetVisibleChildFull("changesViewContainer", StackTransitionType.None);

                return;
            }

            _changesViewStack.SetVisibleChildFull("commitsViewContainer", StackTransitionType.None);
        }
    }

    public enum ChangesView
    {
        CommitList = 0,
        ChangesList = 1,
    }
}
