using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Subscriptions;

public class GetSubscriptionPlansQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenPlansHaveDifferentStates_ReturnsOnlyActivePlansInDisplayOrder()
    {
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                SubscriptionPlanId = Guid.NewGuid(),
                Code = "companion_pro",
                Name = "Companion Pro",
                TargetRole = SubscriptionTargetRole.Companion,
                PriceVnd = 79000,
                BenefitsJson = "[\"Companion\"]",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                SubscriptionPlanId = Guid.NewGuid(),
                Code = "inactive",
                Name = "Inactive",
                TargetRole = SubscriptionTargetRole.Learner,
                PriceVnd = 1000,
                BenefitsJson = "[]",
                DisplayOrder = 1,
                IsActive = false
            },
            new()
            {
                SubscriptionPlanId = Guid.NewGuid(),
                Code = "learner_pro",
                Name = "Learner Pro",
                TargetRole = SubscriptionTargetRole.Learner,
                PriceVnd = 119000,
                ImmediateBonusPoints = 200,
                BenefitsJson = "[\"Learner\"]",
                DisplayOrder = 1,
                IsActive = true
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.SubscriptionPlans).Returns(plans.BuildMockDbSet().Object);

        var handler = new GetSubscriptionPlansQueryHandler(contextMock.Object);

        var result = await handler.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Select(item => item.Code).Should().ContainInOrder("learner_pro", "companion_pro");
        result.Value.Data.Should().OnlyContain(item => item.Code != "inactive");
        result.Value.Data.First().Entitlements.ImmediateBonusPoints.Should().Be(200);
    }
}
