using System;

using Gtk;

namespace Evergreen.Dialogs
{
    public class About : Dialog, IDisposable
    {
        public About() : this(new Builder("about.ui")) { }

        private About(Builder builder) : base(builder.GetObject("about").Handle)
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
