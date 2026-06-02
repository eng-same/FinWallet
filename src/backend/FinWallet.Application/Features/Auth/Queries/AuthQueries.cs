using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FinWallet.Application.Features.Auth.Queries;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            throw new FinWalletException("USER_NOT_FOUND", "User not found.");
        }

        return new UserDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        );
    }
}
