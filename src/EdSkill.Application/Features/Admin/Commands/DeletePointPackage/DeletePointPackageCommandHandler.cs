using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdSkill.Application.Features.Admin.Commands.DeletePointPackage;

public class DeletePointPackageCommandHandler : IRequestHandler<DeletePointPackageCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeletePointPackageCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeletePointPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await _context.PointPackages
            .FirstOrDefaultAsync(item => item.PointPackageId == request.PackageId, cancellationToken);

        if (package == null)
        {
            return Result.Failure("POINT_PACKAGE_NOT_FOUND", "Point package was not found.");
        }

        if (!package.IsDeleted)
        {
            package.IsDeleted = true;
            package.IsActive = false;
            package.UpdatedAt = _dateTimeProvider.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
