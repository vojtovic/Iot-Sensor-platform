namespace Domain;



using System;


public enum ClaimStatus
{
    Credentials,
    Pending,
    Rejected,
    Error
}


public record ClaimResponseMessage(ClaimStatus Status, DateTimeOffset? ServerTime, string? Username, string? Password, int? RetryAfter, string? Message, ClaimChannels? Channels);
public record ClaimChannels(ChannelGroup Sensors, ChannelGroup Actuators);
public record ChannelGroup(IReadOnlyList<string> Accepted, IReadOnlyList<string> Pending);
