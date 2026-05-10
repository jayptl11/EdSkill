using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin;
using EdSkill.Application.Features.Admin.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Queries.GetSystemConfigs;

public class GetSystemConfigsQueryHandler : IRequestHandler<GetSystemConfigsQuery, Result<IReadOnlyCollection<SystemConfigDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetSystemConfigsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<SystemConfigDto>>> Handle(GetSystemConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _context.SystemConfigs
            .AsNoTracking()
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<SystemConfigDto>>.Success(configs.Select(SystemConfigDtoMapper.Map).ToList());
    }
}
