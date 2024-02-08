using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using ITunesShortcuts.Services;
using ITunesShortcuts.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;

namespace ITunesShortcuts;

public partial class App : Application
{
    readonly IHost host;

    public static IServiceProvider Provider { get; private set; } = default!;
    public static InMemorySink Sink { get; private set; } = new();

    public App()
    {
        host = Host.CreateDefaultBuilder()
            .UseSerilog((context, configuration) =>
            {
                configuration.WriteTo.Debug();
                configuration.WriteTo.File(@"logs\-log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10);
                configuration.WriteTo.Sink(Sink);
            })
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddJsonFile("Configuration.json", true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<Config>(context.Configuration);
                Config config = context.Configuration.Get<Config>() ?? new();

                // Add ViewModels and MainView
                services.AddSingleton<MainView>();

                // Add services
                services.AddSingleton<AppStartupHandler>();
                services.AddSingleton<WindowHelper>();
                services.AddSingleton<JsonConverter>();
                services.AddSingleton<SystemTray>();
            })
            .Build();
        Provider = host.Services;

        InitializeComponent();
    }


    protected override void OnLaunched(LaunchActivatedEventArgs args) =>
        Provider.GetRequiredService<AppStartupHandler>();
}