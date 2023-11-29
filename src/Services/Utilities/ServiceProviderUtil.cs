namespace Turbo.Maui.Services.Utilities;

public static class ServiceProviderUtil
{
    public static TService GetService<TService>() => Current.GetService<TService>();

    public static IServiceProvider Current => IPlatformApplication.Current.Services;
}