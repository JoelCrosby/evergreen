using System;

using Gtk;

namespace Evergreen.Dialogs
{
    public class AboutDialog : Dialog, IDisposable
    {
        public AboutDialog() : this(new Builder("about.ui")) { }

        private AboutDialog(Builder builder) : base(builder.GetObject("about").Handle)
        {
            builder.Autoconnect(this);

            Response += OnResponseReceived;
        }

        public new void Dispose()
        {
            Response -= OnResponseReceived;

            base.Dispose();
        }

        public void OnResponseReceived(object _, EventArgs args) => Hide();
    }
}
