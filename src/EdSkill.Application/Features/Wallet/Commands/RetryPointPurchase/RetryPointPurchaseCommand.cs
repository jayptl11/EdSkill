using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.RetryPointPurchase;

public record RetryPointPurchaseCommand(Guid PaymentTransactionId) : IRequest<Result<CreatePointPurchaseResultDto>>;
