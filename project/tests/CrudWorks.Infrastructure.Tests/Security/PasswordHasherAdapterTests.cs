using CrudWorks.Infrastructure.Security;

namespace CrudWorks.Infrastructure.Tests.Security;

public sealed class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter _hasher = new();

    [Fact]
    public void Hash_DoesNotStoreThePlaintextPassword()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.NotEqual("Secret123", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.True(_hasher.Verify("Secret123", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.False(_hasher.Verify("Secret124", hash));
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("Secret123", "not-a-password-hash"));
    }

    // The null/empty hash is the unknown-email path: it must still run a full
    // PBKDF2 verification (against a dummy hash) so response timing matches the
    // wrong-password path and cannot be used to enumerate accounts.
    [Fact]
    public void Verify_WithNullHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("Secret123", null));
    }

    [Fact]
    public void Verify_WithEmptyHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("Secret123", string.Empty));
    }
}
