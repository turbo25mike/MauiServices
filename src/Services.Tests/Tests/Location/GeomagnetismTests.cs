namespace Turbo.Maui.Services.Tests;

public class GeomagnetismTests
{
    //Commented out because it will fail if the date is too far off from last tested.
    [Fact]
    public void Data_NoDate_Tests()
    {
        //the expected results will need to be updated for the test to succeed based on todays date.
        //https://www.ngdc.noaa.gov/geomag/calculators/magcalc.shtml#igrfwmm

        //Leupold
        //Given(45.524, -122.822, 0).Expected(14.8501, 67.2298, 20057.9, 47785.3, 19387.9, 5140.7, 51824.2, within);
        //Jeff's House
        //Given(45.36623317957041, -123.00365492296764, 0).Expected(14.8444, 67.0715, 20146.7, 47627.9, 19474.3, 5161.5, 51713.7, within);

        //Florida
        Given(28.54055, -81.3847, 0).Expected(-6, 56, 24628, 37692, 24456, -2909, 45025, within);
    }

    [Fact]
    public void Data_2023_10_27_Tests()
    {
        var testDate = new DateTime(2023, 10, 27);
        //Leupold
        Given(45.524, -122.822, 0, testDate).Expected(14.8501, 67.2298, 20057.9, 47785.3, 19387.9, 5140.7, 51824.2, within);
        //Jeff's House
        Given(45.36623317957041, -123.00365492296764, 0, testDate).Expected(14.8444, 67.0715, 20146.7, 47627.9, 19474.3, 5161.5, 51713.7, within);
    }

    [Fact]
    public void Data_2023_07_02_Tests()
    {
        var testDate = new DateTime(2023, 7, 2);

        Given(54, -120, 28000, testDate).Expected(16.2, 74.02, 15158.8, 52945.6, 14556.6, 4230.3, 55072.9, within);
        Given(-58, 156, 68000, testDate).Expected(40.48, -81.60, 9223.1, -62459.5, 7015.1, 5987.8, 63136.8, within);
        Given(-65, -88, 39000, testDate).Expected(29.86, -60.29, 20783.0, -36415.6, 18024.0, 10347.2, 41928.8, within);
        Given(-23, 81, 27000, testDate).Expected(-13.98, -58.52, 25568.2, -41759.2, 24811.0, -6176.1, 48965.0, within);
        Given(34, 0, 11000, testDate).Expected(1.08, 46.69, 29038.8, 30807.0, 29033.7, 545.3, 42335.9, within);
        Given(-62, 65, 72000, testDate).Expected(-66.98, -68.38, 17485, -44111.8, 6836.8, -16092.9, 47450.8, within);
        Given(86, 70, 55000, testDate).Expected(61.19, 87.51, 2424.9, 55750.6, 1168.7, 2124.7, 55803.4, within);
        Given(32, 163, 59000, testDate).Expected(0.36, 43.05, 28170.6, 26318.3, 28170.0, 176.0, 38551.7, within);
        Given(48, 148, 65000, testDate).Expected(-9.39, 61.70, 23673.8, 43968.0, 23356.8, -3861.2, 49936.3, within);
        Given(30, 28, 95000, testDate).Expected(4.49, 44.12, 29754.7, 28857.6, 29663.3, 2331.2, 41450.1, within);
    }

    [Fact]
    public void Data_2024_01_01_Tests()
    {
        var testDate = new DateTime(2024, 1, 1);

        Given(-60, -59, 95000, testDate).Expected(8.86, -55.03, 18317.9, -26193.2, 18099.3, 2821.2, 31962.9, within);
        Given(-70, 42, 95000, testDate).Expected(-54.29, -64.59, 18188.3, -38293.4, 10615.3, -14769.3, 42393.4, within);
        Given(87, -154, 50000, testDate).Expected(-82.22, 89.39, 597.9, 55904.6, 80.9, -592.4, 55907.8, within);
        Given(32, 19, 58000, testDate).Expected(3.94, 45.89, 29401.0, 30329.8, 29331.6, 2019.5, 42241.2, within);
        Given(34, -13, 57000, testDate).Expected(-2.62, 45.83, 28188.3, 29015.5, 28158.7, -1290.8, 40453.4, within);
        Given(-76, 49, 38000, testDate).Expected(-63.51, -67.40, 18425.8, -44260.7, 8218.0, -16491.7, 47942.9, within);
        Given(-50, -179, 49000, testDate).Expected(31.57, -71.40, 18112.2, -53818.3, 15431.4, 9482.7, 56784.4, within);
        Given(-55, -171, 90000, testDate).Expected(38.07, -72.91, 16409.7, -53373.5, 12918.7, 10118.6, 55839.2, within);
        Given(42, -19, 41000, testDate).Expected(-5.00, 56.57, 24410.2, 36981.3, 24317.3, -2127.0, 44311.1, within);
        Given(46, -22, 19000, testDate).Expected(-6.60, 61.04, 22534.0, 40713.3, 22384.7, -2590.4, 46533.4, within);
    }

    [Fact]
    public void Data_2024_07_02_Tests()
    {
        var testDate = new DateTime(2024, 7, 2);

        Given(13, -132, 31000, testDate).Expected(9.21, 31.51, 28413.4, 17417.2, 28046.9, 4548.8, 33326.9, within);
        Given(-2, 158, 93000, testDate).Expected(7.16, -17.78, 34124.3, -10940.3, 33858.1, 4253.5, 35835.1, within);
        Given(-76, 40, 51000, testDate).Expected(-55.63, -66.27, 18529.2, -42141.5, 10459.6, -15294.7, 46035.2, within);
        Given(22, -132, 64000, testDate).Expected(10.52, 43.88, 26250.1, 25239.1, 25808.9, 4792.5, 36415.3, within);
        Given(-65, 55, 26000, testDate).Expected(-62.60, -65.67, 18702.1, -41366.0, 8607.6, -16603.5, 45397.3, within);
        Given(-21, 32, 66000, testDate).Expected(-13.34, -56.95, 15940.8, -24502.7, 15510.7, -3677.9, 29231.7, within);
        Given(9, -172, 18000, testDate).Expected(9.39, 15.78, 31031.6, 8768.5, 30615.4, 5065.5, 32246.7, within);
        Given(88, 26, 63000, testDate).Expected(29.81, 87.38, 2523.6, 55156.1, 2189.6, 1254.6, 55213.8, within);
        Given(17, 5, 33000, testDate).Expected(0.61, 13.58, 34062.9, 8230.6, 34060.9, 362.9, 35043.1, within);
        Given(-18, 138, 77000, testDate).Expected(4.63, -47.71, 31825.9, -34986.2, 31722.0, 2569.6, 47296.1, within);
    }

    [Fact]
    public void Direction_2023_07_02_Tests()
    {
        var testDate = new DateTime(2023, 7, 2);
        Given(45, -123, 28000, testDate).AndDirectionIs(10).Should().BeApproximately(24, ToleranceOf);
        Given(45, -123, 28000, testDate).AndDirectionIs(0).Should().BeApproximately(15, ToleranceOf);
        Given(45, -123, 28000, testDate).AndDirectionIs(359).Should().BeApproximately(14, ToleranceOf);
        Given(45, -123, 28000, testDate).AndDirectionIs(180).Should().BeApproximately(195, ToleranceOf);
    }

    #region Private

    private const double within = 5;
    private const double ToleranceOf = 1;

    private Geomagnetism Given(double lat, double lng, double alt, DateTime? dt = null) => new(lat, lng, alt, dt);

    #endregion
}