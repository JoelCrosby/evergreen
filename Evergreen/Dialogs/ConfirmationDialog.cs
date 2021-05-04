using Gtk;

namespace Evergreen.Dialogs
{
    public static class ConfirmationDialog
    {
        public static bool Open(Window parent, string title, string message, string confirmLabel, MessageType type = MessageType.Info)
        {
            var dialog = new MessageDialog(
                parent,
                DialogFlags.DestroyWithParent,
                type,
                ButtonsType.OkCancel,
                title
            )
            {
                SecondaryText = message,
            };

            var res = (ResponseType)dialog.Run();

            dialog.Dispose();

            return res == ResponseType.Ok;
        }
    }
}
