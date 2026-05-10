using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Profile.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;
