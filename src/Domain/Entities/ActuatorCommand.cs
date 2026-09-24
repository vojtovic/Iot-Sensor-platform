namespace Domain;

public class ActuatorCommand
{
    public int Id { get; set; }
    public int ActuatorId { get; set; }
    public Actuator Actuator { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public CommandStatus Status { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? Acknowledged { get; set; }
}
