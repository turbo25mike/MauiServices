namespace BluetoothService.Example;

public partial class DevicePage : ContentPage
{
    public DevicePage(DeviceViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
