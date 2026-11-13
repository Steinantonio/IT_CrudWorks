using CrudWorks.Domain.Tasks;
using CrudWorks.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CrudWorks.Infrastructure.Persistence;

public sealed class CrudWorksDbContext(DbContextOptions<CrudWorksDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrudWorksDbContext).Assembly);
    }
}
