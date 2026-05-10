using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPointWallet;

public record GetMyPointWalletQuery() : IRequest<Result<PointWalletSummaryDto>>;
