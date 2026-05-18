using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.MySpace.DTOs;
using MediatR;

namespace EdSkill.Application.Features.MySpace.Queries.GetMySpace;

public record GetMySpaceQuery : IRequest<Result<MySpaceDto>>;
