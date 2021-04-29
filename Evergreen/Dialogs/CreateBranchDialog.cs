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

        [UI] private readonly Button btnCancel;
        [UI] private readonly Button btnCreate;
        [UI] private readonly CheckButton checkCheckout;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly Label labelErrorText;
        [UI] private readonly Entry entryBranchName;

#pragma warning restore 064

        private GitService _git { get; set; }

        public event EventHandler<CreateBranchEventArgs> BranchCreated;

        public CreateBranchDialog(GitService git) : this(git, new Builder("create-branch.ui")) { }

        private CreateBranchDialog(GitService git, Builder builder) : base(builder.GetObject("create-branch").Handle)
        {
            _git = git;

            builder.Autoconnect(this);

            btnCancel.Clicked += CancelClicked;
            btnCreate.Clicked += CreateClicked;
        }

        public new Result<CreateBranchResult> Show()
        {
            headerBar.Subtitle = $"Base - {_git.GetHeadFriendlyName()}";
            checkCheckout.Active = true;

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
            var name = entryBranchName.Text;
            var checkout = checkCheckout.Active;

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

            OnBranchCreated(new CreateBranchEventArgs
            {
                Name = name,
                Checkout = checkout,
            });

            Hide();
        }

        private async void ShowError(string message)
        {
            labelErrorText.Text = message;
            labelErrorText.Visible = true;

            await Task.Delay(3000).ConfigureAwait(false);

            labelErrorText.Visible = false;
        }

        private void Reset()
        {
            labelErrorText.Text = null;
            labelErrorText.Visible = false;
            entryBranchName.Text = null;
            checkCheckout.Active = false;
        }

        protected virtual void OnBranchCreated(CreateBranchEventArgs e)
        {
            var handler = BranchCreated;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        public new void Dispose()
        {
            base.Dispose();

            btnCancel.Clicked -= CancelClicked;
            btnCreate.Clicked -= CreateClicked;
        }
    }

    public class CreateBranchEventArgs : EventArgs
    {
        public string Name { get; init; }

        public bool Checkout { get; init; }
    }
}
