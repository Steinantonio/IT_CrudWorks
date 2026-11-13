using CrudWorks.Domain.Users;

namespace CrudWorks.Application.Auth;

public interface ITokenService
{
    TokenResult Create(User user);
}
