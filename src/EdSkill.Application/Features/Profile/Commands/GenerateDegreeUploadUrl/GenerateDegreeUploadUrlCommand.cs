using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Profile.Commands.GenerateDegreeUploadUrl;

public record GenerateDegreeUploadUrlCommand(
    string FileName,
    string ContentType,
    long FileSize
) : IRequest<Result<DegreeUploadUrlDto>>;
