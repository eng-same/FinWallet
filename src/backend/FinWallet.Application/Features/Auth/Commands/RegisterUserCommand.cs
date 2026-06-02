using FinWallet.Application.Common.DTOs;
using FinWallet.Application.Common.Exceptions;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using FinWallet.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinWallet.Application.Features.Auth.Commands;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword
) : IRequest<AuthResponseDto>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");
        
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Passwords must match.");
    }
}

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new FinWalletException("VALIDATION_ERROR", "User with this email already exists.");
        }

        // 2. Create the User entity
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "User registration failed.";
            throw new FinWalletException("VALIDATION_ERROR", firstError);
        }

        // 3. Assign the "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // 4. Generate unique wallet number: FW-{CurrentYear}-{NextNumber:D6}
        var currentYear = DateTimeOffset.UtcNow.Year;
        var walletCount = await _dbContext.Wallets.CountAsync(cancellationToken);
        var walletNumber = $"FW-{currentYear}-{(walletCount + 1):D6}";

        // 5. Create the Wallet entity
        var wallet = new Wallet
        {
            UserId = user.Id,
            WalletNumber = walletNumber,
            Balance = 0m,
            Currency = "LYD",
            Status = WalletStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Wallets.Add(wallet);

        // 6. Write Audit Logs
        var userAudit = new AuditLog
        {
            UserId = user.Id,
            Action = "UserRegistered",
            EntityName = nameof(ApplicationUser),
            EntityId = user.Id.ToString(),
            Description = $"User registered with email {user.Email}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(userAudit);

        var walletAudit = new AuditLog
        {
            UserId = user.Id,
            Action = "WalletCreated",
            EntityName = nameof(Wallet),
            EntityId = wallet.Id.ToString(),
            Description = $"Wallet created automatically for user: {walletNumber}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.AuditLogs.Add(walletAudit);

        // Save changes to database (the TransactionBehavior will commit the transaction)
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 7. Generate JWT token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.IsActive, user.CreatedAt, user.LastLoginAt);
        var walletDto = new WalletDto(wallet.Id, wallet.UserId, wallet.WalletNumber, wallet.Balance, wallet.Currency, wallet.Status.ToString(), wallet.CreatedAt, wallet.UpdatedAt, wallet.FrozenAt, wallet.FrozenReason);

        return new AuthResponseDto(token, userDto, "User", walletDto);
    }
}
