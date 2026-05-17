using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.ProcessVnPayReturnCallback;

public record ProcessVnPayReturnCallbackCommand(IReadOnlyDictionary<string, string> Payload) : IRequest<Result<VnPayReturnResultDto>>;
