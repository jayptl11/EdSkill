using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Wallet.Queries.GetMyPayments;

public record GetMyPaymentsQuery(string? Status, int Page, int Limit) : IRequest<Result<PaymentTransactionHistoryDto>>;
