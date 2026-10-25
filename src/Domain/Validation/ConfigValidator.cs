namespace Domain;

using System.Collections.Generic;

public class ConfigValidator : IConfigValidator
{

    public string? Evaluate(ConfigMessage message, ICollection<Sensor> Sensors)
    {

        HashSet<string> checkedChannel = new HashSet<string>();


        if (message is null)
        {
            return "message can not be null";
        }
        if (message.Channels is null)
        {
            return "message channel can not be null";
        }
        if (message.Interval > 86400 || message.Interval < 10)
        {
            return "interval has to be bigger than 10 and smaller than 86400";
        }
        if (message.MaxSilence > 86400 || message.MaxSilence < message.Interval)
        {
            return "max silence has to be smaller than 86400 and bigger than message interval";
        }
        if (message.Channels.Sensors is null)
        {
            return "message channel sensors can not be null";
        }
        foreach (var c in message.Channels.Sensors)
        {
            if (!checkedChannel.Add(c.Channel))
            {
                return $"message channel sensor can not be duplicit : {c.Channel}";
            }
        }


        foreach (var s in message.Channels.Sensors)
        {
            var sensors = Sensors.FirstOrDefault(x => x.Channel == s.Channel);
            if (sensors is null)
            {
                return $"device has no channel: {s.Channel} ";
            }
            if (s.Deadband < 0 || sensors.SensorType.MaxValid - sensors.SensorType.MinValid < s.Deadband)
            {
                var temp = sensors.SensorType.MaxValid - sensors.SensorType.MinValid;
                return $"Channel: {s.Channel} deadband has to bigger than 0 , Deadband has to be smaller than {temp}";
            }

        }
        return null;

    }
}
