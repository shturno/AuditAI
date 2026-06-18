using System.Text.Json;

namespace AuditAI.Application.AuditLogs.Services;

internal static class AuditLogMetadata
{
    private static readonly string[] ForbiddenKeyFragments =
    [
        "password",
        "token",
        "jwt",
        "secret",
        "connectionstring",
        "credential"
    ];

    public static string? Build(params (string Key, object? Value)[] values)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                continue;
            }

            if (ForbiddenKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            metadata[key] = value;
        }

        return metadata.Count == 0 ? null : JsonSerializer.Serialize(metadata);
    }
}
