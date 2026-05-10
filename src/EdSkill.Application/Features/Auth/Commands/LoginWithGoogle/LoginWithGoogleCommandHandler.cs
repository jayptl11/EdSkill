using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth;
using EdSkill.Application.Features.Auth.DTOs;
using EdSkill.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuth;
    private readonly ITokenService _tokenService;

    public LoginWithGoogleCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuth,
        ITokenService tokenService)
    {
        _context = context;
        _googleAuth = googleAuth;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuth.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        if (googleUser == null)
            return Result<LoginResponse>.Failure("INVALID_GOOGLE_TOKEN", "Invalid Google token");

        var signupIntent = SignupIntents.Normalize(request.SignupIntent);

        var user = await _context.Users
            .Include(u => u.UserProfile)
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email, cancellationToken);

        if (user == null)
        {
            var username = googleUser.Email.Split('@')[0];
            var finalUsername = await EnsureUniqueUsernameAsync(username, cancellationToken);
            var now = DateTime.UtcNow;

            user = new User
            {
                UserId = NewId.NextGuid(),
                Email = googleUser.Email,
                Username = finalUsername,
                PasswordHash = string.Empty,
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                CreatedAt = now,
                Status = "active",
                Roles = SignupIntents.GetRoles(signupIntent).ToList()
            };

            _context.Users.Add(user);

            var userProfile = new UserProfile
            {
                ProfileId = NewId.NextGuid(),
                UserId = user.UserId,
                DisplayName = BuildDisplayName(googleUser.GivenName, googleUser.FamilyName, finalUsername),
                IsPublic = true,
                CreatedAt = now,
                UpdatedAt = now,
                LastActiveAt = now,
                SkillsToTeach = new List<string>(),
                SkillsToLearn = new List<string>()
            };

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync(cancellationToken);
            user.UserProfile = userProfile;
        }

        if (user.Status == "suspended")
            return Result<LoginResponse>.Failure("ACCOUNT_SUSPENDED", "Account is suspended");

        user.LastLogin = DateTime.UtcNow;
        if (user.UserProfile != null)
        {
            user.UserProfile.LastActiveAt = user.LastLogin;
            user.UserProfile.UpdatedAt = user.LastLogin.Value;
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            TokenId = NewId.NextGuid(),
            UserId = user.UserId,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays)
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshTokenValue,
            user.UserId,
            user.Email,
            user.Username,
            user.LastLogin,
            user.Roles,
            false
        ));
    }

    private async Task<string> EnsureUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var candidate = baseUsername;
        var suffix = 0;

        while (await _context.Users.AsNoTracking().AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            suffix++;
            candidate = $"{baseUsername}{suffix}";
        }

        return candidate;
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string fallback)
    {
        var combinedName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(combinedName) ? fallback : combinedName;
    }
}
