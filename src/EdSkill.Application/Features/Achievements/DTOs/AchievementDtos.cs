namespace EdSkill.Application.Features.Achievements.DTOs;

public record AchievementSummaryDto(
    Guid AchievementId,
    string Name,
    string Description,
    string? IconUrl,
    DateTime AwardedAt
);

public record AdminAchievementDto(
    Guid AchievementId,
    string Name,
    string Description,
    string? IconUrl,
    string Track,
    string Metric,
    int Threshold,
    int SortOrder,
    bool IsActive,
    DateTime EffectiveFromUtc
);

public record AchievementIconUploadUrlDto(
    string UploadUrl,
    string PublicUrl,
    string ObjectKey,
    DateTime ExpiresAt
);

public record CreateAchievementRequest(
    string Name,
    string Description,
    string? IconUrl,
    string Track,
    string Metric,
    int Threshold,
    int SortOrder
);

public sealed class UpdateAchievementRequest
{
    private string? _name;
    private string? _description;
    private string? _iconUrl;
    private string? _track;
    private string? _metric;
    private int? _threshold;
    private int? _sortOrder;
    private bool? _isActive;

    public bool HasName { get; private set; }
    public bool HasDescription { get; private set; }
    public bool HasIconUrl { get; private set; }
    public bool HasTrack { get; private set; }
    public bool HasMetric { get; private set; }
    public bool HasThreshold { get; private set; }
    public bool HasSortOrder { get; private set; }
    public bool HasIsActive { get; private set; }

    public string? Name
    {
        get => _name;
        set
        {
            HasName = true;
            _name = value;
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

    public string? IconUrl
    {
        get => _iconUrl;
        set
        {
            HasIconUrl = true;
            _iconUrl = value;
        }
    }

    public string? Track
    {
        get => _track;
        set
        {
            HasTrack = true;
            _track = value;
        }
    }

    public string? Metric
    {
        get => _metric;
        set
        {
            HasMetric = true;
            _metric = value;
        }
    }

    public int? Threshold
    {
        get => _threshold;
        set
        {
            HasThreshold = true;
            _threshold = value;
        }
    }

    public int? SortOrder
    {
        get => _sortOrder;
        set
        {
            HasSortOrder = true;
            _sortOrder = value;
        }
    }

    public bool? IsActive
    {
        get => _isActive;
        set
        {
            HasIsActive = true;
            _isActive = value;
        }
    }
}

public record GenerateAchievementIconUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize
);
