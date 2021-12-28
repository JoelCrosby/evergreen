using System.Linq;

using Gtk;

namespace Evergreen.Dialogs
{
    public static class ConfirmationDialog
    {
        public static bool Open(
            Window parent,
            string title,
            string message,
            string confirmLabel,
            MessageType type = MessageType.Info)
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

            var box = dialog.Children.FirstOrDefault() as Box;
            var innerBox = box?.Children.LastOrDefault() as Box;
            var btnBox = innerBox?.Children.LastOrDefault() as ButtonBox;

            if (btnBox?.Children.LastOrDefault() is Button { } okBtn)
            {
                okBtn.Label = confirmLabel;
            }

            var res = (ResponseType)dialog.Run();

            dialog.Dispose();

            return res == ResponseType.Ok;
        }
    }
}
