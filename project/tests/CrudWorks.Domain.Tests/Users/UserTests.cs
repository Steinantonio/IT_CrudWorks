using CrudWorks.Domain.Common;
using CrudWorks.Domain.Users;

namespace CrudWorks.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_NormalizesEmailToTrimmedLowercase()
    {
        var user = User.Create("  DEMO@CrudWorks.LOCAL  ", "Demo User", "hash");

        Assert.Equal("demo@crudworks.local", user.Email);
    }

    [Fact]
    public void Create_TrimsDisplayName()
    {
        var user = User.Create("demo@crudworks.local", "  Demo User  ", "hash");

        Assert.Equal("Demo User", user.DisplayName);
    }

    [Fact]
    public void Create_AssignsIdAndUtcTimestamp()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var user = User.Create("demo@crudworks.local", "Demo User", "hash");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAtUtc.Kind);
        Assert.InRange(user.CreatedAtUtc, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingEmail_ThrowsDomainException(string? email)
    {
        Assert.Throws<DomainException>(() => User.Create(email!, "Demo User", "hash"));
    }

    [Fact]
    public void Create_WithOverlongEmail_ThrowsDomainException()
    {
        var email = $"{new string('a', 250)}@example.com";

        Assert.Throws<DomainException>(() => User.Create(email, "Demo User", "hash"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("  A  ")]
    public void Create_WithDisplayNameOutsideRange_ThrowsDomainException(string displayName)
    {
        Assert.Throws<DomainException>(() => User.Create("demo@crudworks.local", displayName, "hash"));
    }

    [Fact]
    public void Create_WithTooLongDisplayName_ThrowsDomainException()
    {
        var displayName = new string('D', User.DisplayNameMaxLength + 1);

        Assert.Throws<DomainException>(() => User.Create("demo@crudworks.local", displayName, "hash"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingPasswordHash_ThrowsDomainException(string? passwordHash)
    {
        Assert.Throws<DomainException>(() => User.Create("demo@crudworks.local", "Demo User", passwordHash!));
    }
}
