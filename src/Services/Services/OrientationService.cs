using System.Numerics;

namespace Turbo.Maui.Services;

public interface IOrientationService : ITurboService
{
    void StartMonitoring();
    void StopMonitoring();
    event EventHandler<OrientationEventArgs> OrientationUpdate;
    Quaternion Orientation { get; }
}

public class OrientationService : IOrientationService
{
    public OrientationService()
    {

        OrientationSensor.ReadingChanged += (s, e) =>
        {
            if (Math.Abs(Orientation.Y - e.Reading.Orientation.Y) > _AccuracyLimit)
            {
                Orientation = e.Reading.Orientation;
                OrientationUpdate?.Invoke(this, new OrientationEventArgs(e.Reading.Orientation));
            }
        };
    }

    public void StartMonitoring()
    {
        try
        {
            if (!OrientationSensor.IsMonitoring)
                OrientationSensor.Start(_Speed);
        }
        catch (Exception)
        {
        }
    }

    public void StopMonitoring()
    {
        try
        {
            if (OrientationSensor.IsMonitoring)
                OrientationSensor.Stop();
        }
        catch (Exception)
        {
        }
    }

    public event EventHandler<OrientationEventArgs> OrientationUpdate;

    public Quaternion Orientation { get; private set; }

    const double _AccuracyLimit = 0.001;

    readonly SensorSpeed _Speed = SensorSpeed.UI;
}

public class OrientationEventArgs : EventArgs
{
    public Quaternion Orientation { get; private set; }
    public OrientationEventArgs(Quaternion o) { Orientation = o; }
}
