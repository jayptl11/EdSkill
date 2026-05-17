using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.DeletePointPackage;

public record DeletePointPackageCommand(Guid PackageId) : IRequest<Result>;
