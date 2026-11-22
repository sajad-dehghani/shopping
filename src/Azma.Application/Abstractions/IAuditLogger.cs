namespace Azma.Application.Abstractions;

/// <summary>Who performed an administrative action.</summary>
public sealed record AdminActor(string? UserId, string? UserName, string? IpAddress = null)
{
    public static readonly AdminActor System = new(null, "system");
}

public interface IAuditLogger
{
    Task LogAsync(AdminActor actor, string action, string entityType, object? entityId, string? details = null, CancellationToken cancellationToken = default);
}
