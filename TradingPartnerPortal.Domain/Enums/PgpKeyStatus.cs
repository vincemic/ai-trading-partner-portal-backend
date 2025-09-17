namespace TradingPartnerPortal.Domain.Enums;

public enum PgpKeyStatus
{
    PendingActivation,
    Active,
    Revoked,
    Expired,
    Superseded
}