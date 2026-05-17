namespace EdSkill.Application.Features.Admin.DTOs;

public record AdminPointPackageDto(
    Guid PackageId,
    string Code,
    string Name,
    string? Description,
    int Points,
    int BonusPoints,
    int TotalPoints,
    int PriceVnd,
    string Currency,
    string? BadgeText,
    bool IsHighlighted,
    int DisplayOrder,
    bool IsActive,
    bool IsDeleted,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreatePointPackageRequest(
    string Code,
    string Name,
    string? Description,
    int Points,
    int BonusPoints,
    int PriceVnd,
    string? BadgeText,
    bool IsHighlighted,
    int DisplayOrder,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt
);

public sealed class UpdatePointPackageRequest
{
    private string? _code;
    private string? _name;
    private string? _description;
    private int? _points;
    private int? _bonusPoints;
    private int? _priceVnd;
    private string? _badgeText;
    private bool? _isHighlighted;
    private int? _displayOrder;
    private bool? _isActive;
    private DateTime? _startsAt;
    private DateTime? _endsAt;

    public bool HasCode { get; private set; }
    public bool HasName { get; private set; }
    public bool HasDescription { get; private set; }
    public bool HasPoints { get; private set; }
    public bool HasBonusPoints { get; private set; }
    public bool HasPriceVnd { get; private set; }
    public bool HasBadgeText { get; private set; }
    public bool HasIsHighlighted { get; private set; }
    public bool HasDisplayOrder { get; private set; }
    public bool HasIsActive { get; private set; }
    public bool HasStartsAt { get; private set; }
    public bool HasEndsAt { get; private set; }

    public string? Code
    {
        get => _code;
        set
        {
            HasCode = true;
            _code = value;
        }
    }

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

    public int? Points
    {
        get => _points;
        set
        {
            HasPoints = true;
            _points = value;
        }
    }

    public int? BonusPoints
    {
        get => _bonusPoints;
        set
        {
            HasBonusPoints = true;
            _bonusPoints = value;
        }
    }

    public int? PriceVnd
    {
        get => _priceVnd;
        set
        {
            HasPriceVnd = true;
            _priceVnd = value;
        }
    }

    public string? BadgeText
    {
        get => _badgeText;
        set
        {
            HasBadgeText = true;
            _badgeText = value;
        }
    }

    public bool? IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            HasIsHighlighted = true;
            _isHighlighted = value;
        }
    }

    public int? DisplayOrder
    {
        get => _displayOrder;
        set
        {
            HasDisplayOrder = true;
            _displayOrder = value;
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

    public DateTime? StartsAt
    {
        get => _startsAt;
        set
        {
            HasStartsAt = true;
            _startsAt = value;
        }
    }

    public DateTime? EndsAt
    {
        get => _endsAt;
        set
        {
            HasEndsAt = true;
            _endsAt = value;
        }
    }
}
