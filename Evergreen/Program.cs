using System;
using System.IO;

using Evergreen.Core.Helpers;
using Evergreen.Windows;

using GLib;

using Gtk;

using Application = Gtk.Application;

namespace Evergreen
{
    public static class Program
    {
        public static Window Window { get; private set; }

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += ApplicationExceptionHandler;

            Application.Init();

            var app = new Application("org.evergreen.evergreen", ApplicationFlags.None);
            app.Register(Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            Window = win;

            win.Show();
            Application.Run();
        }

        private static void ApplicationExceptionHandler(object _, UnhandledExceptionEventArgs e)
        {
            var logDir = PathUtils.GetCacheFolder();

            try
            {
                var stacktrace = (e.ExceptionObject as Exception)?.StackTrace;

                if (stacktrace is null)
                {
                    Environment.Exit(1);
                }

                Directory.CreateDirectory(logDir);

                using var fs = File.OpenWrite(Path.Join(logDir, "error.log"));
                using var sw = new StreamWriter(fs);

                sw.Write(stacktrace);
            }
            catch
            {
                Environment.Exit(1);
            }
        }
    }
}
