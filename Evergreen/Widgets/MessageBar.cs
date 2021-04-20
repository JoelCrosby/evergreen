using System.Threading.Tasks;

using Gtk;

namespace Evergreen.Widgets
{
    public class MessageBar
    {
        private InfoBar View { get; }
        private Label MessageLabel { get; }

        public MessageBar(InfoBar view, Label label)
        {
            View = view;
            MessageLabel = label;
        }

        public MessageBar Build()
        {
            View.Respond += (e, _) => Hide();

            return this;
        }

        public Task Open(string msg, MessageType type = MessageType.Info)
        {
            MessageLabel.Text = msg;
            View.MessageType = type;

            return Task.Run(Reveal);
        }

        public Task Error(string msg)
        {
            MessageLabel.Text = msg;
            View.MessageType = MessageType.Error;

            return Task.Run(Reveal);
        }

        private async Task Reveal()
        {
            Show();

            await Task.Delay(3000);

            Hide();
        }

        private void Show()
        {
            View.SetProperty("revealed", new GLib.Value(true));
        }

        private void Hide()
        {
            View.SetProperty("revealed", new GLib.Value(false));
        }
    }
}
