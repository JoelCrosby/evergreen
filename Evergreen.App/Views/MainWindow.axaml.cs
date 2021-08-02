using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Evergreen.App.ViewModels;

using JetBrains.Annotations;

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

        private async void OpenRepoClicked([CanBeNull] object? _, RoutedEventArgs __)
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
