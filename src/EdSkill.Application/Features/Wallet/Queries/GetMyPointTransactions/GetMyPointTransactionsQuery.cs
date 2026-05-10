using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPointTransactions;

public record GetMyPointTransactionsQuery(string? Type, int Page = 1, int Limit = 20) : IRequest<Result<PointTransactionHistoryDto>>;
