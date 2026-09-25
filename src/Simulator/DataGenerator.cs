namespace Simulator;



using System;

public class DataGenerator
{
    public double GenerateValue(double Min, double Max, double variance)
    {
        var value = Min + Random.Shared.NextDouble() * (Max - Min);
        value += Random.Shared.NextDouble() * variance;
        value -= Random.Shared.NextDouble() * variance;
        value = Math.Round(value, 3);
        if (Random.Shared.Next(100) == 0)
        {
            value = 10000;
        }
        return value;
    }

    public string? GenerateStatus()
    {
        if (Random.Shared.Next(150) == 0)
        {
            return "error";
        }
        else
        {
            return null;
        }
    }
}
