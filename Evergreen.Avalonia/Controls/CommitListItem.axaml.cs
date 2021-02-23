using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Evergreen.Avalonia.ViewModels;

namespace Evergreen.Avalonia.Controls
{
    public class CommitListItem : UserControl
    {
        public CommitListItemViewModel Item { get; set; }

        public CommitListItem() => InitializeComponent();

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

