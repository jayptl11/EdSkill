using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Common.Interfaces;

public interface IPolicyConsentService
{
    Task<Result> ValidateRegistrationPolicyAcceptancesAsync(
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<PolicyConsent>>> BuildRegistrationPolicyConsentsAsync(
        Guid userId,
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken);

    Task<Result> AcceptPoliciesAsync(
        Guid userId,
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken);

    Task<Result<PolicyConsentStatusDto>> GetConsentStatusAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
