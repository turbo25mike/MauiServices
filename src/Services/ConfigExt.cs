using Microsoft.Maui.LifecycleEvents;

namespace Turbo.Maui.Services;

public static class ConfigExt
{
#if ANDROID
    private static Platforms.Android.BLEBroadcastReceiver rec;
#endif
    public static MauiAppBuilder UseTurboMauiServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IKeyService, KeyService>();
        builder.Services.AddSingleton<IBluetoothAdapter, BLEAdapter>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IDBService, DBService>();
        builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
        builder.Services.AddSingleton<IWebService, WebService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();

#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
         {
             events.AddAndroid(android => android
                 .OnCreate((activity, bundle) => rec = new Platforms.Android.BLEBroadcastReceiver())
                 .OnResume((activity) => Platforms.Android.BroadcastReceiverUtil.Register(rec, activity, new Android.Content.IntentFilter(Android.Bluetooth.BluetoothAdapter.ActionStateChanged)))
                 .OnStop((activity) => Platforms.Android.BroadcastReceiverUtil.Unregister(rec, activity))
             );
         });
#endif

        return builder;
    }
}