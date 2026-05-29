using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Companions.Queries.GetCompanionSkillDetail;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Companions;

public class GetCompanionSkillDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenSkillIsTaught_ReturnsOnlyMatchingOffers()
    {
        var now = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var companionId = Guid.NewGuid();
        var taughtSkillId = Guid.NewGuid();
        var otherSkillId = Guid.NewGuid();
        var taughtSkill = new Skill
        {
            SkillId = taughtSkillId,
            Name = "Speaking",
            Slug = "speaking",
            IconKey = "languages",
            BasePointCost = 100,
            IsActive = true
        };
        var otherSkill = new Skill
        {
            SkillId = otherSkillId,
            Name = "Excel",
            Slug = "excel",
            IconKey = "calculator",
            BasePointCost = 90,
            IsActive = true
        };

        var companion = new User
        {
            UserId = companionId,
            Username = "companion",
            Roles = new List<string> { "companion" },
            UserProfile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = companionId,
                DisplayName = "Companion",
                IsPublic = true
            },
            UserSkills = new List<UserSkill>
            {
                new()
                {
                    UserSkillId = Guid.NewGuid(),
                    UserId = companionId,
                    SkillId = taughtSkillId,
                    Skill = taughtSkill,
                    Type = UserSkillType.Teach
                }
            }
        };

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
                    DisplayName = "Learner"
                }
            }
        };

        var matchingOffer = new Session
        {
            SessionId = Guid.NewGuid(),
            CompanionId = companionId,
            SkillId = taughtSkillId,
            Skill = taughtSkill.Name,
            DeliveryMode = SessionDeliveryMode.Online,
            DurationMinutes = 60,
            PointCost = 100,
            PricingModel = SessionPricingModel.LegacyManual,
            ScheduledAt = new DateTime(2026, 5, 18, 8, 0, 0, DateTimeKind.Utc),
            Status = SessionStatus.Available
        };

        var sessions = new List<Session>
        {
            matchingOffer,
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = taughtSkillId,
                Skill = taughtSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 80,
                PricingModel = SessionPricingModel.LegacyManual,
                ScheduledAt = now.AddDays(-2),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                SkillId = otherSkillId,
                Skill = otherSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 90,
                PricingModel = SessionPricingModel.LegacyManual,
                ScheduledAt = new DateTime(2026, 5, 19, 8, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                LearnerId = reviewerId,
                SkillId = taughtSkillId,
                Skill = taughtSkill.Name,
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                ActualDuration = 60,
                ScheduledAt = new DateTime(2026, 5, 10, 8, 0, 0, DateTimeKind.Utc),
                Status = SessionStatus.Completed,
                DisbursedAt = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        var reviews = new List<Review>
        {
            new()
            {
                ReviewId = Guid.NewGuid(),
                SessionId = sessions[2].SessionId,
                ReviewerId = reviewerId,
                RevieweeId = companionId,
                Rating = 4,
                Comment = "Helpful"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);
        var sessionPricingServiceMock = new Mock<ISessionPricingService>();

        var handler = new GetCompanionSkillDetailQueryHandler(
            contextMock.Object,
            dateTimeProviderMock.Object,
            sessionPricingServiceMock.Object);

        var result = await handler.Handle(
            new GetCompanionSkillDetailQuery(companionId, taughtSkillId, 1, 10, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Skill.SkillId.Should().Be(taughtSkillId);
        result.Value.Offers.Total.Should().Be(1);
        result.Value.Offers.Data.Should().ContainSingle();
        result.Value.Offers.Data.Single().SessionId.Should().Be(matchingOffer.SessionId);
        result.Value.TotalReviews.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenProfileIsPrivate_ReturnsFailure()
    {
        var companionId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var users = new List<User>
        {
            new()
            {
                UserId = companionId,
                Username = "companion",
                Roles = new List<string> { "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = companionId,
                    DisplayName = "Private",
                    IsPublic = false
                },
                UserSkills = new List<UserSkill>
                {
                    new()
                    {
                        UserSkillId = Guid.NewGuid(),
                        UserId = companionId,
                        SkillId = skillId,
                        Skill = new Skill
                        {
                            SkillId = skillId,
                            Name = "Speaking",
                            Slug = "speaking",
                            IsActive = true
                        },
                        Type = UserSkillType.Teach
                    }
                }
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(new List<Session>().BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(new List<Review>().BuildMockDbSet().Object);
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc));

        var handler = new GetCompanionSkillDetailQueryHandler(
            contextMock.Object,
            dateTimeProviderMock.Object,
            Mock.Of<ISessionPricingService>());

        var result = await handler.Handle(new GetCompanionSkillDetailQuery(companionId, skillId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROFILE_PRIVATE");
    }
}
