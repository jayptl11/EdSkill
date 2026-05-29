using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Companions.Queries.GetCompanionPublicProfile;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Features.Profile;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Companions;

public class GetCompanionPublicProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanionHasTeachingSkills_ReturnsAllSkillsIncludingWithoutOffers()
    {
        var now = new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var skillWithOfferId = Guid.NewGuid();
        var skillWithoutOfferId = Guid.NewGuid();
        var formulaSkill = new Skill
        {
            SkillId = skillWithOfferId,
            Name = "Speaking",
            Slug = "speaking",
            IconKey = "languages",
            BasePointCost = 100,
            IsActive = true
        };
        var hiddenOfferSkill = new Skill
        {
            SkillId = skillWithoutOfferId,
            Name = "React",
            Slug = "react",
            IconKey = "code",
            BasePointCost = 140,
            IsActive = true
        };

        var companion = new User
        {
            UserId = companionId,
            Username = "companion",
            Roles = new List<string> { "learner", "companion" },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = companionId,
                DisplayName = "Companion One",
                Bio = "Bio",
                AvatarUrl = "https://cdn.edskill.test/u/1.png",
                IsPublic = true,
                TotalSessions = 3
            },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = companionId,
                    SkillId = formulaSkill.SkillId,
                    Skill = formulaSkill,
                    Type = UserSkillType.Teach
                },
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = companionId,
                    SkillId = hiddenOfferSkill.SkillId,
                    Skill = hiddenOfferSkill,
                    Type = UserSkillType.Teach
                }
            }
        };

        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = formulaSkill.SkillId,
                Skill = formulaSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 120,
                DurationOptions = new List<int> { 60, 90, 120 },
                PricingModel = SessionPricingModel.FormulaV1,
                ScheduledAt = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = formulaSkill.SkillId,
                Skill = formulaSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                PricingModel = SessionPricingModel.LegacyManual,
                ScheduledAt = new DateTime(2026, 5, 18, 8, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                LearnerId = Guid.NewGuid(),
                SkillId = formulaSkill.SkillId,
                Skill = formulaSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                ActualDuration = 125,
                ScheduledAt = new DateTime(2026, 5, 10, 8, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Completed,
                DisbursedAt = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        var reviewSessionId = sessions[2].SessionId;
        var reviewerId = Guid.NewGuid();
        var users = new List<User>
        {
            companion,
            new()
            {
                UserId = reviewerId,
                Username = "learner",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = reviewerId,
                    DisplayName = "Learner One",
                    IsPublic = true
                }
            }
        };

        var reviews = new List<Review>
        {
            new()
            {
                ReviewId = Guid.NewGuid(),
                SessionId = reviewSessionId,
                ReviewerId = reviewerId,
                RevieweeId = companionId,
                Rating = 5,
                Comment = "Great"
            }
        };

        var userAchievements = new List<UserAchievement>
        {
            AchievementTestData.CreateUserAchievement(companionId, Guid.NewGuid())
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserAchievements).Returns(userAchievements.BuildMockDbSet().Object);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);
        var sessionPricingServiceMock = new Mock<ISessionPricingService>();
        sessionPricingServiceMock
            .Setup(x => x.GetPlatformMarkupPctAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);
        var subscriptionEntitlementServiceMock = new Mock<ISubscriptionEntitlementService>();
        subscriptionEntitlementServiceMock
            .Setup(x => x.GetResolvedEntitlementsAsync(companionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedSubscriptionEntitlements(
                [],
                false,
                true,
                "Companion Pro",
                true,
                12,
                null,
                30m,
                0,
                0));

        var handler = new GetCompanionPublicProfileQueryHandler(
            contextMock.Object,
            dateTimeProviderMock.Object,
            sessionPricingServiceMock.Object,
            subscriptionEntitlementServiceMock.Object);

        var result = await handler.Handle(new GetCompanionPublicProfileQuery(companionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Achievements.Should().ContainSingle();
        result.Value.ActivitySummary.TotalTeachingHours.Should().Be(2);
        result.Value.ActivitySummary.AvgRating.Should().Be(5);
        result.Value.SubscriptionBadge.Should().Be("Companion Pro");
        result.Value.HasPriorityVisibility.Should().BeTrue();
        result.Value.TeachingSkills.Should().HaveCount(2);
        result.Value.TeachingSkills.Should().Contain(skill => skill.SkillId == skillWithoutOfferId && !skill.HasAvailableOffers);
        result.Value.TeachingSkills.Should().Contain(skill =>
            skill.SkillId == skillWithOfferId
            && skill.HasAvailableOffers
            && skill.OfferCount == 1
            && skill.StartingPointCost == 75
            && skill.NextScheduledAt == new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc));
    }
}
