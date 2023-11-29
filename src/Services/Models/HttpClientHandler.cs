namespace Turbo.Maui.Services.Models;

public interface IHttpHandler
{
    Task<HttpResponseMessage> GetAsync(string url);
    Task<HttpResponseMessage> GetAsync(Uri uri);
    Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content);
    Task<HttpResponseMessage> PostAsync(string url, HttpContent content);
    void AddAuthorization(string token);
    void RemoveAuthorization();
    void UpdateTimeout(TimeSpan? timeout = null);
    Task<HttpResponseMessage> PutAsync(string uri, HttpContent content);
    Task<HttpResponseMessage> PutAsync(Uri uri, HttpContent content);
    Task<HttpResponseMessage> DeleteAsync(Uri uri);
    Task<HttpResponseMessage> DeleteAsync(string url);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    Task<byte[]> GetByteArrayAsync(Uri uri);
}

public class HttpClientHandler : IHttpHandler
{
    public HttpClientHandler()
    {
        _Client = new() { Timeout = _DefaultTimeout };
    }

    public async Task<HttpResponseMessage> GetAsync(Uri uri) => await _Client.GetAsync(uri);

    public async Task<HttpResponseMessage> GetAsync(string url) => await _Client.GetAsync(url);

    public async Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content) => await _Client.PostAsync(uri, content);

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content) => await _Client.PostAsync(url, content);

    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content) => await _Client.PutAsync(url, content);

    public async Task<HttpResponseMessage> PutAsync(Uri uri, HttpContent content) => await _Client.PutAsync(uri, content);

    public async Task<HttpResponseMessage> DeleteAsync(string url) => await _Client.DeleteAsync(url);

    public async Task<HttpResponseMessage> DeleteAsync(Uri uri) => await _Client.DeleteAsync(uri);

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => await _Client.SendAsync(request);

    public async Task<byte[]> GetByteArrayAsync(Uri uri) => await _Client.GetByteArrayAsync(uri);

    public void UpdateTimeout(TimeSpan? timeout = null) => _Client.Timeout = timeout ?? _DefaultTimeout;

    public void AddAuthorization(string token) => _Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

    public void RemoveAuthorization() => _Client.DefaultRequestHeaders.Remove("Authorization");

    private readonly HttpClient _Client;
    private readonly TimeSpan _DefaultTimeout = TimeSpan.FromSeconds(5);
}