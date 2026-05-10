using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Profile.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<ProfileDto>>;
