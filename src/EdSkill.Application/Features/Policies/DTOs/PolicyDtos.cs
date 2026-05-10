using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Features.Policies.DTOs;

public record PolicyDocumentSummaryDto(
    string Slug,
    string Category,
    string Audience,
    string? PolicyType,
    string Version,
    string Title,
    string Summary,
    bool RequiresAcceptance,
    DateTime EffectiveAt
);

public record PolicyDocumentDetailDto(
    string Slug,
    string Category,
    string Audience,
    string? PolicyType,
    string Version,
    string Title,
    string Summary,
    string ContentMarkdown,
    bool RequiresAcceptance,
    DateTime EffectiveAt
);

public record PolicyConsentItemDto(
    string PolicyType,
    string Slug,
    string Title,
    string RequiredVersion,
    string? AcceptedVersion,
    DateTime? AcceptedAt,
    bool IsAcceptedLatest
);

public record PolicyConsentStatusDto(
    bool IsUpToDate,
    IReadOnlyCollection<string> MissingRequiredTypes,
    IReadOnlyCollection<PolicyConsentItemDto> RequiredPolicies
);

public record AcceptPoliciesRequest(
    IReadOnlyCollection<PolicyAcceptanceInput>? AcceptedPolicies
);
