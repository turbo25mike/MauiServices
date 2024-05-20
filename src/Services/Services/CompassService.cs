namespace Turbo.Maui.Services;

public interface ICompassService
{
    void StartMonitoring();
    void StopMonitoring();
    event EventHandler<CompassEventArgs> HeadingUpdate;
    double HeadingMagneticNorth { get; }
}

public class CompassService : ICompassService
{
    public CompassService()
    {
        Compass.ReadingChanged += (s, e) =>
        {
            if (Math.Abs(HeadingMagneticNorth - e.Reading.HeadingMagneticNorth) > _AccuracyLimit)
            {
                HeadingMagneticNorth = e.Reading.HeadingMagneticNorth;
                HeadingUpdate?.Invoke(this, new CompassEventArgs(e.Reading));
            }
        };
    }

    public void StartMonitoring()
    {
        try
        {
            if (!Compass.IsMonitoring)
                Compass.Start(_Speed);
        }
        catch (Exception)
        {
        }
    }

    public void StopMonitoring()
    {
        try
        {
            if (Compass.IsMonitoring)
                Compass.Stop();
        }
        catch (Exception)
        {
        }
    }

    public event EventHandler<CompassEventArgs>? HeadingUpdate;
    public double HeadingMagneticNorth { get; private set; }

    const double _AccuracyLimit = 0.001;
    readonly SensorSpeed _Speed = SensorSpeed.UI;
}

public class CompassEventArgs : EventArgs
{
    public CompassData Data { get; private set; }
    public CompassEventArgs(CompassData d) => Data = d;
}