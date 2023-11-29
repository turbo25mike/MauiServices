using System.Text.RegularExpressions;

namespace Turbo.Maui.Services;

/** Class to calculate magnetic declination, magnetic field strength,
inclination etc. for any point on the earth.

Adapted from the geomagc software and World Magnetic Model of the NOAA
Satellite and Information Service, National Geophysical Data Center

http://www.ngdc.noaa.gov/geomag/WMM/DoDWMM.shtml
**/

public class Geomagnetism
{
    /** Initialise the instance and calculate for given location and date
	- parameters:
		- longitude: Longitude in decimal degrees
		- latitude: Latitude in decimal degrees
		- altitude: Altitude in metres (with respect to WGS-1984 ellipsoid)
		- date: Date of the calculation*/
    public Geomagnetism(double latitude, double longitude, double altitude = 0, DateTime? date = null)
    {
        if (date == null) date = DateTime.Now;
        Init();
        Calculate(latitude, longitude, altitude, date.Value);
    }

    public double GetTrueDirection(double magneticDirection)
    {
        if (magneticDirection >= 360 || magneticDirection < 0) throw new ArgumentOutOfRangeException(nameof(magneticDirection));

        var trueNorth = magneticDirection + Declination;

        if (trueNorth >= 359) trueNorth -= 360;
        if (trueNorth < 0) trueNorth += 360;

        return trueNorth;
    }

    private void Init()
    {
        sp[0] = 0;
        cp[0] = 1;
        snorm[0] = 1;
        pp[0] = 1;
        dp[0, 0] = 0;
        c[0, 0] = 0;
        cd[0, 0] = 0;

        epoch = double.Parse(WMM_COF[0].Trim(' ').Split(" ")[0]);

        for (var i = 1; i < WMM_COF.Length - 1; i++)
        {
            var row = Regex.Replace(WMM_COF[i].Trim(' '), @"\s+", " ");
            var tokens = row.Split(" ");

            var n = int.Parse(tokens[0]);
            var m = int.Parse(tokens[1]);
            var gnm = double.Parse(tokens[2]);
            var hnm = double.Parse(tokens[3]);
            var dgnm = double.Parse(tokens[4]);
            var dhnm = double.Parse(tokens[5]);

            if (m <= n)
            {
                c[m, n] = gnm;
                cd[m, n] = dgnm;

                if (m != 0)
                {
                    c[n, m - 1] = hnm;
                    cd[n, m - 1] = dhnm;
                }
            }
        }
        // Convert schmidt normalized gauss coefficients to unnormalized
        snorm[0] = 1;

        for (var n = 1; n <= MAX_DEG; n++)
        {
            snorm[n] = snorm[n - 1] * (2 * n - 1) / n;

            int j = 2;
            int m = 0;
            int d1 = 1;
            int d2 = (n - m + d1) / d1;

            while (d2 > 0)
            {
                k[m, n] = (double)(((n - 1) * (n - 1)) - (m * m)) / ((2 * n - 1) * (2 * n - 3));

                if (m > 0)
                {
                    double flnmj = (double)((n - m + 1) * j) / (n + m);
                    snorm[n + m * 13] = snorm[n + (m - 1) * 13] * Math.Sqrt(flnmj);
                    j = 1;
                    c[n, m - 1] = snorm[n + m * 13] * c[n, m - 1];
                    cd[n, m - 1] = snorm[n + m * 13] * cd[n, m - 1];
                }
                c[m, n] = snorm[n + m * 13] * c[m, n];
                cd[m, n] = snorm[n + m * 13] * cd[m, n];
                d2 -= 1;
                m += d1;
            }
            fn[n] = n + 1;
            fm[n] = n;
        }
        k[1, 1] = 0;
        otime = oalt = olat = olon = -1000.0;
    }

