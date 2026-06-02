using FinWallet.Domain.Entities;

namespace FinWallet.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}
