using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Services;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Wallet;

public class WalletPaymentProcessingServiceTests
{
    [Fact]
    public async Task ProcessVnPayCallbackAsync_WhenSuccessAndPending_CreditsPointsOnce()
    {
        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PointPackageId = Guid.NewGuid(),
            Provider = PaymentProvider.VnPay,
            AmountVnd = 59000,
            Currency = "VND",
            Status = PaymentStatus.Pending
        };
        var package = new PointPackage
        {
            PointPackageId = payment.PointPackageId!.Value,
            Code = "goi_1",
            Name = "Gói 1",
            Points = 500,
            BonusPoints = 0,
            PriceVnd = 59000
        };

        var payments = new List<PaymentTransaction> { payment };
        var packages = new List<PointPackage> { package };
        var wallets = new List<PointWallet>();
        var pointTransactions = new List<PointTransaction>();

        var contextMock = CreateContextMock(payments, packages, wallets, pointTransactions);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<WalletPaymentProcessingResult>(It.IsAny<Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>> operation, CancellationToken ct) => operation(ct));

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        vnPayGatewayMock
            .Setup(x => x.ParseCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(Result<VnPayCallbackParseResult>.Success(
                new VnPayCallbackParseResult(
                    payment.PaymentTransactionId,
                    PaymentStatus.Success,
                    "123456",
                    59000,
                    dateTimeProviderMock.Object.UtcNow,
                    new Dictionary<string, string> { ["vnp_TxnRef"] = payment.PaymentTransactionId.ToString("N") })));

        var pointLedgerService = new PointLedgerService(contextMock.Object, dateTimeProviderMock.Object);
        var subscriptionEntitlementServiceMock = new Mock<ISubscriptionEntitlementService>();
        var service = new WalletPaymentProcessingService(
            contextMock.Object,
            dateTimeProviderMock.Object,
            pointLedgerService,
            subscriptionEntitlementServiceMock.Object,
            transactionExecutorMock.Object,
            vnPayGatewayMock.Object);

        var result = await service.ProcessVnPayCallbackAsync(new Dictionary<string, string> { ["vnp_TxnRef"] = payment.PaymentTransactionId.ToString("N") }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CreditedPoints.Should().Be(500);
        result.Value.AlreadyProcessed.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Success);
        wallets.Should().ContainSingle();
        wallets[0].Balance.Should().Be(500);
        pointTransactions.Should().ContainSingle();
        pointTransactions[0].Type.Should().Be(PointTransactionType.Purchase);
    }

    [Fact]
    public async Task ProcessVnPayCallbackAsync_WhenAlreadySuccess_DoesNotDoubleCredit()
    {
        var packageId = Guid.NewGuid();
        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PointPackageId = packageId,
            Provider = PaymentProvider.VnPay,
            AmountVnd = 59000,
            Currency = "VND",
            Status = PaymentStatus.Success
        };
        var package = new PointPackage
        {
            PointPackageId = packageId,
            Code = "goi_1",
            Name = "Gói 1",
            Points = 500,
            BonusPoints = 0,
            PriceVnd = 59000
        };
        var wallet = new PointWallet
        {
            PointWalletId = Guid.NewGuid(),
            UserId = payment.UserId,
            Balance = 500,
            TotalEarned = 500
        };

        var pointTransactions = new List<PointTransaction>
        {
            new()
            {
                PointTransactionId = Guid.NewGuid(),
                UserId = payment.UserId,
                Type = PointTransactionType.Purchase,
                Amount = 500
            }
        };

        var contextMock = CreateContextMock(
            new List<PaymentTransaction> { payment },
            new List<PointPackage> { package },
            new List<PointWallet> { wallet },
            pointTransactions);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        vnPayGatewayMock
            .Setup(x => x.ParseCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(Result<VnPayCallbackParseResult>.Success(
                new VnPayCallbackParseResult(payment.PaymentTransactionId, PaymentStatus.Success, "123456", 59000, DateTime.UtcNow, new Dictionary<string, string>())));

        var pointLedgerService = new PointLedgerService(contextMock.Object, dateTimeProviderMock.Object);
        var subscriptionEntitlementServiceMock = new Mock<ISubscriptionEntitlementService>();
        var service = new WalletPaymentProcessingService(
            contextMock.Object,
            dateTimeProviderMock.Object,
            pointLedgerService,
            subscriptionEntitlementServiceMock.Object,
            transactionExecutorMock.Object,
            vnPayGatewayMock.Object);

        var result = await service.ProcessVnPayCallbackAsync(new Dictionary<string, string>(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AlreadyProcessed.Should().BeTrue();
        pointTransactions.Should().HaveCount(1);
        wallet.Balance.Should().Be(500);
    }

    [Fact]
    public async Task ProcessVnPayCallbackAsync_WhenFailed_MarksPaymentWithoutCrediting()
    {
        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PointPackageId = Guid.NewGuid(),
            Provider = PaymentProvider.VnPay,
            AmountVnd = 59000,
            Currency = "VND",
            Status = PaymentStatus.Pending
        };
        var package = new PointPackage
        {
            PointPackageId = payment.PointPackageId!.Value,
            Code = "goi_1",
            Name = "Gói 1",
            Points = 500,
            BonusPoints = 0,
            PriceVnd = 59000
        };

        var payments = new List<PaymentTransaction> { payment };
        var packages = new List<PointPackage> { package };
        var wallets = new List<PointWallet>();
        var pointTransactions = new List<PointTransaction>();

        var contextMock = CreateContextMock(payments, packages, wallets, pointTransactions);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<WalletPaymentProcessingResult>(It.IsAny<Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>> operation, CancellationToken ct) => operation(ct));

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        vnPayGatewayMock
            .Setup(x => x.ParseCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(Result<VnPayCallbackParseResult>.Success(
                new VnPayCallbackParseResult(payment.PaymentTransactionId, PaymentStatus.Failed, "123456", 59000, null, new Dictionary<string, string>())));

        var pointLedgerService = new PointLedgerService(contextMock.Object, dateTimeProviderMock.Object);
        var subscriptionEntitlementServiceMock = new Mock<ISubscriptionEntitlementService>();
        var service = new WalletPaymentProcessingService(
            contextMock.Object,
            dateTimeProviderMock.Object,
            pointLedgerService,
            subscriptionEntitlementServiceMock.Object,
            transactionExecutorMock.Object,
            vnPayGatewayMock.Object);

        var result = await service.ProcessVnPayCallbackAsync(new Dictionary<string, string>(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        wallets.Should().BeEmpty();
        pointTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessVnPayCallbackAsync_WhenSubscriptionSuccess_ActivatesSubscriptionAndCreditsImmediateBonus()
    {
        var planId = Guid.NewGuid();
        var payment = new PaymentTransaction
        {
            PaymentTransactionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SubscriptionPlanId = planId,
            Provider = PaymentProvider.VnPay,
            AmountVnd = 119000,
            Currency = "VND",
            Status = PaymentStatus.Pending
        };
        var plan = new SubscriptionPlan
        {
            SubscriptionPlanId = planId,
            Code = "learner_pro",
            Name = "Learner Pro",
            TargetRole = SubscriptionTargetRole.Learner,
            PriceVnd = 119000,
            ImmediateBonusPoints = 200,
            IsActive = true
        };

        var payments = new List<PaymentTransaction> { payment };
        var wallets = new List<PointWallet>();
        var pointTransactions = new List<PointTransaction>();
        var subscriptionPlans = new List<SubscriptionPlan> { plan };
        var userSubscriptions = new List<UserSubscription>();

        var contextMock = CreateContextMock(
            payments,
            [],
            wallets,
            pointTransactions,
            subscriptionPlans,
            userSubscriptions);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var paidAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(paidAt);

        var transactionExecutorMock = new Mock<ITransactionExecutor>();
        transactionExecutorMock
            .Setup(x => x.ExecuteAsync<WalletPaymentProcessingResult>(It.IsAny<Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<WalletPaymentProcessingResult>>> operation, CancellationToken ct) => operation(ct));

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        vnPayGatewayMock
            .Setup(x => x.ParseCallback(It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Returns(Result<VnPayCallbackParseResult>.Success(
                new VnPayCallbackParseResult(
                    payment.PaymentTransactionId,
                    PaymentStatus.Success,
                    "SUB123",
                    119000,
                    paidAt,
                    new Dictionary<string, string>())));

        var pointLedgerService = new PointLedgerService(contextMock.Object, dateTimeProviderMock.Object);
        var subscriptionEntitlementService = new SubscriptionEntitlementService(
            contextMock.Object,
            dateTimeProviderMock.Object,
            pointLedgerService);
        var service = new WalletPaymentProcessingService(
            contextMock.Object,
            dateTimeProviderMock.Object,
            pointLedgerService,
            subscriptionEntitlementService,
            transactionExecutorMock.Object,
            vnPayGatewayMock.Object);

        var result = await service.ProcessVnPayCallbackAsync(new Dictionary<string, string>(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SubscriptionPlanId.Should().Be(planId);
        result.Value.SubscriptionPlanName.Should().Be("Learner Pro");
        result.Value.CreditedPoints.Should().Be(200);
        payment.Status.Should().Be(PaymentStatus.Success);
        userSubscriptions.Should().ContainSingle();
        userSubscriptions[0].PlanId.Should().Be(planId);
        wallets.Should().ContainSingle();
        wallets[0].Balance.Should().Be(200);
        pointTransactions.Should().ContainSingle();
        pointTransactions[0].Type.Should().Be(PointTransactionType.SubscriptionPurchaseBonus);
    }

    private static Mock<IApplicationDbContext> CreateContextMock(
        List<PaymentTransaction> payments,
        List<PointPackage> packages,
        List<PointWallet> wallets,
        List<PointTransaction> pointTransactions,
        List<SubscriptionPlan>? subscriptionPlans = null,
        List<UserSubscription>? userSubscriptions = null)
    {
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(packages.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointWallets).Returns(wallets.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointTransactions).Returns(pointTransactions.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.SubscriptionPlans).Returns((subscriptionPlans ?? []).BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.UserSubscriptions).Returns((userSubscriptions ?? []).BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.Sessions).Returns(new List<Session>().BuildMockDbSet().Object);
        return contextMock;
    }
}
