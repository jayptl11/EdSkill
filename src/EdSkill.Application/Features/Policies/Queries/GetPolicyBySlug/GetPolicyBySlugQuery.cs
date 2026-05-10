using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Policies.Queries.GetPolicyBySlug;

public record GetPolicyBySlugQuery(string Slug) : IRequest<Result<PolicyDocumentDetailDto>>;
