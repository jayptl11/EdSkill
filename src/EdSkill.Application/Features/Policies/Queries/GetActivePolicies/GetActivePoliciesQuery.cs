using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Policies.Queries.GetActivePolicies;

public record GetActivePoliciesQuery : IRequest<Result<IReadOnlyCollection<PolicyDocumentSummaryDto>>>;
