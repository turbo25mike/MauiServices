using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HttpClientHandler = Turbo.Maui.Services.Models.HttpClientHandler;

namespace Turbo.Maui.Services;

public interface IWebService
{
    Task<HttpResponseData<T>> Get<T>(string route);
    Task<HttpResponseData<T>> Get<T>(Uri route);
    Task<byte[]> GetFile(string route);
    Task<HttpResponseData<long>> GetFileSize(string url);
    Task<HttpResponseData<DateTime>> GetDateLastModified(string route);
    Task<HttpResponse> Post(string route, object obj);
    Task<HttpResponse> Delete(string route);
    Task<HttpResponse> Put(string route, PatchDocument patch);
    void UpdateTimeout(TimeSpan? timeout = null);
    void AddAuthorization(string token);
    void RemoveAuthorization();
}

public class WebService : IWebService
{
    public WebService(IKeyService key, IHttpHandler client = null) { _KeyService = key; _Client = client ?? new HttpClientHandler(); }

    #region Public Methods

    public void AddAuthorization(string token) => _Client.AddAuthorization(token);

    public async Task<byte[]> GetFile(string route) => await _Client.GetByteArrayAsync(GetUri(route));

    public async Task<HttpResponseData<long>> GetFileSize(string route)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetUri(route));
        // in order to keep the response as small as possible, set the requested byte range to [0,0] (i.e., only the first byte)
        request.Headers.Range = new RangeHeaderValue(from: 0, to: 0);

        using var response = await _Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode != HttpStatusCode.PartialContent)
            throw new WebException($"expected partial content response ({HttpStatusCode.PartialContent}), instead received: {response.StatusCode}");

        var contentRange = response.Content.Headers.GetValues(@"content-range").Single();
        var lengthString = Regex.Match(contentRange, @"(?<=^bytes\s[0-9]+\-[0-9]+/)[0-9]+$").Value;
        return new HttpResponseData<long>()
        {
            Data = long.Parse(lengthString),
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            WasSuccessful = response.IsSuccessStatusCode
        };
    }

    public async Task<HttpResponseData<DateTime>> GetDateLastModified(string route)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetUri(route));
        // in order to keep the response as small as possible, set the requested byte range to [0,0] (i.e., only the first byte)
        request.Headers.Range = new RangeHeaderValue(from: 0, to: 0);

        using var response = await _Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode != HttpStatusCode.PartialContent)
            throw new WebException($"expected partial content response ({HttpStatusCode.PartialContent}), instead received: {response.StatusCode}");

        return new HttpResponseData<DateTime>()
        {
            Data = response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.MinValue,
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            WasSuccessful = response.IsSuccessStatusCode
        };
    }

    public async Task<HttpResponseData<T>> Get<T>(Uri route) => await Call<T>(async () => await _Client.GetAsync(route));

    public Task<HttpResponseData<T>> Get<T>(string route) => Call<T>(async () => await _Client.GetAsync(GetUri(route)));

    public async Task<HttpResponse> Put(string route, PatchDocument patch) => await Call(async () => await _Client.PutAsync(GetUri(route), GetJsonPatchContent(patch)));

    public async Task<HttpResponse> Post(string route, object obj) => await Call(async () => await _Client.PostAsync(GetUri(route), GetJsonContent(obj)));

    public async Task<HttpResponse> Delete(string route) => await Call(async () => await _Client.DeleteAsync(GetUri(route)));

    public void UpdateTimeout(TimeSpan? timeout = null) => _Client.UpdateTimeout(timeout);

    public void RemoveAuthorization() => _Client.RemoveAuthorization();

    #endregion

    #region Private Methods

    private static async Task<HttpResponse> Call(Func<Task<HttpResponseMessage>> function)
    {
        try
        {
            var r = await function.Invoke();
            Log($"HttpResponse {r.StatusCode.GetHashCode()} {r.ReasonPhrase}, {r.RequestMessage.RequestUri}");
            if (!r.IsSuccessStatusCode) throw new ArgumentOutOfRangeException();
            return new HttpResponse(r);
        }
        catch (Exception ex)
        {
            Log($"{function.Method.Name} - exception: {ex}");
            return new HttpResponse() { StatusCode = HttpStatusCode.InternalServerError, WasSuccessful = false, ReasonPhrase = ex.ToString() };
        }
    }

    private static async Task<HttpResponseData<T>> Call<T>(Func<Task<HttpResponseMessage>> function)
    {
        try
        {
            var response = await function.Invoke();
            return await HttpResponseData<T>.Create(response);
        }
        catch (Exception ex)
        {
            Log($"{function.Method.Name} - exception: {ex}");
            return new HttpResponseData<T>()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ReasonPhrase = ex.ToString(),
                WasSuccessful = false
            };
        }
    }

    private StringContent GetJsonPatchContent(PatchDocument patch)
    {
        var json = patch.Serialize();
        return new(json, Encoding.UTF8, "application/json-patch+json");
    }

    private StringContent GetJsonContent(object obj) => new(GetJson(obj), Encoding.UTF8, "application/json");

    private static string GetJson(object obj) => obj.GetType() == typeof(string) ? obj.ToString() : JsonSerializer.Serialize(obj);

    private bool IsSimple(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // nullable type, check if the nested type is simple.
            return IsSimple(type.GetGenericArguments()[0]);
        }
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal);
    }

    private Uri GetUri(string route)
    {
        if (route.StartsWith("http")) return new Uri(route);
        var apiUrl = _KeyService.GetKey<string>("API_KEY") ?? "";
        var uri = new Uri($"{apiUrl}{route}");
        return uri;
    }

    private static void Log(string error) => Debug.WriteLine($"{nameof(WebService)}: {error}");

    #endregion

    #region Properties

    private readonly IKeyService _KeyService;
    private readonly IHttpHandler _Client;

    #endregion
}