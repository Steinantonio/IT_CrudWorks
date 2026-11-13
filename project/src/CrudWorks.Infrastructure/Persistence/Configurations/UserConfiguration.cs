using CrudWorks.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrudWorks.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        // The application checks for duplicates first; this index is the final guard
        // against two concurrent registrations for the same address.
        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(User.DisplayNameMaxLength);

        builder.Property(user => user.PasswordHash)
            .IsRequired();

        // SQLite stores DateTime as text and reads it back with an unspecified kind;
        // reattach UTC so serialized timestamps keep the trailing "Z".
        builder.Property(user => user.CreatedAtUtc)
            .IsRequired()
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
