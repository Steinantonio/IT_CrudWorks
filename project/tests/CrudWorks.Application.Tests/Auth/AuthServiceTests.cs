using CrudWorks.Application.Auth;
using CrudWorks.Application.Common.Exceptions;
using CrudWorks.Application.Users;
using CrudWorks.Domain.Users;
using NSubstitute;

namespace CrudWorks.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ILoginAudit _loginAudit = Substitute.For<ILoginAudit>();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_userRepository, _unitOfWork, _passwordHasher, _tokenService, _loginAudit);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_HashesPasswordAndPersistsNormalizedUser()
    {
        _userRepository.FindByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash("Secret123").Returns("hashed-password");

        var response = await _authService.RegisterAsync(
            new RegisterRequest("  NEW@Example.COM ", "New User", "Secret123"),
            CancellationToken.None);

        Assert.Equal("new@example.com", response.Email);
        Assert.Equal("New User", response.DisplayName);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "new@example.com"
                && user.DisplayName == "New User"
                && user.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflictAndDoesNotPersist()
    {
        var existing = User.Create("demo@crudworks.local", "Demo User", "hash");
        _userRepository.FindByEmailAsync("demo@crudworks.local", Arg.Any<CancellationToken>())
            .Returns(existing);

        await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(
            new RegisterRequest("DEMO@crudworks.local", "Other User", "Secret123"),
            CancellationToken.None));

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Theory]
    [InlineData("", "Demo User", "Secret123")]
    [InlineData("not-an-email", "Demo User", "Secret123")]
    [InlineData("demo@crudworks.local", "D", "Secret123")]
    [InlineData("demo@crudworks.local", "Demo User", "short1")]
    [InlineData("demo@crudworks.local", "Demo User", "onlyletters")]
    [InlineData("demo@crudworks.local", "Demo User", "12345678")]
    // Well-formed but 262 characters (> the 256 maximum): must be a 400 validation
    // error, not a DomainException that would surface as a 500.
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@example.com", "Demo User", "Secret123")]
    public async Task RegisterAsync_WithInvalidInput_ThrowsValidationAndDoesNotPersist(
        string email, string displayName, string password)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _authService.RegisterAsync(
            new RegisterRequest(email, displayName, password),
            CancellationToken.None));

        Assert.NotEmpty(exception.Errors);
        await _userRepository.DidNotReceive().FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithMissingFields_ReportsFieldLevelErrors()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _authService.RegisterAsync(
            new RegisterRequest(null, null, null),
            CancellationToken.None));

        Assert.Equal(["email", "displayName", "password"], exception.Errors.Keys);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_VerifiesAgainstNullHashAndThrowsGenericMessage()
    {
        _userRepository.FindByEmailAsync("missing@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("missing@example.com", "Secret123"),
            CancellationToken.None));

        Assert.Equal("Invalid email or password", exception.Message);
        // The hash is null on the unknown-email path; verification still runs so both
        // failure paths do the same hashing work and timing cannot enumerate accounts.
        _passwordHasher.Received(1).Verify("Secret123", (string?)null);
    }

    [Fact]
    public async Task LoginAsync_FailurePaths_ThrowTheSameExceptionTypeAndMessage()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "stored-hash");
        _userRepository.FindByEmailAsync("demo@crudworks.local", Arg.Any<CancellationToken>())
            .Returns(user);
        _userRepository.FindByEmailAsync("missing@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Verify("Wrong123", "stored-hash").Returns(false);

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("demo@crudworks.local", "Wrong123"),
            CancellationToken.None));
        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("missing@example.com", "Wrong123"),
            CancellationToken.None));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
        Assert.Equal("Invalid email or password", unknownEmail.Message);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_AuditsOnlyTheEmailDomain()
    {
        _userRepository.FindByEmailAsync("missing@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("local-part@Example.COM", "Secret123"),
            CancellationToken.None));

        _loginAudit.Received(1).FailedLogin("example.com");
        _loginAudit.DidNotReceive().FailedLogin(Arg.Is<string>(domain =>
            domain.Contains("local-part", StringComparison.Ordinal)
            || domain.Contains("Secret123", StringComparison.Ordinal)
            || domain.Contains('@', StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_AuditsOnlyTheEmailDomain()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "stored-hash");
        _userRepository.FindByEmailAsync("demo@crudworks.local", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("Wrong123", "stored-hash").Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("demo@crudworks.local", "Wrong123"),
            CancellationToken.None));

        _loginAudit.Received(1).FailedLogin("crudworks.local");
        _loginAudit.DidNotReceive().FailedLogin(Arg.Is<string>(domain =>
            domain.Contains("demo", StringComparison.Ordinal)
            || domain.Contains("Wrong123", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsTheSameGenericMessage()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "stored-hash");
        _userRepository.FindByEmailAsync("demo@crudworks.local", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("Wrong123", "stored-hash").Returns(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(
            new LoginRequest("demo@crudworks.local", "Wrong123"),
            CancellationToken.None));

        Assert.Equal("Invalid email or password", exception.Message);
        _tokenService.DidNotReceive().Create(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUserSummary()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "stored-hash");
        var expiresAtUtc = new DateTime(2026, 11, 11, 19, 30, 0, DateTimeKind.Utc);
        _userRepository.FindByEmailAsync("demo@crudworks.local", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("Secret123", "stored-hash").Returns(true);
        _tokenService.Create(user).Returns(new TokenResult("jwt-token", expiresAtUtc));

        var response = await _authService.LoginAsync(
            new LoginRequest("  DEMO@crudworks.local", "Secret123"),
            CancellationToken.None);

        Assert.Equal("jwt-token", response.AccessToken);
        Assert.Equal(expiresAtUtc, response.ExpiresAtUtc);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal("demo@crudworks.local", response.User.Email);
        Assert.Equal("Demo User", response.User.DisplayName);
        _loginAudit.DidNotReceive().FailedLogin(Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ThrowsValidation()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(
            new LoginRequest("demo@crudworks.local", " "),
            CancellationToken.None));

        Assert.Contains("password", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task LoginAsync_WithAMalformedEmail_ThrowsValidationWithTheEmailKey(string email)
    {
        // ADR-0004 §3: a malformed body is a 400 with a field-level error, not a
        // credential failure. The request never reaches the repository, so no
        // account lookup and no password hashing happen for a malformed body.
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(
            new LoginRequest(email, "Secret123"),
            CancellationToken.None));

        Assert.Equal(["email"], exception.Errors.Keys);
        Assert.Equal(["Email must be a valid address."], exception.Errors["email"]);
        await _userRepository.DidNotReceive().FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string?>());
        _tokenService.DidNotReceive().Create(Arg.Any<User>());
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsTheCaller()
    {
        var user = User.Create("demo@crudworks.local", "Demo User", "hash");
        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var response = await _authService.GetCurrentUserAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.DisplayName, response.DisplayName);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithUnknownSubject_ThrowsUnauthorized()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.GetCurrentUserAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
