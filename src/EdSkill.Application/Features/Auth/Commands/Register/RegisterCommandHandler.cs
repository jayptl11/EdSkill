using System.Text.Json;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.DTOs;
using EdSkill.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IOTPCacheService _otpCacheService;
    private readonly IPasswordService _passwordService;
    private readonly IPolicyConsentService _policyConsentService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IOTPCacheService otpCacheService,
        IPasswordService passwordService,
        IPolicyConsentService policyConsentService)
    {
        _context = context;
        _emailService = emailService;
        _otpCacheService = otpCacheService;
        _passwordService = passwordService;
        _policyConsentService = policyConsentService;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            return Result.Failure("EMAIL_EXISTS", "Email already registered");
        }

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);

        if (usernameExists)
        {
            return Result.Failure("USERNAME_EXISTS", "Username already taken");
        }

        var policyValidationResult = await _policyConsentService.ValidateRegistrationPolicyAcceptancesAsync(
            request.AcceptedPolicies,
            cancellationToken);

        if (!policyValidationResult.IsSuccess)
        {
            return policyValidationResult;
        }

        var passwordHash = _passwordService.HashPassword(request.Password);
        var roles = request.Roles!
            .Select(role => role.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        var registrationData = JsonSerializer.Serialize(new RegistrationOtpPayload(
            request.Username,
            passwordHash,
            request.FirstName,
            request.LastName,
            roles,
            request.AcceptedPolicies!.ToArray()));

        var result = await _otpCacheService.GenerateAndStoreOtpAsync(
            request.Email,
            OtpPurpose.Register,
            registrationData,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        var otp = _otpCacheService.GetLastGeneratedOtp();
        await _emailService.SendOtpEmailAsync(request.Email, otp, cancellationToken);

        return Result.Success();
    }
}
