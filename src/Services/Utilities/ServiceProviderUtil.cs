namespace Turbo.Maui.Services.Utilities;

public static class ServiceProviderUtil
{
    public static TService? GetService<TService>()
    {
        if (Current is null) throw new ArgumentNullException("IServiceProvider");
        return Current.GetService<TService>();
    }

    public static IServiceProvider? Current => IPlatformApplication.Current?.Services ?? null;
}