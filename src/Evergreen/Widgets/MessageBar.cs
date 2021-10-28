using System;

using GLib;

using Gtk;

using Task = System.Threading.Tasks.Task;

namespace Evergreen.Widgets
{
    public class MessageBar : IDisposable
    {
        public MessageBar(InfoBar view, Label label)
        {
            View = view;
            MessageLabel = label;
            View.Respond += OnRespond;
        }

        private InfoBar View { get; }
        private Label MessageLabel { get; }

        public void Dispose() => View.Respond -= OnRespond;

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

        private void OnRespond(object _, RespondArgs args) => Hide();

        private async Task Reveal()
        {
            Show();

            await Task.Delay(3000);

            Hide();
        }

        private void Show() => View.SetProperty("revealed", new Value(true));

        private void Hide() => View.SetProperty("revealed", new Value(false));
    }
}
