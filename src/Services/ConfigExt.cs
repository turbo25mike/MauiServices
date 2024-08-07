﻿using Microsoft.Maui.LifecycleEvents;
using Turbo.Maui.Services.Platforms;
namespace Turbo.Maui.Services;

public static class ConfigExt
{
#if ANDROID
    private static BLEBroadcastReceiver rec = new BLEBroadcastReceiver();
#endif
    public static MauiAppBuilder UseTurboMauiServices(this MauiAppBuilder builder, Auth0Service.Options? options = null)
    {
        builder.Services.AddSingleton<IKeyService, KeyService>();
        builder.Services.AddSingleton<IBluetoothAdapter, BLEAdapter>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        //builder.Services.AddSingleton<IDBService, DBService>();
        builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
        builder.Services.AddSingleton<IWebService, WebService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton<IAuth0Service>(new Auth0Service(options));

#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
         {
             events.AddAndroid(android => android
                 .OnResume((activity) => BroadcastReceiverUtil.Register(rec, activity, new Android.Content.IntentFilter(Android.Bluetooth.BluetoothAdapter.ActionStateChanged)))
                 .OnStop((activity) => BroadcastReceiverUtil.Unregister(rec, activity))
             );
         });
#endif

        return builder;
    }
}