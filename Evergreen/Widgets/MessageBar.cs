using System;
using System.Threading.Tasks;

using Gtk;

namespace Evergreen.Widgets
{
    public class MessageBar : IDisposable
    {
        private InfoBar _view { get; }
        private Label _messageLabel { get; }

        public MessageBar(InfoBar view, Label label)
        {
            _view = view;
            _messageLabel = label;
        }

        public MessageBar Build()
        {
            _view.Respond += OnRespond;

            return this;
        }

        public Task Open(string msg, MessageType type = MessageType.Info)
        {
            _messageLabel.Text = msg;
            _view.MessageType = type;

            return Task.Run(Reveal);
        }

        public Task Error(string msg)
        {
            _messageLabel.Text = msg;
            _view.MessageType = MessageType.Error;

            return Task.Run(Reveal);
        }

        private void OnRespond(object _, RespondArgs args)
        {
            Hide();
        }

        private async Task Reveal()
        {
            Show();

            await Task.Delay(3000);

            Hide();
        }

        private void Show()
        {
            _view.SetProperty("revealed", new GLib.Value(true));
        }

        private void Hide()
        {
            _view.SetProperty("revealed", new GLib.Value(false));
        }

        public void Dispose()
        {
            _view.Respond -= OnRespond;
        }
    }
}
