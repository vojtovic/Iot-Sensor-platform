namespace Domain;



using System;


public record AckMessage(int Version, bool Accepted, string? Error);
