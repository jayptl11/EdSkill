using EdSkill.Application.Common.Models;
using EdSkill.Domain.Enums;

namespace EdSkill.Application.Common.Interfaces;

public interface IOTPCacheService
{
    Task<Result> GenerateAndStoreOtpAsync(
        string email,
        OtpPurpose purpose,
        string? additionalData,
        CancellationToken cancellationToken);

    Task<Result<(string data, OtpPurpose purpose)>> VerifyOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken);

    Task<Result> ResendOtpAsync(
        string email,
        CancellationToken cancellationToken);

    Task<Result<string>> GetDataAsync(
        string email,
        CancellationToken cancellationToken);

    Task DeleteOtpDataAsync(
        string email,
        CancellationToken cancellationToken);
        
    string GetLastGeneratedOtp();
}
