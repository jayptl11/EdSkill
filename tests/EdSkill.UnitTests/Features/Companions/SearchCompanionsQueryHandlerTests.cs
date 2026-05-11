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
    public async Task Handle_WhenOfflineSearchMatchesAvailableSession_ReturnsPublicCompanionsWithRating()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var otherCompanionId = Guid.NewGuid();
        var completedSessionId = Guid.NewGuid();

        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                Aliases = new List<string> { "Tiếng Anh" },
                IsActive = true
            }
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
                    AvatarUrl = "https://cdn.edskill.test/u/1.png",
                    Bio = "Public companion",
                    IsPublic = true
                },
                UserSkills = new List<UserSkill>
                {
                    new()
                    {
                        UserSkillId = Guid.NewGuid(),
                        UserId = companionId,
                        SkillId = skillId,
                        Skill = skills[0],
                        Type = UserSkillType.Teach
                    }
                }
            },
            new()
            {
                UserId = otherCompanionId,
                Username = "companion2",
                Roles = new List<string> { "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = otherCompanionId,
                    DisplayName = "Companion Two",
                    IsPublic = true
                },
                UserSkills = new List<UserSkill>
                {
                    new()
                    {
                        UserSkillId = Guid.NewGuid(),
                        UserId = otherCompanionId,
                        SkillId = skillId,
                        Skill = skills[0],
                        Type = UserSkillType.Teach
                    }
                }
            },
            new()
            {
                UserId = Guid.NewGuid(),
                Username = "learner1",
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    DisplayName = "Learner One",
                    IsPublic = true
                }
            }
        };

        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                Skill = "Tiếng Anh",
                DeliveryMode = SessionDeliveryMode.Offline,
                Location = "Ho Chi Minh City",
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = otherCompanionId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Offline,
                Location = "Ha Noi",
                DurationMinutes = 60,
                PointCost = 90,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = completedSessionId,
                CompanionId = companionId,
                LearnerId = users[2].UserId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddDays(-2),
                Status = SessionStatus.Completed
            }
        };

        var reviews = new List<Review>
        {
            new()
            {
                ReviewId = Guid.NewGuid(),
                SessionId = completedSessionId,
                ReviewerId = users[2].UserId,
                RevieweeId = companionId,
                Rating = 5,
                Comment = "Great session"
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(reviews.BuildMockDbSet().Object);
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.TryGetUserId()).Returns((Guid?)null);

        var handler = new SearchCompanionsQueryHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, SessionDeliveryMode.Offline, "chi minh", 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Data.Should().ContainSingle();
        result.Value.Data.Single().CompanionId.Should().Be(companionId);
        result.Value.Data.Single().AvgRating.Should().Be(5);
        result.Value.Data.Single().TotalReviews.Should().Be(1);
        result.Value.Data.Single().MatchingSessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedCompanionSearches_ExcludesSelfFromResults()
    {
        var skillId = Guid.NewGuid();
        var companionId = Guid.NewGuid();
        var otherCompanionId = Guid.NewGuid();

        var skills = new List<Skill>
        {
            new()
            {
                SkillId = skillId,
                Name = "Speaking",
                Slug = "speaking",
                IsActive = true
            }
        };

        var users = new List<User>
        {
            new()
            {
                UserId = companionId,
                Username = "self-companion",
                Roles = new List<string> { "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = companionId,
                    DisplayName = "Self Companion",
                    IsPublic = true
                },
                UserSkills = new List<UserSkill>
                {
                    new()
                    {
                        UserSkillId = Guid.NewGuid(),
                        UserId = companionId,
                        SkillId = skillId,
                        Skill = skills[0],
                        Type = UserSkillType.Teach
                    }
                }
            },
            new()
            {
                UserId = otherCompanionId,
                Username = "other-companion",
                Roles = new List<string> { "companion" },
                UserProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = otherCompanionId,
                    DisplayName = "Other Companion",
                    IsPublic = true
                },
                UserSkills = new List<UserSkill>
                {
                    new()
                    {
                        UserSkillId = Guid.NewGuid(),
                        UserId = otherCompanionId,
                        SkillId = skillId,
                        Skill = skills[0],
                        Type = UserSkillType.Teach
                    }
                }
            }
        };

        var sessions = new List<Session>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = companionId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 100,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            },
            new()
            {
                SessionId = Guid.NewGuid(),
                CompanionId = otherCompanionId,
                Skill = "Speaking",
                DeliveryMode = SessionDeliveryMode.Online,
                DurationMinutes = 60,
                PointCost = 90,
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                Status = SessionStatus.Available
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Skills).Returns(skills.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(sessions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Reviews).Returns(new List<Review>().BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.TryGetUserId()).Returns(companionId);

        var handler = new SearchCompanionsQueryHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(
            new SearchCompanionsQuery(skillId, SessionDeliveryMode.Online, null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().ContainSingle();
        result.Value.Data.Single().CompanionId.Should().Be(otherCompanionId);
    }
}
