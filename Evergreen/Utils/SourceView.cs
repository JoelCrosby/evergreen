using Gtk;

using GtkSource;

namespace Evergreen.Utils
{
    public static class SourceViews
    {
        public static (SourceView, ScrolledWindow) Create()
        {
            var scroller = new ScrolledWindow
            {
                Visible = true
            };

            var sourceView = new SourceView
            {
                ShowLineNumbers = true,
                ShowLineMarks = true,
                TabWidth = 4,
                Editable = false,
                Visible = true,
                Monospace = true
            };

            scroller.Add(sourceView);

            return (sourceView, scroller);
        }
    }
}
