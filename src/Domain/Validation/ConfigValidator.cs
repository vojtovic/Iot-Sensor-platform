namespace Domain;

using System.Collections.Generic;

public class ConfigValidator : IConfigValidator
{

    public bool Evaluate(ConfigMessage message, ICollection<Sensor> Sensors)
    {

        HashSet<string> checkedChannel = new HashSet<string>();


        if (message is null)
        {
            return false;
        }
        if (message.Channels is null)
        {
            return false;
        }
        if (message.Interval > 86400 || message.Interval < 10)
        {
            return false;
        }
        if (message.MaxSilence > 86400 || message.MaxSilence < message.Interval)
        {
            return false;
        }
        if (message.Channels.Sensors is null)
        {
            return false;
        }
        foreach (var c in message.Channels.Sensors)
        {
            if (!checkedChannel.Add(c.Channel))
            {
                return false;
            }
        }


        foreach (var s in message.Channels.Sensors)
        {
            var sensors = Sensors.FirstOrDefault(x => x.Channel == s.Channel);
            if (sensors is null)
            {
                return false;
            }
            if (s.Deadband < 0 || sensors.SensorType.MaxValid - sensors.SensorType.MinValid < s.Deadband)
            {
                return false;
            }

        }
        return true;

    }
}
