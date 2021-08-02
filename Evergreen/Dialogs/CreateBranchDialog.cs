using System;
using System.Threading.Tasks;

using Evergreen.Lib.Git;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

using Gtk;

using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Dialogs
{
    public class CreateBranchDialog : Dialog, IDisposable
    {
#pragma warning disable 0649

        [UI] private readonly Button _btnCancel;
        [UI] private readonly Button _btnCreate;
        [UI] private readonly CheckButton _checkCheckout;
        [UI] private readonly HeaderBar _headerBar;
        [UI] private readonly Label _labelErrorText;
        [UI] private readonly Entry _entryBranchName;

#pragma warning restore 064

        private readonly GitService _git;

        public event EventHandler<CreateBranchEventArgs> BranchCreated;

        public CreateBranchDialog(GitService git) : this(git, new Builder("create-branch.ui")) { }

        private CreateBranchDialog(GitService git, Builder builder) : base(builder.GetObject("create-branch").Handle)
        {
            _git = git;

            builder.Autoconnect(this);

            _btnCancel.Clicked += CancelClicked;
            _btnCreate.Clicked += CreateClicked;
        }

        public new Result<CreateBranchResult> Show()
        {
            _headerBar.Subtitle = $"Base - {_git.GetHeadFriendlyName()}";
            _checkCheckout.Active = true;

            base.Show();

            return Result<CreateBranchResult>.Success();
        }

        public new void Hide()
        {
            Reset();

            base.Hide();
        }

        private void CancelClicked(object sender, EventArgs _) => Hide();

        private void CreateClicked(object sender, EventArgs _)
        {
            var name = _entryBranchName.Text;
            var checkout = _checkCheckout.Active;

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("please enter a name for the branch");
                return;
            }

            if (name.Contains(" "))
            {
                ShowError("name cannot contain spaces");
                return;
            }

            OnBranchCreated(
                new CreateBranchEventArgs
                {
                    Name = name,
                    Checkout = checkout,
                }
            );

            Hide();
        }

        private async void ShowError(string message)
        {
            _labelErrorText.Text = message;
            _labelErrorText.Visible = true;

            await Task.Delay(3000).ConfigureAwait(false);

            _labelErrorText.Visible = false;
        }

        private void Reset()
        {
            _labelErrorText.Text = null;
            _labelErrorText.Visible = false;
            _entryBranchName.Text = null;
            _checkCheckout.Active = false;
        }

        protected virtual void OnBranchCreated(CreateBranchEventArgs e)
        {
            var handler = BranchCreated;

            handler?.Invoke(this, e);
        }

        public new void Dispose()
        {
            base.Dispose();

            _btnCancel.Clicked -= CancelClicked;
            _btnCreate.Clicked -= CreateClicked;
        }
    }

    public class CreateBranchEventArgs : EventArgs
    {
        public string Name { get; init; }

        public bool Checkout { get; init; }
    }
}
