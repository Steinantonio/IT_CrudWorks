using System.Text.Json;
using System.Text.Json.Serialization;
using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace CrudWorks.Api.Pagination;

/// <summary>
/// Encodes and decodes the opaque cursor from ADR-0006. The token carries only
/// the caller's own sort key; it is not signed and must never be treated as an
/// authorization token. Ownership is enforced by the repository's UserId filter.
/// </summary>
internal static class TaskCursorCodec
{
    private const int CurrentVersion = 1;
    private const string InvalidMessage = "Cursor is invalid or no longer supported.";

    public static string Encode(TaskPageCursor cursor)
    {
        var payload = new CursorPayload
        {
            V = CurrentVersion,
            C = cursor.CreatedAtUtc,
            I = cursor.Id,
        };

        return WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    public static TaskPageCursor Decode(string cursor)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<CursorPayload>(
                WebEncoders.Base64UrlDecode(cursor));

            if (payload is null
                || payload.V != CurrentVersion
                || payload.C.Kind != DateTimeKind.Utc
                || payload.I is null
                || payload.I == Guid.Empty)
            {
                throw new FormatException();
            }

            return new TaskPageCursor(payload.C, payload.I.Value);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentException)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["cursor"] = [InvalidMessage],
            });
        }
    }

    private sealed class CursorPayload
    {
        [JsonRequired]
        [JsonPropertyName("v")]
        public int V { get; init; }

        [JsonRequired]
        [JsonPropertyName("c")]
        public DateTime C { get; init; }

        [JsonRequired]
        [JsonPropertyName("i")]
        public Guid? I { get; init; }
    }
}