    /** Calculate for given location and date
	- parameters:
		- longitude: Longitude in decimal degrees
		- latitude: Latitude in decimal degrees
		- altitude: Altitude in metres (with respect to WGS-1984 ellipsoid)
		- date: Date of the calculation*/
    private void Calculate(double latitude, double longitude, double altitude, DateTime date)
    {
        double rlon = ToRadians(longitude);
        double rlat = ToRadians(latitude);
        double altitudeKm = altitude / 1000;
        Declination = double.NaN;
        Inclination = double.NaN;
        Intensity = double.NaN;
        HorizontalIntensity = double.NaN;
        VerticalIntensity = double.NaN;
        NorthIntensity = double.NaN;
        EastIntensity = double.NaN;

        int year = date.Year;
        int yearLength = DateTime.IsLeapYear(date.Year) ? 366 : 365;

        double yearFraction = year + (double)(date.DayOfYear - 1) / yearLength;
        double dt = yearFraction - epoch;
        double srlon = Math.Sin(rlon);
        double srlat = Math.Sin(rlat);
        double crlon = Math.Cos(rlon);
        double crlat = Math.Cos(rlat);
        double srlat2 = srlat * srlat;
        double crlat2 = crlat * crlat;
        double a2 = WGS84_A * WGS84_A;
        double b2 = WGS84_B * WGS84_B;
        double c2 = a2 - b2;
        double a4 = a2 * a2;
        double b4 = b2 * b2;
        double c4 = a4 - b4;

        sp[1] = srlon;
        cp[1] = crlon;

        // Convert from geodetic coords. to spherical coords.
        if (altitudeKm != oalt || latitude != olat)
        {
            double q = Math.Sqrt(a2 - c2 * srlat2);
            double q1 = altitudeKm * q;
            double q2 = (q1 + a2) / (q1 + b2) * ((q1 + a2) / (q1 + b2));
            double r2 = (altitudeKm * altitudeKm) + 2 * q1 + (a4 - c4 * srlat2) / (q * q);

            ct = srlat / Math.Sqrt(q2 * crlat2 + srlat2);
            st = Math.Sqrt(1 - (ct * ct));
            r = Math.Sqrt(r2);
            d = Math.Sqrt(a2 * crlat2 + b2 * srlat2);
            ca = (altitudeKm + d) / r;
            sa = c2 * crlat * srlat / (r * d);
        }
        if (longitude != olon)
        {
            for (var m = 2; m <= MAX_DEG; m++)
            {
                sp[m] = sp[1] * cp[m - 1] + cp[1] * sp[m - 1];
                cp[m] = cp[1] * cp[m - 1] - sp[1] * sp[m - 1];
            }
        }
        double aor = IAU66_RADIUS / r;

        double ar = aor * aor;
        double br = 0;
        double bt = 0;
        double bp = 0;
        double bpp = 0;
        double par;
        double parp;
        double temp1;
        double temp2;


        for (var n = 1; n <= MAX_DEG; n++)
        {
            ar *= aor;

            int m = 0;
            int d3 = 1;
            int d4 = (n + m + d3) / d3;

            while (d4 > 0)
            {

                // Compute unnormalized associated legendre polynomials and derivatives via recursion relations
                if (altitudeKm != oalt || latitude != olat)
                {
                    if (n == m)
                    {
                        snorm[n + m * 13] = st * snorm[n - 1 + (m - 1) * 13];
                        dp[m, n] = st * dp[m - 1, n - 1] + ct * snorm[n - 1 + (m - 1) * 13];
                    }
                    if (n == 1 && m == 0)
                    {
                        snorm[n + m * 13] = ct * snorm[n - 1 + m * 13];
                        dp[m, n] = ct * dp[m, n - 1] - st * snorm[n - 1 + m * 13];
                    }
                    if (n > 1 && n != m)
                    {
                        if (m > n - 2)
                            snorm[n - 2 + m * 13] = 0;
                        if (m > n - 2)
                            dp[m, n - 2] = 0;
                        snorm[n + m * 13] = ct * snorm[n - 1 + m * 13] - k[m, n] * snorm[n - 2 + m * 13];
                        dp[m, n] = ct * dp[m, n - 1] - st * snorm[n - 1 + m * 13] - k[m, n] * dp[m, n - 2];
                    }
                }

                // Time adjust the gauss coefficients
                if (yearFraction != otime)
                {
                    tc[m, n] = c[m, n] + dt * cd[m, n];

                    if (m != 0)
                        tc[n, m - 1] = c[n, m - 1] + dt * cd[n, m - 1];
                }

                // Accumulate terms of the spherical harmonic expansions
                par = ar * snorm[n + m * 13];

                if (m == 0)
                {
                    temp1 = tc[m, n] * cp[m];
                    temp2 = tc[m, n] * sp[m];
                }
                else
                {
                    temp1 = tc[m, n] * cp[m] + tc[n, m - 1] * sp[m];
                    temp2 = tc[m, n] * sp[m] - tc[n, m - 1] * cp[m];
                }

                bt -= ar * temp1 * dp[m, n];

                bp += fm[m] * temp2 * par;

                br += fn[n] * temp1 * par;

                // Special case: north/south geographic poles
                if (st == 0 && m == 1)
                {
                    if (n == 1)
                        pp[n] = pp[n - 1];
                    else
                        pp[n] = ct * pp[n - 1] - k[m, n] * pp[n - 2];
                    parp = ar * pp[n];
                    bpp += fm[m] * temp2 * parp;
                }
                d4 -= 1;
                m += d3;
            }
        }

        if (st == 0)
            bp = bpp;
        else
            bp /= st;

        // Rotate magnetic vector components from spherical to geodetic coordinates
        // northIntensity must be the east-west field component
        // eastIntensity must be the north-south field component
        // verticalIntensity must be the vertical field component.
        NorthIntensity = -bt * ca - br * sa;
        EastIntensity = bp;
        VerticalIntensity = bt * sa - br * ca;

        // Compute declination (dec), inclination (dip) and total intensity (ti)
        HorizontalIntensity = Math.Sqrt((NorthIntensity * NorthIntensity) + (EastIntensity * EastIntensity));

        Intensity = Math.Sqrt((HorizontalIntensity * HorizontalIntensity) + (VerticalIntensity * VerticalIntensity));
        //	Calculate the declination.
        Declination = ToDegrees(Math.Atan2(EastIntensity, NorthIntensity));

        Inclination = ToDegrees(Math.Atan2(VerticalIntensity, HorizontalIntensity));

        otime = yearFraction;
        oalt = altitudeKm;
        olat = latitude;
        olon = longitude;
    }

