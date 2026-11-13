using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrudWorks.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c>. Keeping design-time commands out of the API host
/// means migrations do not depend on runtime configuration such as the JWT key.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CrudWorksDbContext>
{
    public CrudWorksDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CrudWorksDbContext>()
            .UseSqlite("Data Source=crudworks.db")
            .Options;

        return new CrudWorksDbContext(options);
    }
}
