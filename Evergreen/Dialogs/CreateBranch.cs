using Evergreen.Lib.Git;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

using Gtk;

namespace Evergreen.Dialogs
{
    public class CreateBranch : Dialog
    {
        private GitService Git { get; set; }

        public CreateBranch() : this(new Builder("create-branch.ui")) { }

        private CreateBranch(Builder builder) : base(builder.GetObject("create-branch").Handle)
        {
            builder.Autoconnect(this);
        }

        public CreateBranch Build(GitService git)
        {
            Git = git;

            return this;
        }

        public new Result<CreateBranchResult> Show()
        {
            base.Show();

            return Result<CreateBranchResult>.Success();
        }
    }
}
