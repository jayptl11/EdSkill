namespace EdSkill.Application.Features.Profile.DTOs;

public record ProfileDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? University,
    string? Faculty,
    int? YearOfStudy,
    IReadOnlyCollection<string> SkillsToTeach,
    IReadOnlyCollection<string> SkillsToLearn,
    bool IsPublic,
    IReadOnlyCollection<string> Roles,
    int TotalSessions,
    DateTime? LastActiveAt,
    bool IsCompanionOnboardingComplete,
    IReadOnlyCollection<string> MissingCompanionProfileFields
);

public record AvatarUploadUrlDto(
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

public sealed class UpdateMyProfileRequest
{
    private string? _displayName;
    private string? _bio;
    private string? _university;
    private string? _faculty;
    private int? _yearOfStudy;
    private IReadOnlyCollection<string>? _skillsToTeach;
    private IReadOnlyCollection<string>? _skillsToLearn;
    private string? _avatarUrl;
    private bool? _isPublic;

    public bool HasDisplayName { get; private set; }
    public bool HasBio { get; private set; }
    public bool HasUniversity { get; private set; }
    public bool HasFaculty { get; private set; }
    public bool HasYearOfStudy { get; private set; }
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

    public string? University
    {
        get => _university;
        set
        {
            HasUniversity = true;
            _university = value;
        }
    }

    public string? Faculty
    {
        get => _faculty;
        set
        {
            HasFaculty = true;
            _faculty = value;
        }
    }

    public int? YearOfStudy
    {
        get => _yearOfStudy;
        set
        {
            HasYearOfStudy = true;
            _yearOfStudy = value;
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
