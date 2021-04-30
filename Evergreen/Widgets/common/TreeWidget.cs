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

        protected T GetSelected<T>(int index = 0)
        {
            _view.Selection.GetSelected(out var model, out var iter);

            return (T)model?.GetValue(iter, index);
        }

        protected List<T> GetAllSelected<T>(int index = 0)
        {
            var selectedList = new List<T>();

            _view.Selection.SelectedForeach((model, _, iter) =>
            {
                var selected = (T)model.GetValue(iter, index);

                selectedList.Add(selected);
            });

            return selectedList;
        }
    }
}
