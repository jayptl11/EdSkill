using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Skills.Commands.DeleteSkill;

public class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteSkillCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.SkillId == request.SkillId, cancellationToken);

        if (skill is null || skill.IsDeleted)
        {
            return Result.Failure("SKILL_NOT_FOUND", "The specified skill does not exist.");
        }

        if (!skill.IsDeleted)
        {
            skill.IsDeleted = true;
            skill.IsActive = false;
            skill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
