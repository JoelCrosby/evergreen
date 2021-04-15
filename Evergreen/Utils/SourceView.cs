using Gtk;

using GtkSource;

namespace Evergreen.Utils
{
    public static class SourceViews
    {
        public static (SourceView, ScrolledWindow) Create()
        {
            var scroller = new ScrolledWindow();
            scroller.Visible = true;

            var sourceView = new SourceView();

            sourceView.ShowLineNumbers = true;
            sourceView.ShowLineMarks = true;
            sourceView.TabWidth = 4;
            sourceView.Editable = false;
            sourceView.Visible = true;
            sourceView.Monospace = true;
            sourceView.SetSizeRequest(400, 4000);


            scroller.Add(sourceView);

            return (sourceView, scroller);
        }
    }
}
