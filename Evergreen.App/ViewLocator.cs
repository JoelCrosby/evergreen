using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Evergreen.App.ViewModels;

namespace Evergreen.App
{
    public class ViewLocator : IDataTemplate
    {
        public static bool SupportsRecycling => false;

        public IControl Build(object param)
        {
            var name = param.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object data) => data is ViewModelBase;
    }
}