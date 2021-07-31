using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Evergreen.App.ViewModels;
using Evergreen.Lib.Git;


namespace Evergreen.App.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindowViewModel? Model => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async void OpenRepoClicked(object? sender, RoutedEventArgs routedEventArgs)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Open Repository",
            };

            var response = await dialog.ShowAsync(this);

            if (response is null)
            {
                return;
            }

            if (!GitService.IsRepository(response))
            {
                return;
            }

            Model?.OpenCommand.Execute(response);
        }

        private void CloseRepoClicked(object sender, EventArgs _)
        {

        }
    }
}
