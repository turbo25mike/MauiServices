using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;

namespace Turbo.Maui.Services.Utilities;

public class WebBrowserAuthenticator : IdentityModel.OidcClient.Browser.IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            WebAuthenticatorResult result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(options.StartUrl),
                new Uri(options.EndUrl));

            var url = new RequestUrl(options.EndUrl)
                .Create(new Parameters(result.Properties));

            // Workaround for Facebook issue
            //Noted at: https://auth0.com/blog/add-authentication-to-dotnet-maui-apps-with-auth0/
            if (url.EndsWith(_FacebookWorkAround))
                url = url[..url.LastIndexOf(_FacebookWorkAround)];

            return new BrowserResult
            {
                Response = url,
                ResultType = BrowserResultType.Success
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel,
                ErrorDescription = "Login canceled by the user."
            };
        }
    }

    private const string _FacebookWorkAround = "%23_%3D_";
}