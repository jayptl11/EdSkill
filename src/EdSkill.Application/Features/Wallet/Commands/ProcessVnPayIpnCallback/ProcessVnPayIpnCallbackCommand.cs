using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayIpnCallback;

public record ProcessVnPayIpnCallbackCommand(IReadOnlyDictionary<string, string> Payload) : IRequest<Result<VnPayIpnResponseDto>>;
