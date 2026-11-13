using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Persistence;

public sealed class UnitOfWork(CrudWorksDbContext dbContext) : IUnitOfWork
{
    private const int SqliteConstraintUnique = 2067;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        // Keyed on the violated columns (SQLite reports them in the message, e.g.
        // "UNIQUE constraint failed: Users.Email") so a future unique index on
        // another table is never misreported as an email conflict.
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception, "Users.Email"))
        {
            throw new ConflictException("Email already registered.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, string columns)
    {
        if (exception.InnerException is not SqliteException
            {
                SqliteExtendedErrorCode: SqliteConstraintUnique
            } sqliteException)
        {
            return false;
        }

        // The message lists the violated columns as "table.column" (SQLite reports
        // columns, not index names, for simple unique indexes); matching the column
        // pair keeps a future "Tasks.<column>" violation from the email conflict.
        return sqliteException.Message.Contains(
            $"UNIQUE constraint failed: {columns}", StringComparison.OrdinalIgnoreCase);
    }
}
