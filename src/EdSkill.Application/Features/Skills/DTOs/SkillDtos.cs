namespace EdSkill.Application.Features.Skills.DTOs;

public record SkillDto(
    Guid Id,
    string Name,
    string Slug,
    string? Category,
    string? IconKey
);

public record AdminSkillDto(
    Guid Id,
    string Name,
    string Slug,
    string? Category,
    string? IconKey,
    int BasePointCost,
    IReadOnlyCollection<string> Aliases,
    bool IsActive
);

public record CreateSkillRequest(
    string Name,
    string? Slug,
    string? Category,
    string? IconKey,
    int BasePointCost,
    IReadOnlyCollection<string>? Aliases
);

public sealed class UpdateSkillRequest
{
    private string? _name;
    private string? _slug;
    private string? _category;
    private string? _iconKey;
    private int? _basePointCost;
    private IReadOnlyCollection<string>? _aliases;
    private bool? _isActive;

    public bool HasName { get; private set; }
    public bool HasSlug { get; private set; }
    public bool HasCategory { get; private set; }
    public bool HasIconKey { get; private set; }
    public bool HasBasePointCost { get; private set; }
    public bool HasAliases { get; private set; }
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

    public string? Slug
    {
        get => _slug;
        set
        {
            HasSlug = true;
            _slug = value;
        }
    }

    public string? Category
    {
        get => _category;
        set
        {
            HasCategory = true;
            _category = value;
        }
    }

    public string? IconKey
    {
        get => _iconKey;
        set
        {
            HasIconKey = true;
            _iconKey = value;
        }
    }

    public int? BasePointCost
    {
        get => _basePointCost;
        set
        {
            HasBasePointCost = true;
            _basePointCost = value;
        }
    }

    public IReadOnlyCollection<string>? Aliases
    {
        get => _aliases;
        set
        {
            HasAliases = true;
            _aliases = value;
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
