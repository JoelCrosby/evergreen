using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
        [UI] private Button openRepo = null;
        [UI] private HeaderBar headerBar = null;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            Titlebar = headerBar;

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepo_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void OpenRepo_Clicked(object sender, EventArgs a)
        {
            Console.WriteLine("Open repo Clicked!");
        }
    }
}
