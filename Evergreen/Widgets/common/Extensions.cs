using System.Collections.Generic;

using Gtk;

namespace Evergreen.Widgets.Common
{
    using System;

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

        public static T GetSelectedAtPos<T>(
            this TreeView treeView, double x, double y, int index = 0)
        {
            var xInt = Convert.ToInt32(x);
            var yInt = Convert.ToInt32(y);

            return GetSelectedAtPos<T>(treeView, xInt, yInt, index);
        }

        public static T GetSelectedAtPos<T>(
            this TreeView treeView, int x, int y, int index = 0)
        {
            if (!treeView.GetPathAtPos(x, y, out var path))
            {
                return default;
            }

            var model = treeView.Model;

            if (!model.GetIter(out var iter, path))
            {
                return default;
            }

            return (T)model.GetValue (iter, index);
        }
    }
}
