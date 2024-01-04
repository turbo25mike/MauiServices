using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace Turbo.Maui.Services;

public interface IAuth0Service
{
    IdentityModel.OidcClient.Browser.IBrowser Browser { get; }
    Task<LoginResult> Login();
}

public class Auth0Service : IAuth0Service
{
    public Auth0Service(Options options)
    {
        oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = $"https://{options.Domain}",
            ClientId = options.ClientID,
            Scope = options.Scope,
            RedirectUri = options.RedirectUri,
            Browser = new WebBrowserAuthenticator()
        });
        _APIaudience = options.API_Audience;
    }

    public IdentityModel.OidcClient.Browser.IBrowser Browser
    {
        get => oidcClient.Options.Browser;
        private set => oidcClient.Options.Browser = value;
    }

    public async Task<LoginResult> Login()
    {

        LoginRequest loginRequest = null;

        if (!string.IsNullOrEmpty(_APIaudience))
        {
            loginRequest = new LoginRequest
            {
                FrontChannelExtraParameters = new Parameters(new Dictionary<string, string>()
                {
                  { "audience", _APIaudience }
                })
            };
        }
        return await oidcClient.LoginAsync(loginRequest);
    }

    public async Task<BrowserResult> LogoutAsync()
    {
        var logoutRequest = new LogoutRequest();
        var endSessionUrl = new RequestUrl($"{oidcClient.Options.Authority}/v2/logout")
          .Create(new Parameters(new Dictionary<string, string>
        {
          {"client_id", oidcClient.Options.ClientId },
          {"returnTo", oidcClient.Options.RedirectUri }
        }));
        var browserOptions = new BrowserOptions(endSessionUrl, oidcClient.Options.RedirectUri)
        {
            Timeout = TimeSpan.FromSeconds(logoutRequest.BrowserTimeout),
            DisplayMode = logoutRequest.BrowserDisplayMode
        };

        return await oidcClient.Options.Browser.InvokeAsync(browserOptions);
    }

    private readonly OidcClient oidcClient;
    private string _APIaudience;

    public class Options
    {
        public string Domain { get; set; } = "";
        public string ClientID { get; set; } = "";
        public string RedirectUri { get; set; } = "";
        public string Scope { get; set; } = "openid profile";
        public string API_Audience { get; set; } = "";
    }
}