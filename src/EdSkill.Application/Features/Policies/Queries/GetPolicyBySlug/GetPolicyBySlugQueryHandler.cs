using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Policies.Queries.GetPolicyBySlug;

public class GetPolicyBySlugQueryHandler : IRequestHandler<GetPolicyBySlugQuery, Result<PolicyDocumentDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPolicyBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PolicyDocumentDetailDto>> Handle(GetPolicyBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var document = await _context.PolicyDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(policy => policy.IsActive && policy.Slug == slug, cancellationToken);

        if (document == null)
        {
            return Result<PolicyDocumentDetailDto>.Failure("POLICY_DOCUMENT_NOT_FOUND", "Policy document was not found.");
        }

        return Result<PolicyDocumentDetailDto>.Success(PolicyDtoMapper.MapDetail(document));
    }
}
