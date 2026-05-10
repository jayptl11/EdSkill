using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Policies;
using EdSkill.Application.Features.Policies.DTOs;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Common.Services;

public class PolicyConsentService : IPolicyConsentService
{
    private static readonly PolicyType[] RequiredPolicyTypes =
    [
        PolicyType.Terms,
        PolicyType.Privacy,
        PolicyType.PointsTokens
    ];

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PolicyConsentService(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> ValidateRegistrationPolicyAcceptancesAsync(
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken)
    {
        var resolvedResult = await ResolveAcceptedPoliciesAsync(acceptedPolicies, requireAllRequiredPolicies: true, cancellationToken);
        return resolvedResult.IsSuccess
            ? Result.Success()
            : Result.Failure(resolvedResult.ErrorCode!, resolvedResult.ErrorMessage!);
    }

    public async Task<Result<IReadOnlyCollection<PolicyConsent>>> BuildRegistrationPolicyConsentsAsync(
        Guid userId,
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken)
    {
        var resolvedResult = await ResolveAcceptedPoliciesAsync(acceptedPolicies, requireAllRequiredPolicies: true, cancellationToken);
        if (!resolvedResult.IsSuccess)
        {
            return Result<IReadOnlyCollection<PolicyConsent>>.Failure(resolvedResult.ErrorCode!, resolvedResult.ErrorMessage!);
        }

        var acceptedAt = _dateTimeProvider.UtcNow;
        var consents = resolvedResult.Value!
            .Select(policy => new PolicyConsent
            {
                PolicyConsentId = Guid.NewGuid(),
                UserId = userId,
                PolicyType = policy.PolicyType!.Value,
                PolicyVersion = policy.Version,
                AcceptedAt = acceptedAt
            })
            .ToList();

        return Result<IReadOnlyCollection<PolicyConsent>>.Success(consents);
    }

    public async Task<Result> AcceptPoliciesAsync(
        Guid userId,
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        CancellationToken cancellationToken)
    {
        var resolvedResult = await ResolveAcceptedPoliciesAsync(acceptedPolicies, requireAllRequiredPolicies: false, cancellationToken);
        if (!resolvedResult.IsSuccess)
        {
            return Result.Failure(resolvedResult.ErrorCode!, resolvedResult.ErrorMessage!);
        }

        var requestedPolicies = resolvedResult.Value!
            .Select(policy => new { PolicyType = policy.PolicyType!.Value, policy.Version })
            .ToList();

        var requestedPolicyTypes = requestedPolicies.Select(policy => policy.PolicyType).Distinct().ToList();
        var existingConsents = await _context.PolicyConsents
            .Where(consent => consent.UserId == userId)
            .Where(consent => requestedPolicyTypes.Contains(consent.PolicyType))
            .ToListAsync(cancellationToken);

        var acceptedAt = _dateTimeProvider.UtcNow;
        var newConsents = new List<PolicyConsent>();

        foreach (var requestedPolicy in requestedPolicies)
        {
            var alreadyAccepted = existingConsents.Any(consent =>
                consent.PolicyType == requestedPolicy.PolicyType &&
                consent.PolicyVersion == requestedPolicy.Version);

            if (alreadyAccepted)
            {
                continue;
            }

            newConsents.Add(new PolicyConsent
            {
                PolicyConsentId = Guid.NewGuid(),
                UserId = userId,
                PolicyType = requestedPolicy.PolicyType,
                PolicyVersion = requestedPolicy.Version,
                AcceptedAt = acceptedAt
            });
        }

        if (newConsents.Count > 0)
        {
            _context.PolicyConsents.AddRange(newConsents);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<PolicyConsentStatusDto>> GetConsentStatusAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var requiredPolicies = await _context.PolicyDocuments
            .AsNoTracking()
            .Where(document => document.IsActive && document.RequiresAcceptance && document.PolicyType.HasValue)
            .Where(document => RequiredPolicyTypes.Contains(document.PolicyType!.Value))
            .OrderBy(document => document.Title)
            .ToListAsync(cancellationToken);

        if (requiredPolicies.Count != RequiredPolicyTypes.Length)
        {
            return Result<PolicyConsentStatusDto>.Failure("POLICY_DOCUMENT_NOT_FOUND", "Required policy documents are not configured.");
        }

        var latestConsents = await _context.PolicyConsents
            .AsNoTracking()
            .Where(consent => consent.UserId == userId)
            .Where(consent => RequiredPolicyTypes.Contains(consent.PolicyType))
            .GroupBy(consent => consent.PolicyType)
            .Select(group => group
                .OrderByDescending(consent => consent.AcceptedAt)
                .ThenByDescending(consent => consent.PolicyVersion)
                .First())
            .ToListAsync(cancellationToken);

        var consentLookup = latestConsents.ToDictionary(consent => consent.PolicyType);
        var requiredItems = requiredPolicies
            .Select(policy =>
            {
                consentLookup.TryGetValue(policy.PolicyType!.Value, out var acceptedConsent);
                var isAcceptedLatest = acceptedConsent?.PolicyVersion == policy.Version;

                return new PolicyConsentItemDto(
                    PolicyTypeMapper.ToApiValue(policy.PolicyType.Value),
                    policy.Slug,
                    policy.Title,
                    policy.Version,
                    acceptedConsent?.PolicyVersion,
                    acceptedConsent?.AcceptedAt,
                    isAcceptedLatest);
            })
            .ToList();

        var missingRequiredTypes = requiredItems
            .Where(item => !item.IsAcceptedLatest)
            .Select(item => item.PolicyType)
            .ToList();

        return Result<PolicyConsentStatusDto>.Success(new PolicyConsentStatusDto(
            missingRequiredTypes.Count == 0,
            missingRequiredTypes,
            requiredItems));
    }

    private async Task<Result<IReadOnlyCollection<PolicyDocument>>> ResolveAcceptedPoliciesAsync(
        IReadOnlyCollection<PolicyAcceptanceInput>? acceptedPolicies,
        bool requireAllRequiredPolicies,
        CancellationToken cancellationToken)
    {
        if (acceptedPolicies == null || acceptedPolicies.Count == 0)
        {
            return Result<IReadOnlyCollection<PolicyDocument>>.Failure("POLICY_VERSION_INVALID", "Accepted policies are required.");
        }

        var normalizedInputs = new List<(PolicyType PolicyType, string Version)>();

        foreach (var acceptedPolicy in acceptedPolicies)
        {
            if (!PolicyTypeMapper.TryParse(acceptedPolicy.PolicyType, out var policyType))
            {
                return Result<IReadOnlyCollection<PolicyDocument>>.Failure("UNSUPPORTED_POLICY_TYPE", "Policy type is not supported.");
            }

            if (!RequiredPolicyTypes.Contains(policyType))
            {
                return Result<IReadOnlyCollection<PolicyDocument>>.Failure("UNSUPPORTED_POLICY_TYPE", "Policy type is not supported.");
            }

            var version = acceptedPolicy.PolicyVersion?.Trim();
            if (string.IsNullOrWhiteSpace(version))
            {
                return Result<IReadOnlyCollection<PolicyDocument>>.Failure("POLICY_VERSION_INVALID", "Policy version is invalid.");
            }

            normalizedInputs.Add((policyType, version));
        }

        var duplicateType = normalizedInputs
            .GroupBy(input => input.PolicyType)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateType != null)
        {
            return Result<IReadOnlyCollection<PolicyDocument>>.Failure("POLICY_VERSION_INVALID", "Duplicate policy types are not allowed.");
        }

        if (requireAllRequiredPolicies)
        {
            var missingTypes = RequiredPolicyTypes.Except(normalizedInputs.Select(input => input.PolicyType)).ToList();
            if (missingTypes.Count > 0 || normalizedInputs.Count != RequiredPolicyTypes.Length)
            {
                return Result<IReadOnlyCollection<PolicyDocument>>.Failure("POLICY_VERSION_INVALID", "Required policy acceptances are incomplete.");
            }
        }

        var requestedTypes = normalizedInputs.Select(input => input.PolicyType).Distinct().ToList();
        var activeDocuments = await _context.PolicyDocuments
            .AsNoTracking()
            .Where(document => document.IsActive && document.RequiresAcceptance && document.PolicyType.HasValue)
            .Where(document => requestedTypes.Contains(document.PolicyType!.Value))
            .ToListAsync(cancellationToken);

        var resolvedDocuments = new List<PolicyDocument>();

        foreach (var input in normalizedInputs)
        {
            var matchingDocument = activeDocuments.FirstOrDefault(document =>
                document.PolicyType == input.PolicyType &&
                string.Equals(document.Version, input.Version, StringComparison.Ordinal));

            if (matchingDocument == null)
            {
                var hasActiveDocumentForType = activeDocuments.Any(document => document.PolicyType == input.PolicyType);
                var errorCode = hasActiveDocumentForType ? "POLICY_VERSION_INVALID" : "POLICY_DOCUMENT_NOT_FOUND";
                var errorMessage = hasActiveDocumentForType
                    ? "Policy version is not the active version."
                    : "Policy document was not found.";

                return Result<IReadOnlyCollection<PolicyDocument>>.Failure(errorCode, errorMessage);
            }

            resolvedDocuments.Add(matchingDocument);
        }

        return Result<IReadOnlyCollection<PolicyDocument>>.Success(resolvedDocuments);
    }
}
