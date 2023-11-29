using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Turbo.Maui.Services;

namespace WebService.Example;

public partial class MainViewModel : ObservableObject
{
    private readonly IWebService _WebService;

    public MainViewModel(IWebService webService)
    {
        _WebService = webService;
    }

    [RelayCommand]
    private async Task GetData()
    {
        Heading = "Fetching Data";
        SubHeading = "...";
        var fact = await _WebService.Get<CatFact>("https://meowfacts.herokuapp.com/");
        if (fact != null)
        {
            //We're online!
            Heading = "Cat Fact";
            if (fact.Data.Any())
                SubHeading = fact.Data.First();
            else
                SubHeading = "Hmmm... Should've had a result.  Check the JSON endpoint to see what it returned.";
        }
        else
        {
            Heading = "Offline";
            SubHeading = "Check to see if we're not blocked by OS. And that your data model is correct.";
        }
    }

    [ObservableProperty]
    private string _Heading = "";

    [ObservableProperty]
    private string _SubHeading = "";
}

public class CatFact
{
    public CatFact() { Data = new(); }

    [JsonPropertyName("data")]
    public List<string> Data { get; set; }
}

