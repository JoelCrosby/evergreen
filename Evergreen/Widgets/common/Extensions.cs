using System.Collections.Generic;

using Gtk;

namespace Evergreen.Widgets.Common
{
    public static class Extensions
    {
        public static T GetSelected<T>(this TreeView treeView, int index = 0)
        {
            treeView.Selection.GetSelected(out var model, out var iter);

            return (T)model?.GetValue(iter, index);
        }

        public static List<T> GetAllSelected<T>(this TreeView treeView, int index = 0)
        {
            var selectedList = new List<T>();

            treeView.Selection.SelectedForeach((model, _, iter) =>
            {
                var selected = (T)model.GetValue(iter, index);

                selectedList.Add(selected);
            });

            return selectedList;
        }
    }
}
