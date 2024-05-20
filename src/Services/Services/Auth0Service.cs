using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace Turbo.Maui.Services;

public interface IAuth0Service
{
    IdentityModel.OidcClient.Browser.IBrowser Browser { get; }
    Task<LoginResult> Login();
    void Update(Auth0Service.Options? options);
}

public class Auth0Service : IAuth0Service
{
    public Auth0Service(Options? options)
    {
        Update(options);
    }

    public void Update(Options? options)
    {
        if (options is null) return;
        _OidcClient = new OidcClient(new OidcClientOptions
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
        get
        {
            if (_OidcClient is null) throw new ArgumentNullException("OIDC Client can not be null");
            return _OidcClient.Options.Browser;
        }
        private set
        {
            if (_OidcClient is null) throw new ArgumentNullException("OIDC Client can not be null");
            _OidcClient.Options.Browser = value;
        }
    }

    public async Task<LoginResult> Login()
    {
        if (_OidcClient is null) throw new ArgumentNullException("OIDC Client can not be null");
        if (_APIaudience is null) throw new ArgumentNullException("API audience can not be null");
        
        var loginRequest = new LoginRequest
            {
                FrontChannelExtraParameters = new Parameters(new Dictionary<string, string>()
                {
                  { "audience", _APIaudience }
                })
            };
        return await _OidcClient.LoginAsync(loginRequest);
    }

    public async Task<BrowserResult> LogoutAsync()
    {
        if (_OidcClient is null) throw new ArgumentNullException("OIDC Client can not be null");
        var logoutRequest = new LogoutRequest();
        var endSessionUrl = new RequestUrl($"{_OidcClient.Options.Authority}/v2/logout")
          .Create(new Parameters(new Dictionary<string, string>
        {
          {"client_id", _OidcClient.Options.ClientId },
          {"returnTo", _OidcClient.Options.RedirectUri }
        }));
        var browserOptions = new BrowserOptions(endSessionUrl, _OidcClient.Options.RedirectUri)
        {
            Timeout = TimeSpan.FromSeconds(logoutRequest.BrowserTimeout),
            DisplayMode = logoutRequest.BrowserDisplayMode
        };

        return await _OidcClient.Options.Browser.InvokeAsync(browserOptions);
    }

    private OidcClient? _OidcClient;
    private string? _APIaudience;

    public class Options
    {
        public string Domain { get; set; } = "";
        public string ClientID { get; set; } = "";
        public string RedirectUri { get; set; } = "";
        public string Scope { get; set; } = "openid profile";
        public string API_Audience { get; set; } = "";
    }
}