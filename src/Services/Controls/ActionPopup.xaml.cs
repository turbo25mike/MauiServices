namespace Turbo.Maui.Services;

public partial class ActionPopup : ContentView
{
    public ActionPopup()
    {
        InitializeComponent();
    }

    public static BindableProperty ArgsProperty = BindableProperty.Create(nameof(Args), typeof(PopupArgs), typeof(ActionPopup), null);
    public PopupArgs Args { get => (PopupArgs)GetValue(ArgsProperty); set => SetValue(ArgsProperty, value); }

    void AcceptButton_Clicked(object? sender, EventArgs e)
    {
        Args.IsOpen = false;
        Args?.Action?.Invoke(true);
    }

    void DismissButton_Clicked(object? sender, EventArgs e)
    {
        Args.IsOpen = false;
        Args?.Action?.Invoke(false);
    }
}
