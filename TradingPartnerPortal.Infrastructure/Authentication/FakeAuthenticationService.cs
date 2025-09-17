using TradingPartnerPortal.Application.Models;
using System.Collections.Concurrent;

namespace TradingPartnerPortal.Infrastructure.Authentication;

public class FakeAuthenticationService
{
    private readonly ConcurrentDictionary<string, FakeSession> _sessions = new();
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromHours(8);

    public class FakeSession
    {
        public string UserId { get; set; } = string.Empty;
        public Guid PartnerId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    public class FakeLoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string PartnerId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // PartnerUser, PartnerAdmin, InternalSupport
    }

    public class FakeLoginResponse
    {
        public string SessionToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string PartnerId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ExpiresAt { get; set; } = string.Empty;
    }

    public FakeLoginResponse CreateSession(FakeLoginRequest request)
    {
        if (!IsValidRole(request.Role))
        {
            throw new ArgumentException("Invalid role. Must be PartnerUser, PartnerAdmin, or InternalSupport");
        }

        if (!Guid.TryParse(request.PartnerId, out var partnerId))
        {
            throw new ArgumentException("Invalid partner ID format");
        }

        var sessionToken = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        
        var session = new FakeSession
        {
            UserId = request.UserId,
            PartnerId = partnerId,
            Role = request.Role,
            CreatedAt = now,
            LastAccessed = now
        };

        _sessions[sessionToken] = session;

        return new FakeLoginResponse
        {
            SessionToken = sessionToken,
            UserId = request.UserId,
            PartnerId = request.PartnerId,
            Role = request.Role,
            ExpiresAt = now.Add(SessionTimeout).ToString("O")
        };
    }

    public UserContext? ValidateSession(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken) || !_sessions.TryGetValue(sessionToken, out var session))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        
        // Check if session has expired
        if (now - session.LastAccessed > SessionTimeout)
        {
            _sessions.TryRemove(sessionToken, out _);
            return null;
        }

        // Update last accessed time
        session.LastAccessed = now;

        return new UserContext
        {
            UserId = session.UserId,
            PartnerId = session.PartnerId,
            Role = session.Role
        };
    }

    public void InvalidateSession(string sessionToken)
    {
        _sessions.TryRemove(sessionToken, out _);
    }

    public void CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _sessions
            .Where(kvp => now - kvp.Value.LastAccessed > SessionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _sessions.TryRemove(token, out _);
        }
    }

    private static bool IsValidRole(string role)
    {
        return role switch
        {
            "PartnerUser" or "PartnerAdmin" or "InternalSupport" => true,
            _ => false
        };
    }
}