using Evergreen.Lib.Git;

using Gtk;

namespace Evergreen.Widgets.Common
{
    public abstract class TreeWidget
    {
        protected readonly TreeView View;
        protected readonly GitService Git;

        protected TreeWidget(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }
    }
}
