namespace TradingPartnerPortal.Domain.Enums;

public enum AuditOperationType
{
    KeyUpload,
    KeyGenerate,
    KeyRevoke,
    KeyDownload,
    SftpPasswordChange,
    KeyPromote,
    KeyDemote
}