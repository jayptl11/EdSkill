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
    public async Task Handle_WhenCompanionIsPublic_ReturnsReviewsAndMatchingSessions()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Speaking",
            Slug = "speaking",
            BasePointCost = 100,
            IsActive = true
        };

        var users = new List<User>
        {
            new()
            {
                UserId = companionId,
                Username = "companion1",
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
                        SkillId = skillId,
                        Skill = skill,
                        Type = UserSkillType.Teach
                    }
                }
            },
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
            new()
            {
                SessionId = sessionId,
                CompanionId = companionId,
                SkillId = skillId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Offline,
                Location = "District 1",
                DurationMinutes = 60,
                PointCost = 120,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
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
                ReviewId = reviewId,
                SessionId = sessions[1].SessionId,
                ReviewerId = learnerId,
                RevieweeId = companionId,
                Rating = 4,
                Comment = "Helpful"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(new[] { skill }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);
        var sessionPricingServiceMock = new Mock<ISessionPricingService>();

        var handler = new GetCompanionDetailQueryHandler(contextMock.Object, sessionPricingServiceMock.Object);

        var result = await handler.Handle(
            new GetCompanionDetailQuery(companionId, skillId, SessionDeliveryMode.Offline, "district", 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanionId.Should().Be(companionId);
        result.Value.AvgRating.Should().Be(4);
        result.Value.TotalReviews.Should().Be(1);
        result.Value.Reviews.Data.Should().ContainSingle();
        result.Value.Reviews.Data.Single().ReviewerDisplayName.Should().Be("Learner One");
        result.Value.Sessions.Should().ContainSingle();
        result.Value.Sessions.Single().Location.Should().Be("District 1");
    }
}
