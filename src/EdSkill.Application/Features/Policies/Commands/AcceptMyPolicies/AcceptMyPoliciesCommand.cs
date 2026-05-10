using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Policies.Commands.AcceptMyPolicies;

public record AcceptMyPoliciesCommand(
    IReadOnlyCollection<PolicyAcceptanceInput>? AcceptedPolicies
) : IRequest<Result>;
