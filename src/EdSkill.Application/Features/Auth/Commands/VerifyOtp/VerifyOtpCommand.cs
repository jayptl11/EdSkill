using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Auth.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string Email,
    string Otp
) : IRequest<Result<VerifyOtpResponse>>;
