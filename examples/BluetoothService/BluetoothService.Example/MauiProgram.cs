using Microsoft.Extensions.Logging;
using Turbo.Maui.Services;
using CommunityToolkit.Maui;

namespace BluetoothService.Example;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseTurboMauiServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddScoped<MainPage, MainViewModel>();
        builder.Services.AddScoped<DevicePage, DeviceViewModel>();

        Routing.RegisterRoute(nameof(DevicePage), typeof(DevicePage));

        return builder.Build();
    }
}

