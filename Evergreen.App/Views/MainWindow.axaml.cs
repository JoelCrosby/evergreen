using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Evergreen.App.ViewModels;

// ReSharper disable UnusedParameter.Local

namespace Evergreen.App.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindowViewModel? Model
        {
            get => DataContext as MainWindowViewModel;
        }

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async void OpenRepoClicked(object? sender, RoutedEventArgs args)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Open Repository",
            };

            var response = await dialog.ShowAsync(this);

            Model?.OpenCommand.Execute(response);
        }
    }
}
