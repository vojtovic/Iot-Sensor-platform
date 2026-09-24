namespace Domain;



using System;



public record TelemetryMessage(DateTimeOffset? Ts, long Seq, IReadOnlyList<Reading> Readings);
public record Reading(string Channel, double Value, string? Status);
