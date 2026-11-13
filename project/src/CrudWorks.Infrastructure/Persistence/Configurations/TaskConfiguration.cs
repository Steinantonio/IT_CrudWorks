using CrudWorks.Domain.Tasks;
using CrudWorks.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrudWorks.Infrastructure.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(task => task.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One indexed query per list: filter by UserId, order by CreatedAtUtc.
        builder.HasIndex(task => task.UserId)
            .HasDatabaseName("IX_Tasks_UserId");

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(TaskItem.TitleMaxLength);

        builder.Property(task => task.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        // The enum is stored as its contract name ("Todo", "InProgress", "Done") so
        // the data stays readable; integer status values are unreadable in a demo.
        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(task => task.DueDate);

        // SQLite stores DateTime as text and reads it back with an unspecified kind;
        // reattach UTC so serialized timestamps keep the trailing "Z".
        builder.Property(task => task.CreatedAtUtc)
            .IsRequired()
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.Property(task => task.UpdatedAtUtc)
            .IsRequired()
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
