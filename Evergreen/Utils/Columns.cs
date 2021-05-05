using Evergreen.Renderers;

using Gtk;

namespace Evergreen.Utils
{
    public static class Columns
    {
        public static TreeViewColumn Create(string title, int index, int? maxWidth = null, bool isHidden = false)
        {
            var cell = new CellRendererText();

            var column = new TreeViewColumn
            {
                Title = title,
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

        public static TreeViewColumn CreateLane(string title, int index, int? maxWidth = null, bool isHidden = false)
        {
            var cell = new CellRendererLanes();

            var column = new TreeViewColumn
            {
                Title = title,
            };

            column.PackStart(cell, true);
            column.AddAttribute(cell, "text", index);
            column.AddAttribute(cell, "commit", index + 1);

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
