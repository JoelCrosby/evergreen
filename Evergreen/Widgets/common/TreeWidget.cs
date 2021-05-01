using System.Collections.Generic;

using Evergreen.Lib.Git;

using Gtk;

namespace Evergreen.Widgets.Common
{
    public abstract class TreeWidget
    {
        protected readonly TreeView _view;
        protected readonly GitService _git;

        protected TreeWidget(TreeView view, GitService git)
        {
            _view = view;
            _git = git;
        }
    }
}
