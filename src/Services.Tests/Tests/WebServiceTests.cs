using System.Net;
using System.Text.Json;

namespace Turbo.Maui.Services.Tests;

public class WebServiceTests
{
    [Fact]
    public async void ShouldGetData()
    {
        (await SomeWebService.Get<SomeModel>(SomeModelRoute)).Should().BeEquivalentTo(SomeModel);
    }

    [Theory]
    [InlineData(3000)]
    [InlineData(null)]
    public void ShouldUpdateTimeout(int? timeout)
    {
        GiveTimespan(timeout);
        SomeWebService.UpdateTimeout(SomeTimeout);
        Then.SomeFunction(nameof(SomeWebService.UpdateTimeout)).WasCalledOnce();
    }

    [Theory]
    [InlineData("")]
    [InlineData("test")]
    public void ShouldAddAuthorization(string token)
    {
        SomeWebService.AddAuthorization(token);
        SomeToken.Should().Be(token);
        Then.SomeFunction(nameof(SomeWebService.AddAuthorization)).WasCalledOnce();
    }

    [Fact]
    public void ShouldRemoveAuthorization()
    {
        SomeWebService.RemoveAuthorization();
        Then.SomeFunction(nameof(SomeWebService.RemoveAuthorization)).WasCalledOnce();
    }

    #region Private

    public WebServiceTests()
    {
        SomeKeyService.Setup(c => c.GetKey<string>("API_KEY")).Returns(SomeRootAddress);
        SetupHttpClient();
        SomeWebService = new WebService(SomeKeyService.Object, SomeHttpClient.Object);
    }

    private void SetupHttpClient()
    {
        var a = $"{SomeRootAddress}{SomeModelRoute}";
        SomeHttpClient.Setup(c => c.UpdateTimeout(It.IsAny<TimeSpan?>())).Callback(() => Then.AddCall(nameof(SomeHttpClient.Object.UpdateTimeout)));
        SomeHttpClient.Setup(c => c.RemoveAuthorization()).Callback(() => Then.AddCall(nameof(SomeHttpClient.Object.RemoveAuthorization)));
        SomeHttpClient.Setup(c => c.AddAuthorization(It.IsAny<string>())).Callback(AddAuthorizationWasCalled);
        SomeHttpClient.Setup(c => c.GetAsync(a)).ReturnsAsync(GenerateResponse(SomeModel, a));
        SomeHttpClient.Setup(c => c.GetAsync(new Uri(a))).ReturnsAsync(GenerateResponse(SomeModel, a));
    }

    private void AddAuthorizationWasCalled(string token)
    {
        SomeToken = token;
        Then.AddCall(nameof(SomeHttpClient.Object.AddAuthorization));
    }

    private void GiveTimespan(int? seconds)
    {
        SomeTimeout = seconds is null ? null : TimeSpan.FromSeconds(seconds.Value);
    }

    private static HttpResponseMessage GenerateResponse<T>(T data, string address)
    {
        return new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(data)),
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, address)
        };
    }

    private readonly Mock<IKeyService> SomeKeyService = new();

    public Mock<IHttpHandler> SomeHttpClient = new();

    private readonly IWebService SomeWebService;

    private const string SomeRootAddress = "https://www.someAddress.com/";
    private const string SomeModelRoute = "SomeModel";
    private string SomeToken = "";

    private readonly string SomeStatusRoute = SomeRootAddress + "app/db/status";

    private readonly SomeModel SomeModel = new();
    private TimeSpan? SomeTimeout;
    private readonly Then Then = new();

    #endregion
}