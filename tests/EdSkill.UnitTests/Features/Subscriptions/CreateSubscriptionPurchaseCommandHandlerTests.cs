using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Subscriptions.Commands.CreateSubscriptionPurchase;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Subscriptions;

public class CreateSubscriptionPurchaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValidLearnerPlan_CreatesPendingPaymentAndReturnsUrl()
    {
        var userId = Guid.NewGuid();
        var plan = CreatePlan(SubscriptionTargetRole.Learner, "learner_pro", 119000);
        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var payments = new List<PaymentTransaction>();

        var contextMock = CreateContextMock(users, [plan], [], payments);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var now = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        VnPayCreatePaymentRequest? capturedRequest = null;
        vnPayGatewayMock
            .Setup(x => x.CreatePaymentUrl(It.IsAny<VnPayCreatePaymentRequest>()))
            .Callback<VnPayCreatePaymentRequest>(request => capturedRequest = request)
            .Returns(Result<VnPayCreatePaymentResult>.Success(
                new VnPayCreatePaymentResult("https://sandbox.vnpay.test/sub", now.AddMinutes(15), Guid.NewGuid().ToString("N"))));

        var handler = new CreateSubscriptionPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreateSubscriptionPurchaseCommand(plan.SubscriptionPlanId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payments.Should().ContainSingle();
        payments[0].SubscriptionPlanId.Should().Be(plan.SubscriptionPlanId);
        payments[0].PaymentUrl.Should().Be("https://sandbox.vnpay.test/sub");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Purpose.Should().Be(VnPayPaymentPurpose.SubscriptionPurchase);
        capturedRequest.OrderDescription.Should().Be($"Mua goi {plan.Name}");
    }

    [Fact]
    public async Task Handle_WhenCoverageOverlapsActiveSubscription_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var learnerPlan = CreatePlan(SubscriptionTargetRole.Learner, "learner_pro", 119000);
        var activeMultiRolePlan = CreatePlan(SubscriptionTargetRole.MultiRole, "multi_role_pro", 179000);
        var users = new List<User> { new() { UserId = userId, Roles = ["learner", "companion"] } };
        var userSubscriptions = new List<UserSubscription>
        {
            new()
            {
                UserSubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PlanId = activeMultiRolePlan.SubscriptionPlanId,
                Plan = activeMultiRolePlan,
                Status = UserSubscriptionStatus.Active,
                StartedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        var contextMock = CreateContextMock(users, [learnerPlan, activeMultiRolePlan], userSubscriptions, []);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        var handler = new CreateSubscriptionPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreateSubscriptionPurchaseCommand(learnerPlan.SubscriptionPlanId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SUBSCRIPTION_PLAN_CONFLICT");
    }

    [Fact]
    public async Task Handle_WhenLearnerAndCompanionCoverageDoNotOverlap_AllowsConcurrentPurchase()
    {
        var userId = Guid.NewGuid();
        var learnerPlan = CreatePlan(SubscriptionTargetRole.Learner, "learner_pro", 119000);
        var companionPlan = CreatePlan(SubscriptionTargetRole.Companion, "companion_pro", 79000);
        var users = new List<User> { new() { UserId = userId, Roles = ["learner", "companion"] } };
        var userSubscriptions = new List<UserSubscription>
        {
            new()
            {
                UserSubscriptionId = Guid.NewGuid(),
                UserId = userId,
                PlanId = learnerPlan.SubscriptionPlanId,
                Plan = learnerPlan,
                Status = UserSubscriptionStatus.Active,
                StartedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };
        var payments = new List<PaymentTransaction>();

        var contextMock = CreateContextMock(users, [learnerPlan, companionPlan], userSubscriptions, payments);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var now = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        VnPayCreatePaymentRequest? capturedRequest = null;
        vnPayGatewayMock
            .Setup(x => x.CreatePaymentUrl(It.IsAny<VnPayCreatePaymentRequest>()))
            .Callback<VnPayCreatePaymentRequest>(request => capturedRequest = request)
            .Returns(Result<VnPayCreatePaymentResult>.Success(
                new VnPayCreatePaymentResult("https://sandbox.vnpay.test/sub", now.AddMinutes(15), Guid.NewGuid().ToString("N"))));

        var handler = new CreateSubscriptionPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreateSubscriptionPurchaseCommand(companionPlan.SubscriptionPlanId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payments.Should().ContainSingle();
        payments[0].SubscriptionPlanId.Should().Be(companionPlan.SubscriptionPlanId);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Purpose.Should().Be(VnPayPaymentPurpose.SubscriptionPurchase);
    }

    private static SubscriptionPlan CreatePlan(SubscriptionTargetRole targetRole, string code, int priceVnd)
    {
        return new SubscriptionPlan
        {
            SubscriptionPlanId = Guid.NewGuid(),
            Code = code,
            Name = code,
            TargetRole = targetRole,
            PriceVnd = priceVnd,
            Currency = "VND",
            BillingCycle = SubscriptionBillingCycle.Monthly,
            BenefitsJson = "[]",
            IsActive = true
        };
    }

    private static Mock<IApplicationDbContext> CreateContextMock(
        List<User> users,
        List<SubscriptionPlan> plans,
        List<UserSubscription> userSubscriptions,
        List<PaymentTransaction> payments)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SubscriptionPlans).Returns(plans.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserSubscriptions).Returns(userSubscriptions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);
        return contextMock;
    }
}
