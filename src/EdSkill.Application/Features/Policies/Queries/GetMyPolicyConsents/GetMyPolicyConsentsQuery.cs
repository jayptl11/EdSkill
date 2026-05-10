using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Policies.Queries.GetMyPolicyConsents;

public record GetMyPolicyConsentsQuery : IRequest<Result<PolicyConsentStatusDto>>;
