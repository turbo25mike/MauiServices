using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services.Models;

public partial class PopupArgs : ObservableObject
{
    [ObservableProperty]
    private bool _IsOpen = false;
    [ObservableProperty]
    private string _Title = "";
    [ObservableProperty]
    private string _Body = "";
    [ObservableProperty]
    private string _AcceptTitle = "";
    [ObservableProperty]
    private string _DismissTitle = "";

    public Action<bool>? Action { get; private set; }

    public void Show(string title = "", string body = "", string acceptTitle = "Accept", string dismissTitle = "Dismiss", Action<bool>? action = null)
    {
        Title = title;
        Body = body;
        AcceptTitle = acceptTitle;
        DismissTitle = dismissTitle;
        Action = action;
        IsOpen = true;
    }
}