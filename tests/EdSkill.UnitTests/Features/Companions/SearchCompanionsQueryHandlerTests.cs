using EdSkill.Application.Common.Interfaces;
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
        result.Value.Data.Single().MatchedOffers.Should().ContainSingle();
        result.Value.Data.Single().MatchedOffers.Single().DeliveryMode.Should().Be(SessionDeliveryMode.Online);
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

        var companion = result.Value.Data.Single();
        companion.MatchingSessionCount.Should().Be(1);
        companion.LowestPointCost.Should().Be(219);
        companion.PricingPreview.MinLearnerChargePoints.Should().Be(219);
        companion.PricingPreview.MaxLearnerChargePoints.Should().Be(269);
        companion.MatchedOffers.Should().ContainSingle();
        companion.MatchedOffers.Single().DurationOptions.Should().BeEquivalentTo(new[] { 60, 90 }, options => options.WithStrictOrdering());
        companion.MatchedOffers.Single().DurationPricingOptions.Select(item => item.LearnerChargePoints)
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

    private static SearchCompanionsQueryHandler CreateHandler(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Session> sessions,
        IReadOnlyCollection<Skill> skills,
        Guid? currentUserId = null)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(Array.Empty<Review>().BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.TryGetUserId()).Returns(currentUserId);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock
            .Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        return new SearchCompanionsQueryHandler(contextMock.Object, currentUserServiceMock.Object, sessionPricingServiceMock.Object);
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
        SessionDeliveryMode deliveryMode = SessionDeliveryMode.Online)
    {
        return new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            SkillId = skillId,
            Skill = skillName,
            DeliveryMode = deliveryMode,
            DurationMinutes = maxDurationMinutes,
            DurationOptions = new List<int> { maxDurationMinutes },
            PricingModel = SessionPricingModel.FormulaV1,
            ScheduledAt = scheduledAt,
            Status = SessionStatus.Available
        };
    }
}
