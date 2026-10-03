namespace Domain;



using System;




public record ClaimRequestMessage(string HardwareId, string? ClaimToken, string FirmwareVersion, DateTimeOffset? Ts, IReadOnlyList<SensorDeclaration>? Sensors, IReadOnlyList<ActuatorDeclaration>? Actuators);
public record SensorDeclaration(string Channel, string Type);
public record ActuatorDeclaration(string Channel, string Kind);
