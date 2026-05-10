using EdSkill.Application.Features.Profile.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.Profile;

internal static class ProfileDtoMapper
{
    public static ProfileDto Map(User user, UserProfile profile)
    {
        return new ProfileDto(
            user.UserId,
            profile.DisplayName,
            profile.AvatarUrl,
            profile.Bio,
            profile.University,
            profile.Faculty,
            profile.YearOfStudy,
            profile.SkillsToTeach.AsReadOnly(),
            profile.SkillsToLearn.AsReadOnly(),
            profile.IsPublic,
            user.Roles.AsReadOnly(),
            profile.TotalSessions,
            profile.LastActiveAt);
    }
}
