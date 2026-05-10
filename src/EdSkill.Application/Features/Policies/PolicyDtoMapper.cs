 using EdSkill.Application.Common.Policies;
using EdSkill.Application.Features.Policies.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Policies;

public static class PolicyDtoMapper
{
    public static PolicyDocumentSummaryDto MapSummary(PolicyDocument document)
        => new(
            document.Slug,
            document.Category,
            document.Audience,
            document.PolicyType.HasValue ? PolicyTypeMapper.ToApiValue(document.PolicyType.Value) : null,
            document.Version,
            document.Title,
            document.Summary,
            document.RequiresAcceptance,
            document.EffectiveAt);

    public static PolicyDocumentDetailDto MapDetail(PolicyDocument document)
        => new(
            document.Slug,
            document.Category,
            document.Audience,
            document.PolicyType.HasValue ? PolicyTypeMapper.ToApiValue(document.PolicyType.Value) : null,
            document.Version,
            document.Title,
            document.Summary,
            document.ContentMarkdown,
            document.RequiresAcceptance,
            document.EffectiveAt);
}
