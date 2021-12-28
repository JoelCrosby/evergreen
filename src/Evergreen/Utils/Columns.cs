using Gtk;

namespace Evergreen.Utils
{
    public static class Columns
    {
        public static TreeViewColumn Create(
            string title, int index, int? maxWidth = null, bool isHidden = false, bool isFixed = false)
        {
            var cell = new CellRendererText();

            var sizing = isFixed ? TreeViewColumnSizing.Fixed : TreeViewColumnSizing.Autosize;
            var column = new TreeViewColumn
            {
                Title = title,
                Sizing = sizing,
            };

            column.PackStart(cell, true);
            column.AddAttribute(cell, "text", index);

            if (maxWidth.HasValue)
            {
                column.MaxWidth = maxWidth.Value;
            }

            if (isHidden)
            {
                column.Visible = false;
            }

            return column;
        }
    }
}
