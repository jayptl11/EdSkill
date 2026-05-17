using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Commands.CreatePointPurchase;

public record CreatePointPurchaseCommand(Guid PackageId) : IRequest<Result<CreatePointPurchaseResultDto>>;
