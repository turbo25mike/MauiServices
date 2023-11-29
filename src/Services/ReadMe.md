//This library is for MAUI only.  BluetoothService only works with iOS and Android.

//Services should be instantiated like so in MauiProgram:

using Turbo.Maui.Services;

public static MauiApp CreateMauiApp()
 {
     var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseTurboMauiServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Font.ttf", "Font");
            });

//DEVELOPER NOTES

<PackageReference Include="sqlite-net-pcl" Version="[1.7.335]" />

//Hard coded due to latest version fails in Release mode

//See issue: https://github.com/praeclarum/sqlite-net/issues/1067
