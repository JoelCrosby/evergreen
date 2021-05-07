using Evergreen.Lib.Git;

using Gtk;

namespace Evergreen.Widgets.Common
{
    public abstract class TreeWidget
    {
        protected GitService Git { get; init; }
        protected TreeView View { get; init; }

        protected TreeWidget(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }
    }
}
