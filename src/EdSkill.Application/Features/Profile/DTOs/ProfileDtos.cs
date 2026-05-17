using EdSkill.Application.Features.Achievements.DTOs;
using EdSkill.Application.Features.Subscriptions.DTOs;

namespace EdSkill.Application.Features.Profile.DTOs;

public record ProfileSkillDto(
    Guid SkillId,
    string Name,
    string? IconKey
);

public record ProfileDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    DateTime? DateOfBirth,
    string? Phone,
    string? DegreeUrl,
    IReadOnlyCollection<string> CredentialUrls,
    int CredentialCount,
    IReadOnlyCollection<string> SkillsToTeach,
    IReadOnlyCollection<string> SkillsToLearn,
    IReadOnlyCollection<ProfileSkillDto> TeachingSkills,
    IReadOnlyCollection<ProfileSkillDto> LearningSkills,
    IReadOnlyCollection<AchievementSummaryDto> Achievements,
    bool IsPublic,
    IReadOnlyCollection<string> Roles,
    int TotalSessions,
    DateTime? LastActiveAt,
    bool IsCompanionOnboardingComplete,
    IReadOnlyCollection<string> MissingCompanionProfileFields,
    IReadOnlyCollection<ActiveSubscriptionSummaryDto> ActiveSubscriptions,
    ResolvedSubscriptionEntitlementsDto? SubscriptionEntitlements
);

public record AvatarUploadUrlDto(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey,
    DateTime ExpiresAt
);

public record DegreeUploadUrlDto(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey,
    DateTime ExpiresAt
);

public record GenerateAvatarUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize
);

public record GenerateDegreeUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize
);

public sealed class UpdateMyProfileRequest
{
    private string? _displayName;
    private string? _bio;
    private DateTime? _dateOfBirth;
    private string? _phone;
    private string? _degreeUrl;
    private IReadOnlyCollection<string>? _credentialUrls;
    private IReadOnlyCollection<string>? _skillsToTeach;
    private IReadOnlyCollection<string>? _skillsToLearn;
    private string? _avatarUrl;
    private bool? _isPublic;

    public bool HasDisplayName { get; private set; }
    public bool HasBio { get; private set; }
    public bool HasDateOfBirth { get; private set; }
    public bool HasPhone { get; private set; }
    public bool HasDegreeUrl { get; private set; }
    public bool HasCredentialUrls { get; private set; }
    public bool HasSkillsToTeach { get; private set; }
    public bool HasSkillsToLearn { get; private set; }
    public bool HasAvatarUrl { get; private set; }
    public bool HasIsPublic { get; private set; }

    public string? DisplayName
    {
        get => _displayName;
        set
        {
            HasDisplayName = true;
            _displayName = value;
        }
    }

    public string? Bio
    {
        get => _bio;
        set
        {
            HasBio = true;
            _bio = value;
        }
    }

    public DateTime? DateOfBirth
    {
        get => _dateOfBirth;
        set
        {
            HasDateOfBirth = true;
            _dateOfBirth = value;
        }
    }

    public string? Phone
    {
        get => _phone;
        set
        {
            HasPhone = true;
            _phone = value;
        }
    }

    public string? DegreeUrl
    {
        get => _degreeUrl;
        set
        {
            HasDegreeUrl = true;
            _degreeUrl = value;
        }
    }

    public IReadOnlyCollection<string>? CredentialUrls
    {
        get => _credentialUrls;
        set
        {
            HasCredentialUrls = true;
            _credentialUrls = value;
        }
    }

    public IReadOnlyCollection<string>? SkillsToTeach
    {
        get => _skillsToTeach;
        set
        {
            HasSkillsToTeach = true;
            _skillsToTeach = value;
        }
    }

    public IReadOnlyCollection<string>? SkillsToLearn
    {
        get => _skillsToLearn;
        set
        {
            HasSkillsToLearn = true;
            _skillsToLearn = value;
        }
    }

    public string? AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            HasAvatarUrl = true;
            _avatarUrl = value;
        }
    }

    public bool? IsPublic
    {
        get => _isPublic;
        set
        {
            HasIsPublic = true;
            _isPublic = value;
        }
    }
}