    /** Geomagnetic declination (decimal degrees) [opposite of variation, positive Eastward/negative Westward]*/
    public double Declination { get; private set; }

    /** Geomagnetic inclination/dip angle (degrees) [positive downward]*/
    public double Inclination { get; private set; }

    /** Geomagnetic field intensity/strength (nano Teslas)*/
    public double Intensity { get; private set; }

    /** Geomagnetic horizontal field intensity/strength (nano Teslas)*/
    public double HorizontalIntensity { get; private set; }

    /** Geomagnetic vertical field intensity/strength (nano Teslas) [positive downward]*/
    public double VerticalIntensity { get; private set; }

    /** Geomagnetic North South (northerly component) field intensity/strength (nano Tesla)*/
    public double NorthIntensity { get; private set; }

    /** Geomagnetic East West (easterly component) field intensity/strength (nano Teslas)*/
    public double EastIntensity { get; private set; }

    private static double ToRadians(double v) => v * (Math.PI / 180);
    private static double ToDegrees(double v) => v * (180 / Math.PI);

    /** Mean radius of IAU-66 ellipsoid, in km*/
    private static readonly double IAU66_RADIUS = 6371.2;

    /** Semi-major axis of WGS-1984 ellipsoid, in km*/
    private static readonly double WGS84_A = 6378.137;

    /** Semi-minor axis of WGS-1984 ellipsoid, in km*/
    private static readonly double WGS84_B = 6356.7523142;

