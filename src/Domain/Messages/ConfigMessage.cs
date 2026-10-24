namespace Domain;



using System;


public record ConfigMessage(int Version, int Interval, int MaxSilence, ChannelConfig? Channels);
public record ChannelConfig(IReadOnlyList<SensorConfig> Sensors);
public record SensorConfig(bool Enabled, double Deadband, string Channel);
