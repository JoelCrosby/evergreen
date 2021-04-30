using System;
using Gtk;

namespace Evergreen.Utils
{
    public static class Menus
    {
        public static void Open(params (string label, EventHandler handler)[] items)
        {
            var menu = new Menu();

            foreach (var (label, handler) in items)
            {
                var menuItem = new MenuItem(label);

                if (handler is not null)
                {
                    menuItem.Activated += handler;
                }

                menu.Add(menuItem);
            }

            menu.ShowAll();
            menu.Popup();
        }
    }
}
