using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Auth.Commands.ResendOtp;

public record ResendOtpCommand(string Email) : IRequest<Result>;
