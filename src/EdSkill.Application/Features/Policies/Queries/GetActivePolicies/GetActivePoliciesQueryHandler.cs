using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Policies.Queries.GetActivePolicies;

public class GetActivePoliciesQueryHandler : IRequestHandler<GetActivePoliciesQuery, Result<IReadOnlyCollection<PolicyDocumentSummaryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetActivePoliciesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<PolicyDocumentSummaryDto>>> Handle(GetActivePoliciesQuery request, CancellationToken cancellationToken)
    {
        var documents = await _context.PolicyDocuments
            .AsNoTracking()
            .Where(document => document.IsActive)
            .OrderBy(document => document.Category)
            .ThenBy(document => document.Title)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<PolicyDocumentSummaryDto>>.Success(
            documents.Select(PolicyDtoMapper.MapSummary).ToList());
    }
}
