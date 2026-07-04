using MathNet.Numerics;

namespace ISO11820.Services;

public static class DriftCalculator
{
    public static double CalculateDrift(List<double> temperatures)
    {
        if (temperatures.Count < 10)
            return double.NaN;

        double[] x = Enumerable.Range(0, temperatures.Count).Select(i => (double)i).ToArray();
        double[] y = temperatures.ToArray();

        var (intercept, slope) = Fit.Line(x, y);
        return slope * 600.0; // °C/10min
    }
}
