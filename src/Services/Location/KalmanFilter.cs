namespace Turbo.Maui.Services;

/// <summary>
/// https://freeletics.engineering/2019/06/03/ios_gps_testing.html
/// </summary>
public class KalmanFilter : ILocationFilter
{
    public KalmanFilter(double timeUncertainty = 0) => _TimeUncertainty = timeUncertainty;

    /// Run a Kalman Filter iteration to estimate a new location. First, the uncertainty of the current estimate is
    /// updated based on the elapsed time and the predicted speed of the tracked object. If an accurate measurement
    /// is available, the prediction is then adjusted taking into consideration the accuracy of the measurement.
    ///
    /// - Parameter measurement: A new location measurement
    /// - Returns: The new estimated location
    public Location Process(Location measurement)
    {
        var updatedEstimate = _LocationEstimate == null ?
            InitialEstimate(measurement) :
            Predict(measurement.Timestamp.DateTime);

        // If the measurement is accurate enough, that we will use it to derive a more accurate updated estimate
        if (measurement.Accuracy.Value >= 0)
        {
            var estimate = _LocationEstimate;
            updatedEstimate = Correct(measurement, estimate);
        }

        _LocationEstimate = updatedEstimate;
        return updatedEstimate;
    }

    private Location InitialEstimate(Location measurement)
    {
        _Variance = Math.Pow(measurement.Accuracy.Value, 2);
        _LocationEstimate = measurement;
        return measurement;
    }

    private Location Predict(DateTime time)
    {
        if (_LocationEstimate == null) return new Location();
        var estimate = _LocationEstimate;

        var timeDelta = time.Ticks - estimate.Timestamp.Ticks;

        if (timeDelta > 0)
        {
            // Time has moved on, so the uncertainty in the current position increases
            _Variance += timeDelta * Math.Pow(_TimeUncertainty, 2);
        }

        estimate.Accuracy = Uncertainty;

        return estimate;
    }

    private Location Correct(Location measurement, Location estimate)
    {
        // Kalman gain matrix gainMatrix = Covariance / (Covariance + MeasurementVariance)
        var gain = _Variance / (_Variance + Math.Pow(measurement.Accuracy.Value, 2) + _Epsilon);

        // New covariance matrix is (IdentityMatrix - GainMatrix) * Covariance
        _Variance = (1 - gain) * _Variance;

        // Apply the gain matrix
        var newLat = estimate.Latitude + gain * (measurement.Latitude - estimate.Latitude);
        var newLong = estimate.Longitude + gain * (measurement.Longitude - estimate.Longitude);
        //var newAlt = estimate.altitude + gain * (measurement.altitude - estimate.altitude);
        var newCoord = new Location() { Latitude = newLat, Longitude = newLong, Accuracy = Uncertainty };

        return newCoord;
    }

    /// Small term used to avoid dividing by zero
    private readonly double _Epsilon = 1e-5;

    /// A parameter for how much uncertainty we want to introduce between measurements. It has units in
    /// meters per sec, for how much we believe the user may have traveled in one second. A larger
    /// value means we think the user traveled further, therefore we will rely less on the previous estimate.
    readonly double _TimeUncertainty;

    /// Internal location estimate used to perform filter computations
    private Location _LocationEstimate;

    /// Covariance matrix (one-dimensional) used in computations
    private double _Variance = 0;

    /// Standard deviation (i.e. uncertainty) derived from covariance
    private double Uncertainty => Math.Sqrt(_Variance);
}