using Microsoft.Extensions.Logging;
using Turbo.Maui.Services;
using CommunityToolkit.Maui;

namespace WebService.Example;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseTurboMauiServices()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddScoped<MainPage, MainViewModel>();
        builder.Services.AddScoped<MemberPage, MemberViewModel>();

        Routing.RegisterRoute(nameof(MemberPage), typeof(MemberPage));

        return builder.Build();
    }
}

