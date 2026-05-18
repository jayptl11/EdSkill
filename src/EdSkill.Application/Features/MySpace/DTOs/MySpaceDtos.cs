using EdSkill.Domain.Enums;

namespace EdSkill.Application.Features.MySpace.DTOs;

public record MySpaceSkillDto(
    Guid SkillId,
    string Name,
    string? IconKey
);

public record CompanionSpaceCardDto(
    Guid CompanionSpaceCardId,
    MySpaceSkillDto Skill,
    string Title,
    string? Description,
    int PricePoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string> Languages,
    string? CoverImageUrl,
    IReadOnlyCollection<string> CredentialUrls,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record LearnerSpaceCardDto(
    Guid LearnerSpaceCardId,
    MySpaceSkillDto Skill,
    string Title,
    string? Description,
    int TargetPoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string> Languages,
    string? CoverImageUrl,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record MySpaceDto(
    IReadOnlyCollection<CompanionSpaceCardDto> CompanionCards,
    IReadOnlyCollection<LearnerSpaceCardDto> LearnerCards
);

public record MySpaceUploadUrlDto(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey,
    DateTime ExpiresAt
);

public record GenerateMySpaceUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize
);

public record CreateCompanionSpaceCardRequest(
    Guid SkillId,
    string Title,
    string? Description,
    int PricePoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string>? Languages,
    string? CoverImageUrl,
    IReadOnlyCollection<string>? CredentialUrls,
    bool IsPublished
);

public sealed class UpdateCompanionSpaceCardRequest
{
    private Guid? _skillId;
    private string? _title;
    private string? _description;
    private int? _pricePoints;
    private int? _durationMinutes;
    private IReadOnlyCollection<SessionDeliveryMode>? _deliveryModes;
    private IReadOnlyCollection<string>? _languages;
    private string? _coverImageUrl;
    private IReadOnlyCollection<string>? _credentialUrls;
    private bool? _isPublished;

    public bool HasSkillId { get; private set; }
    public bool HasTitle { get; private set; }
    public bool HasDescription { get; private set; }
    public bool HasPricePoints { get; private set; }
    public bool HasDurationMinutes { get; private set; }
    public bool HasDeliveryModes { get; private set; }
    public bool HasLanguages { get; private set; }
    public bool HasCoverImageUrl { get; private set; }
    public bool HasCredentialUrls { get; private set; }
    public bool HasIsPublished { get; private set; }

    public Guid? SkillId
    {
        get => _skillId;
        set
        {
            HasSkillId = true;
            _skillId = value;
        }
    }

    public string? Title
    {
        get => _title;
        set
        {
            HasTitle = true;
            _title = value;
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            HasDescription = true;
            _description = value;
        }
    }

    public int? PricePoints
    {
        get => _pricePoints;
        set
        {
            HasPricePoints = true;
            _pricePoints = value;
        }
    }

    public int? DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            HasDurationMinutes = true;
            _durationMinutes = value;
        }
    }

    public IReadOnlyCollection<SessionDeliveryMode>? DeliveryModes
    {
        get => _deliveryModes;
        set
        {
            HasDeliveryModes = true;
            _deliveryModes = value;
        }
    }

    public IReadOnlyCollection<string>? Languages
    {
        get => _languages;
        set
        {
            HasLanguages = true;
            _languages = value;
        }
    }

    public string? CoverImageUrl
    {
        get => _coverImageUrl;
        set
        {
            HasCoverImageUrl = true;
            _coverImageUrl = value;
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

    public bool? IsPublished
    {
        get => _isPublished;
        set
        {
            HasIsPublished = true;
            _isPublished = value;
        }
    }
}

public record CreateLearnerSpaceCardRequest(
    Guid SkillId,
    string Title,
    string? Description,
    int TargetPoints,
    int DurationMinutes,
    IReadOnlyCollection<SessionDeliveryMode> DeliveryModes,
    IReadOnlyCollection<string>? Languages,
    string? CoverImageUrl,
    bool IsPublished
);

public sealed class UpdateLearnerSpaceCardRequest
{
    private Guid? _skillId;
    private string? _title;
    private string? _description;
    private int? _targetPoints;
    private int? _durationMinutes;
    private IReadOnlyCollection<SessionDeliveryMode>? _deliveryModes;
    private IReadOnlyCollection<string>? _languages;
    private string? _coverImageUrl;
    private bool? _isPublished;

    public bool HasSkillId { get; private set; }
    public bool HasTitle { get; private set; }
    public bool HasDescription { get; private set; }
    public bool HasTargetPoints { get; private set; }
    public bool HasDurationMinutes { get; private set; }
    public bool HasDeliveryModes { get; private set; }
    public bool HasLanguages { get; private set; }
    public bool HasCoverImageUrl { get; private set; }
    public bool HasIsPublished { get; private set; }

    public Guid? SkillId
    {
        get => _skillId;
        set
        {
            HasSkillId = true;
            _skillId = value;
        }
    }

    public string? Title
    {
        get => _title;
        set
        {
            HasTitle = true;
            _title = value;
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            HasDescription = true;
            _description = value;
        }
    }

    public int? TargetPoints
    {
        get => _targetPoints;
        set
        {
            HasTargetPoints = true;
            _targetPoints = value;
        }
    }

    public int? DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            HasDurationMinutes = true;
            _durationMinutes = value;
        }
    }

    public IReadOnlyCollection<SessionDeliveryMode>? DeliveryModes
    {
        get => _deliveryModes;
        set
        {
            HasDeliveryModes = true;
            _deliveryModes = value;
        }
    }

    public IReadOnlyCollection<string>? Languages
    {
        get => _languages;
        set
        {
            HasLanguages = true;
            _languages = value;
        }
    }

    public string? CoverImageUrl
    {
        get => _coverImageUrl;
        set
        {
            HasCoverImageUrl = true;
            _coverImageUrl = value;
        }
    }

    public bool? IsPublished
    {
        get => _isPublished;
        set
        {
            HasIsPublished = true;
            _isPublished = value;
        }
    }
}
