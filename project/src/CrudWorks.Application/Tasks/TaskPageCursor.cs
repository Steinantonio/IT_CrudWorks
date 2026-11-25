namespace CrudWorks.Application.Tasks;

/// <summary>
/// The keyset position of the last row on a page. The API encodes it into the
/// opaque cursor; the Application layer only needs the typed values.
/// </summary>
public sealed record TaskPageCursor(DateTime CreatedAtUtc, Guid Id);