    /** The maximum number of degrees of the spherical harmonic model*/
    private static readonly int MAX_DEG = 12;

    /** The Gauss coefficients of main geomagnetic model (nt)*/
    private readonly double[,] c = new double[13, 13];

    /** The Gauss coefficients of secular geomagnetic model (nt/yr)*/
    private readonly double[,] cd = new double[13, 13];

    /** The time adjusted geomagnetic gauss coefficients (nt)*/
    private readonly double[,] tc = new double[13, 13];

    /** The theta derivative of p(n,m) (unnormalized)*/
    private readonly double[,] dp = new double[13, 13];

    /** The Schmidt normalization factors*/
    private readonly double[] snorm = new double[169];

    /** The sine of (m*spherical coordinate longitude)*/
    private readonly double[] sp = new double[13];

    /** The cosine of (m*spherical coordinate longitude)*/
    private readonly double[] cp = new double[13];

    private readonly double[] fn = new double[13];

    private readonly double[] fm = new double[13];

    /** The associated Legendre polynomials for m = 1 (unnormalized)*/
    private readonly double[] pp = new double[13];

    private readonly double[,] k = new double[13, 13];

    /** The variables otime (old time), oalt (old altitude),
	*	olat (old latitude), olon (old longitude), are used to
	*	store the values used from the previous calculation to
	*	save on calculation time if some inputs don't change*/
    private double otime;
    private double oalt;
    private double olat;
    private double olon;

    /** The date in years, for the start of the valid time of the fit coefficients*/
    private double epoch;

    private double r = double.NaN;
    private double d = double.NaN;
    private double ca = double.NaN;
    private double sa = double.NaN;
    private double ct = double.NaN;
    private double st = double.NaN;

