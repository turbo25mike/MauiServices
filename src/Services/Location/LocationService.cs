using Timer = System.Timers.Timer;

namespace Turbo.Maui.Services;

public interface ILocationService
{
    void Start(int interval = 10, LocationFilterType filter = LocationFilterType.AVERAGE);
    void Stop();
    Task<Location> Get();
    void SetInterval(int interval);
    void SetFilter(LocationFilterType filter);
    void SetPosition(Pin position);
    void SetPosition(Location position);
    event EventHandler<LocationEventArgs> LocationUpdate;
    double GetDistance(double range, double inclination);
    Location GetRangeCoords(double latitude, double longitude, double distance, double bearing);
    double ToRadians(double v);
    double ToDegrees(double v);
    Location Current { get; }
    LocationFilterType FilterType { get; }
}

public class LocationService : ILocationService
{
    public LocationService()
    {
        _Timer = new Timer
        {
            Interval = TimeSpan.FromSeconds(_DefaultTimerInterval).TotalMilliseconds
        };
        _Timer.Elapsed += async (sender, e) => await Get();

        FilterType = LocationFilterType.AVERAGE;
    }

    #region Public Methods

    public void Start(int interval = 10, LocationFilterType filterType = LocationFilterType.AVERAGE)
    {
        if (_InProgress || FilterType == LocationFilterType.STATIC) return;

        _Filter = (filterType == LocationFilterType.AVERAGE) ? new KalmanFilter() : new LocationAccuracyFilter();
        Current = new Location();

        SetInterval(interval);
        _Timer.Start();
    }

    public void SetFilter(LocationFilterType filter) => FilterType = filter;
    public void SetPosition(Pin position) => SetPosition(new Location(position?.Latitude ?? 0, position?.Longitude ?? 0));
    public void SetPosition(Location position) => Current = position ?? new Location(0, 0);

    public void SetInterval(int interval) => _Timer.Interval = TimeSpan.FromSeconds(interval).TotalMilliseconds;

    public void Stop()
    {
        if (!_InProgress) return;

        _InProgress = false;
        _Timer.Stop();
    }

    /// <summary>
    /// Given a range and inclination this method will return the actual distance to travel.
    /// It does not account for the arc of the earth.
    /// range is in meters
    /// inclination is in degrees
    /// </summary>
    /// <param name="range"></param>
    /// <param name="inclination"></param>
    /// <returns></returns>
    public double GetDistance(double range, double inclination) => range * Math.Cos(ToRadians(inclination));

    public Location GetRangeCoords(double latitude, double longitude, double distance, double bearing)
    {
        /*
         * https://www.movable-type.co.uk/scripts/latlong.html
         * Formula:
         *  φ2 = asin( sin φ1 ⋅ cos δ + cos φ1 ⋅ sin δ ⋅ cos θ )
         *  λ2 = λ1 + atan2( sin θ ⋅ sin δ ⋅ cos φ1, cos δ − sin φ1 ⋅ sin φ2 )
         *  where	
         *  φ is latitude, 
         *  λ is longitude, 
         *  θ is the bearing (clockwise from north),
         *  δ is the angular distance d/R; 
         *  d being the distance travelled, 
         *  R the earth’s radius
         */

        var lat1 = ToRadians(latitude);
        var long1 = ToRadians(longitude);
        var b = ToRadians(bearing);
        var ad = distance / 6371e3; // distance / (Mean) radius of earth (defaults to radius in meters). angular distance in radians  

        var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(ad) + Math.Cos(lat1) * Math.Sin(ad) * Math.Cos(b));
        var long2 = long1 + Math.Atan2(Math.Sin(b) * Math.Sin(ad) * Math.Cos(lat1), Math.Cos(ad) - Math.Sin(lat1) * Math.Sin(lat2));

        return new Location(ToDegrees(lat2), ToDegrees(long2)); ;
    }

    public double ToRadians(double v) => v * Math.PI / 180;
    public double ToDegrees(double v) => v * 180 / Math.PI;

    public async Task<Location> Get()
    {
        try
        {

            if (FilterType == LocationFilterType.STATIC) return Current;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(2));

#if IOS
            request.RequestFullAccuracy = true;
#endif

                var position = await Geolocation.GetLocationAsync(request);

                if (position != null)// && (Current == null || Math.Abs(position.Latitude - Current.Latitude) > MIN_DISTANCE || Math.Abs(position.Longitude - Current.Longitude) > MIN_DISTANCE))
                {
                    Current = _Filter.Process(position);
                    LocationUpdate?.Invoke(this, new LocationEventArgs(Current));
                }
            });
        }
        catch (Exception)
        {
            LocationUpdate?.Invoke(this, new LocationEventArgs(null));
        }

        return Current;
    }

    #endregion

    #region Properties

    public event EventHandler<LocationEventArgs> LocationUpdate;
    public LocationFilterType FilterType { get; private set; }
    public Location Current { get; private set; }

    private readonly int _DefaultTimerInterval = 10;
    private readonly Timer _Timer;
    private bool _InProgress;
    private ILocationFilter _Filter = new KalmanFilter();

    #endregion
}

public class LocationEventArgs : EventArgs
{
    public Location Position { get; private set; }
    public LocationEventArgs(Location d) => Position = d;
}