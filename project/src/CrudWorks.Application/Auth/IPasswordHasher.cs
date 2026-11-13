namespace CrudWorks.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string password);

    // The hash is nullable so the caller can always run a verification — with a
    // dummy hash on the unknown-email path — and keep both login failure paths
    // constant-time (no user enumeration via response timing).
    bool Verify(string password, string? hash);
}
