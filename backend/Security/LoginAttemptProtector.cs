using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ZeroPaper.Security;

public sealed class LoginAttemptProtector
{
    private sealed record AttemptState(int Failures, DateTime LastFailureUtc);
    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new(StringComparer.Ordinal);

    public async Task DelayIfNeededAsync(HttpContext context, string? identifier, CancellationToken cancellationToken)
    {
        var key = BuildKey(context, identifier);
        if (!_attempts.TryGetValue(key, out var state)) return;
        if (state.LastFailureUtc < DateTime.UtcNow.AddMinutes(-15))
        {
            _attempts.TryRemove(key, out _);
            return;
        }

        if (state.Failures < 3) return;
        var delayMs = Math.Min(4000, 250 * (1 << Math.Min(4, state.Failures - 3)));
        await Task.Delay(delayMs, cancellationToken);
    }

    public void RecordFailure(HttpContext context, string? identifier)
    {
        var key = BuildKey(context, identifier);
        _attempts.AddOrUpdate(key,
            _ => new AttemptState(1, DateTime.UtcNow),
            (_, current) => new AttemptState(Math.Min(20, current.Failures + 1), DateTime.UtcNow));
    }

    public void Reset(HttpContext context, string? identifier) =>
        _attempts.TryRemove(BuildKey(context, identifier), out _);

    private static string BuildKey(HttpContext context, string? identifier)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var normalized = identifier?.Trim().ToLowerInvariant() ?? string.Empty;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ip}|{normalized}")));
    }
}
