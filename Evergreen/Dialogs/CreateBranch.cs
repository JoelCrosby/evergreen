using System.Threading.Tasks;
using System;

using Evergreen.Lib.Git;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Dialogs
{
    public class CreateBranch : Dialog
    {
        [UI] private readonly Button btnCancel;
        [UI] private readonly Button btnCreate;
        [UI] private readonly CheckButton checkCheckout;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly Label labelErrorText;
        [UI] private readonly Entry entryBranchName;

        public event EventHandler<CreateBranchEventArgs> BranchCreated;

        private GitService Git { get; set; }

        public CreateBranch() : this(new Builder("create-branch.ui")) { }

        private CreateBranch(Builder builder) : base(builder.GetObject("create-branch").Handle)
        {
            builder.Autoconnect(this);

            btnCancel.Clicked += CancelClicked;
            btnCreate.Clicked += CreateClicked;
        }

        public CreateBranch Build(GitService git)
        {
            Git = git;

            return this;
        }

        public new Result<CreateBranchResult> Show()
        {
            headerBar.Subtitle = $"Base - {Git.GetHeadFriendlyName()}";

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
    }

    public class CreateBranchEventArgs : EventArgs
    {
        public string Name { get; init; }

        public bool Checkout { get; init; }
    }
}