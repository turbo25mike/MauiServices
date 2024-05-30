using System;
using Android.Bluetooth.LE;
using Android.Runtime;

namespace Turbo.Maui.Services.Platforms
{
	public class AdvertCallback: AdvertiseCallback
	{
        public override void OnStartFailure([GeneratedEnum] AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);
        }

        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);
        }
    }
}

