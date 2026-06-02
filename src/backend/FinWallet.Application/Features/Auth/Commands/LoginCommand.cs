using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IApplicationDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new FinWalletException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        // 2. Check if active
        if (!user.IsActive)
        {
            throw new FinWalletException("FORBIDDEN", "This user account is deactivated.");
        }

        // 3. Verify password
        var loginResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!loginResult.Succeeded)
        {
            throw new FinWalletException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        // 4. Update last login timestamp
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        // 5. Audit Log
        var audit = new AuditLog
        {
            UserId = user.Id,
            Action = "UserLoggedIn",
            EntityName = nameof(ApplicationUser),
            EntityId = user.Id.ToString(),
            Description = $"User {user.Email} successfully logged in",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 6. Fetch user roles
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "User";

        // 7. Get user's wallet (if they have one)
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id, cancellationToken);
        WalletDto? walletDto = null;
        if (wallet != null)
        {
            walletDto = new WalletDto(wallet.Id, wallet.UserId, wallet.WalletNumber, wallet.Balance, wallet.Currency, wallet.Status.ToString(), wallet.CreatedAt, wallet.UpdatedAt, wallet.FrozenAt, wallet.FrozenReason);
        }

        // 8. Generate token
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.IsActive, user.CreatedAt, user.LastLoginAt);

        return new AuthResponseDto(token, userDto, primaryRole, walletDto);
    }
}
