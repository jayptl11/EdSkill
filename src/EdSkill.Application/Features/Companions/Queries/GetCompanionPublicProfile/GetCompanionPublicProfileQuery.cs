using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Companions.Queries.GetCompanionPublicProfile;

public record GetCompanionPublicProfileQuery(Guid CompanionId) : IRequest<Result<CompanionPublicProfileDto>>;
