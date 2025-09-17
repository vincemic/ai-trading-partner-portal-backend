namespace TradingPartnerPortal.Application.DTOs;

public class AuditEventDto
{
    public string AuditId { get; set; } = string.Empty;
    public string PartnerId { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Metadata { get; set; }
}