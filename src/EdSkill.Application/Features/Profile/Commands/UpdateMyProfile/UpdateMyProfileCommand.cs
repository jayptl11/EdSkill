using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Profile.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    bool HasDisplayName,
    string? DisplayName,
    bool HasBio,
    string? Bio,
    bool HasUniversity,
    string? University,
    bool HasFaculty,
    string? Faculty,
    bool HasYearOfStudy,
    int? YearOfStudy,
    bool HasSkillsToTeach,
    IReadOnlyCollection<string>? SkillsToTeach,
    bool HasSkillsToLearn,
    IReadOnlyCollection<string>? SkillsToLearn,
    bool HasAvatarUrl,
    string? AvatarUrl,
    bool HasIsPublic,
    bool? IsPublic
) : IRequest<Result<ProfileDto>>;
