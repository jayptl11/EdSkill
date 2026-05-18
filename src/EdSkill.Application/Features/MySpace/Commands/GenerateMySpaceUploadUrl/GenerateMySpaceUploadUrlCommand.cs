using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Commands.GenerateMySpaceUploadUrl;

public enum MySpaceUploadKind
{
    Cover = 0,
    Credential = 1
}

public record GenerateMySpaceUploadUrlCommand(
    MySpaceUploadKind Kind,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<MySpaceUploadUrlDto>>;
