using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Turbo.Maui.Services;
using Turbo.Maui.Services.Examples.Shared.Models;

namespace WebService.Example;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuth0Service _Auth0Service;
    private readonly IWebService _WebService;

    public MainViewModel(IWebService webService, IKeyService keyService, IAuth0Service auth)
    {
        _Auth0Service = auth;
        _WebService = webService;
        keyService.SetEnvironment(KeyService.DefaultEnvironments.DEBUG);
        keyService.AddKey("API_KEY", ApiKey);
    }

    [RelayCommand]
    private async Task Login()
    {
        var result = await _Auth0Service.Login();
        if (!result.IsError)
        {
            _WebService.AddAuthorization(result.AccessToken);
        }
    }

    [RelayCommand]
    private async Task TryUriWithAuth()
    {
        _WebService.AddAuthorization("Test4");
        //var response = await _WebService.Get<IEnumerable<string>>(new Uri("https://tak.limelite.tech/ManageGeoObject/getGeoObject"));
        //var response = await _WebService.Post(new Uri("https://tak.limelite.tech/ManageGeoObject/getGeoObject"), new object());
    }

    [RelayCommand]
    private async Task GetData()
    {
        Heading = "Fetching Data";
        Members.Clear();
        var response = await _WebService.Get<IEnumerable<ShortUser>>("User");
        if (response != null && response.WasSuccessful)
        {
            //We're online!
            Heading = "Avengers";
            foreach (var member in response.Data)
                Members.Add(member);
        }
        else
        {
            Heading = "Offline";
            //Check port, wifi and that your data model is correct.
        }
    }

    [RelayCommand]
    private async Task MemberSelected(ShortUser selectedMember)
    {
        await Shell.Current.GoToAsync(nameof(MemberPage), new Dictionary<string, object>() { { "member", selectedMember } });
    }

    [RelayCommand]
    private async Task Delete(ShortUser selectedMember)
    {
        await _WebService.Delete($"User/{selectedMember.ID}");

        //refresh list
        await GetData();
    }

    [ObservableProperty]
    private string _Heading = "";

    [ObservableProperty]
    private ObservableCollection<ShortUser> _Members = new();


    //non-secure endpoints should not be used and will not work outside of debug
    //if you need to add an unsecure endpoint you will need to update your info.plist and manifest.xml
    private const string ApiKey =
#if __IOS__
	    "http://macbook.local:5246/"; //Local Device name
#else
        "http://10.32.16.116:5246/"; //LOCAL IP
#endif
}