using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Evergreen.App.ViewModels;
using Evergreen.App.Views;
using Evergreen.Core.Git;
using Evergreen.Core.Services;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReactiveUI;

using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Evergreen.App
{
    public class App : Application
    {
        public IServiceProvider Container { get; private set; } = null!;

        public override void Initialize()
        {
            InitDependencyInjection();

            AvaloniaXamlLoader.Load(this);
        }

        private void InitDependencyInjection()
        {
            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.UseMicrosoftDependencyResolver();
                    var resolver = Locator.CurrentMutable;
                    resolver.InitializeSplat();
                    resolver.InitializeReactiveUI();

                    // Configure our local services and access the host configuration
                    ConfigureServices(services);
                })
                .UseEnvironment(Environments.Development)
                .Build();

            // Since MS DI container is a different type,
            // we need to re-register the built container with Splat again
            Container = host.Services;
            Container.UseMicrosoftDependencyResolver();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddMediatR(typeof(GitService).Assembly);

            services.AddSingleton<RepositoriesService>();
            services.AddTransient<MainWindowViewModel>();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Locator.Current.GetService<MainWindowViewModel>(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
