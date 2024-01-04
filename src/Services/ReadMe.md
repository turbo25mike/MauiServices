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


//if you are using Auth0 you must provide the following options to your application
//Domain, ClientID, RedirectUri

//In Auth0 App you must set the "Allowed Callback URLs" and "Allowed Logout URLs" to match the "RedirectUri"

//In your Platforms/iOS/info.plist you must add the following:

<key>CFBundleURLTypes</key>
  <array>
    <dict>
      <key>CFBundleURLName</key>
      <string>MauiAuth0App</string>
      <key>CFBundleURLSchemes</key>
      <array>
        <string>myapp</string>
      </array>
      <key>CFBundleTypeRole</key>
        <string>Editor</string>
    </dict>
  </array>

//rename "myapp" to your redirectUri domain
//for example: RedirectUri = "sample://callback"
//then info.plist should have:
<key>CFBundleURLSchemes</key>
<array>
    <string>sample</string>
</array>

//Android Requirements

//Add a new class into Platforms/Android with the following code:

using Android.App;
using Android.Content.PM;

namespace MyApp;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
              Categories = new[] {
                Android.Content.Intent.CategoryDefault,
                Android.Content.Intent.CategoryBrowsable
              },
              DataScheme = CALLBACK_SCHEME)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
    const string CALLBACK_SCHEME = "myapp";
}

//DEVELOPER NOTES

<PackageReference Include="sqlite-net-pcl" Version="[1.7.335]" />

//Hard coded due to latest version fails in Release mode

//See issue: https://github.com/praeclarum/sqlite-net/issues/1067
