using Android.App;
using Android.Bluetooth;
using Android.Content;

namespace Turbo.Maui.Services.Platforms;

public static class BroadcastReceiverUtil
{
    public static void Register(this BroadcastReceiver rec, Activity activity, IntentFilter filter)
    {
        activity.RegisterReceiver(rec, filter);
    }

    public static void Unregister(this BroadcastReceiver rec, Activity activity)
    {
        activity.UnregisterReceiver(rec);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class BLEBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        // Do stuff here.
        var action = intent.Action;
        if (action == BluetoothAdapter.ActionStateChanged)
        {
            //var state = intent.GetIntExtra(BluetoothAdapter.ExtraState, BluetoothAdapter.Error);

            //Let's tell the bluetooth service what's up

            var bleService = ServiceProviderUtil.GetService<IBluetoothService>();
            if (bleService != null)
                bleService.StateUpdated();
        }
    }
}