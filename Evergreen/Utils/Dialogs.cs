using Gtk;

namespace Evergreen.Utils
{
    public static class Dialogs
    {
        public static (ResponseType, FileChooserNative) Open(Window parent,string title, FileChooserAction action)
        {
            var dialog = new FileChooserNative(
                title,
                parent,
                action,
                "Open",
                "Cancel"
            );

            var response = (ResponseType)dialog.Run();

            return (response, dialog);
        }
    }
}