    /**	The input string array which contains each line of input for the wmm.cof input file.
	*	The columns in this file are as follows:	n,	m,	gnm,	hnm,	dgnm,	dhnm*/
    private static readonly string[] WMM_COF = new[] {
            "    2020.0            WMM-2020        12/10/2019",
            "  1  0  -29404.5       0.0        6.7        0.0",
            "  1  1   -1450.7    4652.9        7.7      -25.1",
            "  2  0   -2500.0       0.0      -11.5        0.0",
            "  2  1    2982.0   -2991.6       -7.1      -30.2",
            "  2  2    1676.8    -734.8       -2.2      -23.9",
            "  3  0    1363.9       0.0        2.8        0.0",
            "  3  1   -2381.0     -82.2       -6.2        5.7",
            "  3  2    1236.2     241.8        3.4       -1.0",
            "  3  3     525.7    -542.9      -12.2        1.1",
            "  4  0     903.1       0.0       -1.1        0.0",
            "  4  1     809.4     282.0       -1.6        0.2",
            "  4  2      86.2    -158.4       -6.0        6.9",
            "  4  3    -309.4     199.8        5.4        3.7",
            "  4  4      47.9    -350.1       -5.5       -5.6",
            "  5  0    -234.4       0.0       -0.3        0.0",
            "  5  1     363.1      47.7        0.6        0.1",
            "  5  2     187.8     208.4       -0.7        2.5",
            "  5  3    -140.7    -121.3        0.1       -0.9",
            "  5  4    -151.2      32.2        1.2        3.0",
            "  5  5      13.7      99.1        1.0        0.5",
            "  6  0      65.9       0.0       -0.6        0.0",
            "  6  1      65.6     -19.1       -0.4        0.1",
            "  6  2      73.0      25.0        0.5       -1.8",
            "  6  3    -121.5      52.7        1.4       -1.4",
            "  6  4     -36.2     -64.4       -1.4        0.9",
            "  6  5      13.5       9.0       -0.0        0.1",
            "  6  6     -64.7      68.1        0.8        1.0",
            "  7  0      80.6       0.0       -0.1        0.0",
            "  7  1     -76.8     -51.4       -0.3        0.5",
            "  7  2      -8.3     -16.8       -0.1        0.6",
            "  7  3      56.5       2.3        0.7       -0.7",
            "  7  4      15.8      23.5        0.2       -0.2",
            "  7  5       6.4      -2.2       -0.5       -1.2",
            "  7  6      -7.2     -27.2       -0.8        0.2",
            "  7  7       9.8      -1.9        1.0        0.3",
            "  8  0      23.6       0.0       -0.1        0.0",
            "  8  1       9.8       8.4        0.1       -0.3",
            "  8  2     -17.5     -15.3       -0.1        0.7",
            "  8  3      -0.4      12.8        0.5       -0.2",
            "  8  4     -21.1     -11.8       -0.1        0.5",
            "  8  5      15.3      14.9        0.4       -0.3",
            "  8  6      13.7       3.6        0.5       -0.5",
            "  8  7     -16.5      -6.9        0.0        0.4",
            "  8  8      -0.3       2.8        0.4        0.1",
            "  9  0       5.0       0.0       -0.1        0.0",
            "  9  1       8.2     -23.3       -0.2       -0.3",
            "  9  2       2.9      11.1       -0.0        0.2",
            "  9  3      -1.4       9.8        0.4       -0.4",
            "  9  4      -1.1      -5.1       -0.3        0.4",
            "  9  5     -13.3      -6.2       -0.0        0.1",
            "  9  6       1.1       7.8        0.3       -0.0",
            "  9  7       8.9       0.4       -0.0       -0.2",
            "  9  8      -9.3      -1.5       -0.0        0.5",
            "  9  9     -11.9       9.7       -0.4        0.2",
            " 10  0      -1.9       0.0        0.0        0.0",
            " 10  1      -6.2       3.4       -0.0       -0.0",
            " 10  2      -0.1      -0.2       -0.0        0.1",
            " 10  3       1.7       3.5        0.2       -0.3",
            " 10  4      -0.9       4.8       -0.1        0.1",
            " 10  5       0.6      -8.6       -0.2       -0.2",
            " 10  6      -0.9      -0.1       -0.0        0.1",
            " 10  7       1.9      -4.2       -0.1       -0.0",
            " 10  8       1.4      -3.4       -0.2       -0.1",
            " 10  9      -2.4      -0.1       -0.1        0.2",
            " 10 10      -3.9      -8.8       -0.0       -0.0",
            " 11  0       3.0       0.0       -0.0        0.0",
            " 11  1      -1.4      -0.0       -0.1       -0.0",
            " 11  2      -2.5       2.6       -0.0        0.1",
            " 11  3       2.4      -0.5        0.0        0.0",
            " 11  4      -0.9      -0.4       -0.0        0.2",
            " 11  5       0.3       0.6       -0.1       -0.0",
            " 11  6      -0.7      -0.2        0.0        0.0",
            " 11  7      -0.1      -1.7       -0.0        0.1",
            " 11  8       1.4      -1.6       -0.1       -0.0",
            " 11  9      -0.6      -3.0       -0.1       -0.1",
            " 11 10       0.2      -2.0       -0.1        0.0",
            " 11 11       3.1      -2.6       -0.1       -0.0",
            " 12  0      -2.0       0.0        0.0        0.0",
            " 12  1      -0.1      -1.2       -0.0       -0.0",
            " 12  2       0.5       0.5       -0.0        0.0",
            " 12  3       1.3       1.3        0.0       -0.1",
            " 12  4      -1.2      -1.8       -0.0        0.1",
            " 12  5       0.7       0.1       -0.0       -0.0",
            " 12  6       0.3       0.7        0.0        0.0",
            " 12  7       0.5      -0.1       -0.0       -0.0",
            " 12  8      -0.2       0.6        0.0        0.1",
            " 12  9      -0.5       0.2       -0.0       -0.0",
            " 12 10       0.1      -0.9       -0.0       -0.0",
            " 12 11      -1.1      -0.0       -0.0        0.0",
            " 12 12      -0.3       0.5       -0.1       -0.1"
    };
}