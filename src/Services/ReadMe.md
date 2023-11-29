//Turbo.Maui.Services

//This library is for MAUI only.

//Services should be instantiated like so in MauiProgram:


public static MauiApp CreateMauiApp()
 {
     var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Font.ttf", "Font");
            });

    //Services
    builder.Services.AddSingleton<IAppInfoHandler, AppInfoService>();
    builder.Services.AddSingleton<IHttpHandler, HttpClientHandler>();
    builder.Services.AddSingleton<IDeviceHandler, DeviceHandlerService>();
    builder.Services.AddSingleton<IKeyService, AppKeyService>();
    builder.Services.AddSingleton<IBluetoothAdapter, BLEAdapter>();
    builder.Services.AddSingleton<INavigationService, NavigationService>();
    builder.Services.AddSingleton<ILocationService, LocationService>();
    builder.Services.AddSingleton<IDBService, DBService>();
    builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
    builder.Services.AddSingleton<IWebService, WebService>();
    builder.Services.AddSingleton<IFileService, FileService>();
    builder.Services.AddSingleton<IAlertService, AlertService>();

//DEVELOPER NOTES

<PackageReference Include="sqlite-net-pcl" Version="[1.7.335]" />

//Hard coded due to latest version fails in Release mode

//See issue: https://github.com/praeclarum/sqlite-net/issues/1067
