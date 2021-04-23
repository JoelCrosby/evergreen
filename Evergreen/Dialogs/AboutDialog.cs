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

            Response += OnResponseRecieved;
        }

        public void OnResponseRecieved(object _, EventArgs args)
        {
            Hide();
        }

        public new void Dispose()
        {
            Response -= OnResponseRecieved;

            base.Dispose();
        }
    }
}
