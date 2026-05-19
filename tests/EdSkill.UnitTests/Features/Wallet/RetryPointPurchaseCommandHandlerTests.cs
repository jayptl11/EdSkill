using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.Commands.RetryPointPurchase;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Wallet;

public class RetryPointPurchaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPendingPointPaymentExists_CreatesNewPaymentAndCancelsOldOne()
    {
        var userId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 19, 9, 0, 0, DateTimeKind.Utc);

        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var package = new PointPackage
        {
            PointPackageId = packageId,
            Code = "goi_1",
            Name = "Goi 1",
            Points = 500,
            BonusPoints = 0,
            PriceVnd = 59000,
            Currency = "VND",
            IsActive = true,
            IsDeleted = false
        };

        var payments = new List<PaymentTransaction>
        {
            new()
            {
                PaymentTransactionId = sourcePaymentId,
                UserId = userId,
                PointPackageId = packageId,
                Provider = PaymentProvider.VnPay,
                AmountVnd = 59000,
                Currency = "VND",
                Status = PaymentStatus.Pending,
                PaymentUrl = "https://sandbox.vnpay.test/expired",
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-2)
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var requestContextServiceMock = new Mock<IRequestContextService>();
        requestContextServiceMock.Setup(x => x.GetClientIpAddress()).Returns("203.0.113.10");

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        VnPayCreatePaymentRequest? capturedRequest = null;
        vnPayGatewayMock
            .Setup(x => x.CreatePaymentUrl(It.IsAny<VnPayCreatePaymentRequest>()))
            .Callback<VnPayCreatePaymentRequest>(request => capturedRequest = request)
            .Returns(Result<VnPayCreatePaymentResult>.Success(
                new VnPayCreatePaymentResult("https://sandbox.vnpay.test/new-pay", now.AddMinutes(15), Guid.NewGuid().ToString("N"))));

        var handler = new RetryPointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new RetryPointPurchaseCommand(sourcePaymentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payments.Should().HaveCount(2);
        payments[0].Status.Should().Be(PaymentStatus.Cancelled);
        payments[0].UpdatedAt.Should().Be(now);
        payments[1].AmountVnd.Should().Be(59000);
        payments[1].PointPackageId.Should().Be(packageId);
        payments[1].PaymentUrl.Should().Be("https://sandbox.vnpay.test/new-pay");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.AmountVnd.Should().Be(59000);
        capturedRequest.Purpose.Should().Be(VnPayPaymentPurpose.PointPurchase);
    }

    [Fact]
    public async Task Handle_WhenPaymentBelongsToAnotherUser_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();

        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var payments = new List<PaymentTransaction>
        {
            new()
            {
                PaymentTransactionId = sourcePaymentId,
                UserId = anotherUserId,
                PointPackageId = Guid.NewGuid(),
                AmountVnd = 59000,
                Currency = "VND",
                Status = PaymentStatus.Pending
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var requestContextServiceMock = new Mock<IRequestContextService>();
        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();

        var handler = new RetryPointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new RetryPointPurchaseCommand(sourcePaymentId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadySucceeded_ReturnsInvalidStatus()
    {
        var userId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();

        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var payments = new List<PaymentTransaction>
        {
            new()
            {
                PaymentTransactionId = sourcePaymentId,
                UserId = userId,
                PointPackageId = Guid.NewGuid(),
                AmountVnd = 59000,
                Currency = "VND",
                Status = PaymentStatus.Success
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var requestContextServiceMock = new Mock<IRequestContextService>();
        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();

        var handler = new RetryPointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new RetryPointPurchaseCommand(sourcePaymentId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PAYMENT_RETRY_INVALID_STATUS");
    }
}
