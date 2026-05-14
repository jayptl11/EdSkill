using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Companions.Queries.GetCompanionDetail;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Companions;

public class GetCompanionDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanionIsPublic_ReturnsReviewsAndTrimmedMatchedOnlineOffers()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var reviewSessionId = Guid.NewGuid();

        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 2, displayName: "Companion One"),
            new()
            {
                UserId = learnerId,
                Username = "learner1",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = learnerId,
                    DisplayName = "Learner One",
                    IsPublic = true
                }
            }
        };

        var sessions = new List<Session>
        {
            CreateFormulaSession(companionId, skillId, "Speaking", 120, DateTime.UtcNow.AddDays(1)),
            CreateFormulaSession(companionId, skillId, "Speaking", 120, DateTime.UtcNow.AddDays(2), SessionDeliveryMode.Offline),
            new()
            {
                SessionId = reviewSessionId,
                CompanionId = companionId,
                LearnerId = learnerId,
                SkillId = skillId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddDays(-1),
                Status = SessionStatus.Completed
            }
        };

        var reviews = new List<Review>
        {
            new()
            {
                ReviewId = Guid.NewGuid(),
                SessionId = reviewSessionId,
                ReviewerId = learnerId,
                RevieweeId = companionId,
                Rating = 4,
                Comment = "Helpful"
            }
        };

        var handler = CreateHandler(users, sessions, new[] { skill }, reviews);

        var result = await handler.Handle(
            new GetCompanionDetailQuery(companionId, skillId, 60, 500, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CredentialCount.Should().Be(2);
        result.Value.AvgRating.Should().Be(4);
        result.Value.TotalReviews.Should().Be(1);
        result.Value.Reviews.Data.Should().ContainSingle();
        result.Value.Sessions.Should().ContainSingle();
        result.Value.Sessions.Single().DeliveryMode.Should().Be(SessionDeliveryMode.Online);
        result.Value.Sessions.Single().DurationOptions.Should().BeEquivalentTo(new[] { 60, 90, 120 }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Handle_WhenLegacyOfferMatches_ReturnsSingleLegacyOffer()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();

        var skill = CreateSkill(skillId, "Speaking");
        var users = new List<User>
        {
            CreateCompanion(companionId, skill, credentialCount: 0, displayName: "Companion One")
        };

        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = skillId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 90,
                PointCost = 140,
                PricingModel = SessionPricingModel.LegacyManual,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            }
        };

        var handler = CreateHandler(users, sessions, new[] { skill }, Array.Empty<Review>());

        var result = await handler.Handle(
            new GetCompanionDetailQuery(companionId, skillId, 60, 150, null, null, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sessions.Should().ContainSingle();
        result.Value.Sessions.Single().PricingModel.Should().Be(SessionPricingModel.LegacyManual);
        result.Value.Sessions.Single().PointCost.Should().Be(140);
        result.Value.Sessions.Single().DurationMinutes.Should().Be(90);
    }

    private static GetCompanionDetailQueryHandler CreateHandler(
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<Session> sessions,
        IReadOnlyCollection<Skill> skills,
        IReadOnlyCollection<Review> reviews)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);

        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock
            .Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        return new GetCompanionDetailQueryHandler(contextMock.Object, sessionPricingServiceMock.Object);
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
                Bio = "Bio",
                AvatarUrl = "https://cdn.edskill.test/u/1.png",
                IsPublic = true,
                TotalSessions = 3,
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
