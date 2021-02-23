using System;

using Evergreen.Gtk.Windows;

using GLib;

using Application = Gtk.Application;

namespace Evergreen.Gtk
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.Init();

            var app = new Application("org.evergreen.evergreen", ApplicationFlags.None);
            app.Register(Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}
