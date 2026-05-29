using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.Queries.SearchCompanions;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Companions;

public class SearchCompanionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenSearchingDefault_ReturnsOnlyOnlineOffersAndExcludesCurrentUser()
    {
        var skillId = Guid.NewGuid();
        var currentCompanionId = Guid.NewGuid();
        var returnedCompanionId = Guid.NewGuid();
        var hiddenOfflineCompanionId = Guid.NewGuid();

        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(currentCompanionId, skill, credentialCount: 1, displayName: "Current Companion"),
            CreateCompanion(returnedCompanionId, skill, credentialCount: 2, displayName: "Returned Companion"),
            CreateCompanion(hiddenOfflineCompanionId, skill, credentialCount: 0, displayName: "Offline Companion")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(currentCompanionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(1)),
            CreateFormulaSession(returnedCompanionId, skillId, "Speaking", 90, DateTime.UtcNow.AddDays(2)),
            CreateFormulaSession(hiddenOfflineCompanionId, skillId, "Speaking", 120, DateTime.UtcNow.AddDays(3), deliveryMode: SessionDeliveryMode.Offline)
        };

        var handler = CreateHandler(users, sessions, new[] { skill }, currentCompanionId);

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Data.Should().ContainSingle();
        result.Value.Data.Single().CompanionId.Should().Be(returnedCompanionId);
        result.Value.Data.Single().Offer.DeliveryMode.Should().Be(SessionDeliveryMode.Online);
    }

    [Fact]
    public async Task Handle_WhenDurationAndPriceFiltersProvided_TrimsMatchedDurationsWithinOffer()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, skillId, "Speaking", 120, DateTime.UtcNow.AddDays(1)),
            CreateFormulaSession(companionId, skillId, "Speaking", 45, DateTime.UtcNow.AddDays(2))
        };

        var handler = CreateHandler(users, sessions, new[] { skill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, 60, 270, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().ContainSingle();

        var classItem = result.Value.Data.Single();
        classItem.Offer.PointCost.Should().Be(219);
        classItem.Offer.PricingPreview.MinLearnerChargePoints.Should().Be(219);
        classItem.Offer.PricingPreview.MaxLearnerChargePoints.Should().Be(269);
        classItem.Offer.DurationOptions.Should().BeEquivalentTo(new[] { 60, 90 }, options => options.WithStrictOrdering());
        classItem.Offer.DurationPricingOptions.Select(item => item.LearnerChargePoints)
            .Should().BeEquivalentTo(new[] { 219, 269 }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_WhenPriceFilterApplied_UsesLearnerChargeInsteadOfCompanionPayout()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(1))
        };

        var handler = CreateHandler(users, sessions, new[] { skill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, null, 160, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
    }

    [Theory]
    [InlineData("Two", 2)]
    [InlineData("ThreeOrMore", 3)]
    public async Task Handle_WhenCredentialCountGroupProvided_ReturnsMatchingCompanionOnly(string credentialCountGroup, int expectedCredentialCount)
    {
        var skillId = Guid.NewGuid();
        var zeroCredentialCompanionId = Guid.NewGuid();
        var twoCredentialCompanionId = Guid.NewGuid();
        var threeCredentialCompanionId = Guid.NewGuid();

        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(zeroCredentialCompanionId, skill, credentialCount: 0, displayName: "Zero"),
            CreateCompanion(twoCredentialCompanionId, skill, credentialCount: 2, displayName: "Two"),
            CreateCompanion(threeCredentialCompanionId, skill, credentialCount: 3, displayName: "Three")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(zeroCredentialCompanionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(1)),
            CreateFormulaSession(twoCredentialCompanionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(2)),
            CreateFormulaSession(threeCredentialCompanionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(3))
        };

        var handler = CreateHandler(users, sessions, new[] { skill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, null, null, credentialCountGroup, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().ContainSingle();
        result.Value.Data.Single().CredentialCount.Should().Be(expectedCredentialCount);
    }

    [Fact]
    public async Task Handle_WhenCompanionHasMultipleMatchedOffers_ReturnsSeparateClassCards()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, skillId, "Speaking", 60, DateTime.UtcNow.AddDays(1), description: "Class One"),
            CreateFormulaSession(companionId, skillId, "Speaking", 90, DateTime.UtcNow.AddDays(2), description: "Class Two")
        };

        var handler = CreateHandler(users, sessions, new[] { skill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Data.Select(item => item.Offer.Description)
            .Should().BeEquivalentTo(new[] { "Class One", "Class Two" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_WhenCompanionHasOtherSkillOffer_DoesNotUseItToPassFilters()
    {
        var searchedSkillId = Guid.NewGuid();
        var otherSkillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();

        var searchedSkill = CreateSkill(searchedSkillId, "Speaking");
        var otherSkill = CreateSkill(otherSkillId, "Canva");
        var users = new List<User>
        {
            CreateCompanion(companionId, searchedSkill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, searchedSkillId, "Speaking", 30, DateTime.UtcNow.AddDays(1)),
            CreateFormulaSession(companionId, otherSkillId, "Canva", 120, DateTime.UtcNow.AddDays(2))
        };

        var handler = CreateHandler(users, sessions, new[] { searchedSkill, otherSkill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(searchedSkillId, 60, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCompanionNoLongerOwnsSearchedSkill_ExcludesCompanion()
    {
        var searchedSkillId = Guid.NewGuid();
        var currentSkillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();

        var searchedSkill = CreateSkill(searchedSkillId, "Speaking");
        var currentSkill = CreateSkill(currentSkillId, "Canva");
        var users = new List<User>
        {
            CreateCompanion(companionId, currentSkill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, searchedSkillId, "Speaking", 60, DateTime.UtcNow.AddDays(1))
        };

        var handler = CreateHandler(users, sessions, new[] { searchedSkill, currentSkill });

        var result = await handler.Handle(
            new SearchCompanionsQuery(searchedSkillId, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenAvailableOfferIsInThePast_ExcludesCompanion()
    {
        var now = new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc);
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 1, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, skillId, "Speaking", 60, now.AddDays(-2))
        };

        var handler = CreateHandler(users, sessions, new[] { skill }, now: now);

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSkillFilterMissing_ReturnsAllSearchableProfilesOrderedByNewestOffer()
    {
        var now = new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc);
        var firstSkillId = Guid.NewGuid();
        var secondSkillId = Guid.NewGuid();
        var olderCompanionId = Guid.NewGuid();
        var newerCompanionId = Guid.NewGuid();

        var firstSkill = CreateSkill(firstSkillId, "Speaking");
        var secondSkill = CreateSkill(secondSkillId, "Canva");
        var users = new List<User>
        {
            CreateCompanion(olderCompanionId, firstSkill, credentialCount: 1, displayName: "Older Companion"),
            CreateCompanion(newerCompanionId, secondSkill, credentialCount: 2, displayName: "Newer Companion")
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(
                olderCompanionId,
                firstSkillId,
                "Speaking",
                60,
                now.AddDays(2),
                createdAt: now.AddDays(-3)),
            CreateFormulaSession(
                newerCompanionId,
                secondSkillId,
                "Canva",
                90,
                now.AddDays(1),
                createdAt: now.AddHours(-2))
        };

        var handler = CreateHandler(users, sessions, new[] { firstSkill, secondSkill }, now: now);

        var result = await handler.Handle(
            new SearchCompanionsQuery(null, null, null, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Data.Select(item => item.Offer.Skill)
            .Should().BeEquivalentTo(new[] { "Canva", "Speaking" }, options => options.WithStrictOrdering());
    }

    private static SearchCompanionsQueryHandler CreateHandler(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Session> sessions,
        IReadOnlyCollection<Skill> skills,
        Guid? currentUserId = null,
        DateTime? now = null)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(Array.Empty<Review>().BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.TryGetUserId()).Returns(currentUserId);
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now ?? new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc));

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock
            .Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        var subscriptionEntitlementServiceMock = new Mock<ISubscriptionEntitlementService>();
        subscriptionEntitlementServiceMock
            .Setup(x => x.GetResolvedEntitlementsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ResolvedSubscriptionEntitlements>());

        return new SearchCompanionsQueryHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            sessionPricingServiceMock.Object,
            subscriptionEntitlementServiceMock.Object);
    }

    private static Skill CreateSkill(Guid skillId, string name)
    {
        return new Skill
        {
            SkillId = skillId,
            Name = name,
            Slug = name.ToLowerInvariant(),
            BasePointCost = 100,
            IsActive = true
        };
    }

    private static User CreateCompanion(Guid companionId, Skill skill, int credentialCount, string displayName)
    {
        return new User
        {
            UserId = companionId,
            Username = displayName.Replace(" ", string.Empty).ToLowerInvariant(),
            Roles = new List<string> { "learner", "companion" },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = companionId,
                DisplayName = displayName,
                IsPublic = true,
                CredentialUrls = Enumerable.Range(1, credentialCount)
                    .Select(index => $"https://cdn.edskill.test/{companionId}/credential-{index}.pdf")
                    .ToList()
            },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = companionId,
                    SkillId = skill.SkillId,
                    Skill = skill,
                    Type = UserSkillType.Teach
                }
            }
        };
    }

    private static Session CreateFormulaSession(
        Guid companionId,
        Guid skillId,
        string skillName,
        int maxDurationMinutes,
        DateTime scheduledAt,
        SessionDeliveryMode deliveryMode = SessionDeliveryMode.Online,
        DateTime? createdAt = null,
        string? description = null)
    {
        var createdAtValue = createdAt ?? scheduledAt.AddDays(-1);

        return new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            SkillId = skillId,
            Skill = skillName,
            Description = description,
            DeliveryMode = deliveryMode,
            DurationMinutes = maxDurationMinutes,
            DurationOptions = new List<int> { maxDurationMinutes },
            PricingModel = SessionPricingModel.FormulaV1,
            ScheduledAt = scheduledAt,
            Status = SessionStatus.Available,
            CreatedAt = createdAtValue,
            UpdatedAt = createdAtValue
        };
    }
}
