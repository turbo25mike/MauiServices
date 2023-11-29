namespace Turbo.Maui.Services;

public class LocationAccuracyFilter : ILocationFilter
{
    public Location Process(Location data)
    {
        if (_BestResult == null)
            _BestResult = data;

        if (data.Accuracy != null)
        {
            if (_BestResult == null)
                _BestResult = data;
            else if (data.Accuracy.Value <= _BestResult.Accuracy.Value)
                _BestResult = data;
        }

        return _BestResult;
    }

    private Location _BestResult;
}