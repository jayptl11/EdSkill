using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Wallet.Commands.CreatePointPurchase;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Wallet;

public class CreatePointPurchaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValidRequest_CreatesPendingPaymentAndReturnsUrl()
    {
        var userId = Guid.NewGuid();
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = "goi_1",
            Name = "Goi 1",
            Points = 500,
            PriceVnd = 59000,
            Currency = "VND",
            IsActive = true,
            IsDeleted = false
        };

        var users = new List<User> { new() { UserId = userId, Roles = ["learner"] } };
        var payments = new List<PaymentTransaction>();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(users.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var now = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var requestContextServiceMock = new Mock<IRequestContextService>();
        requestContextServiceMock.Setup(x => x.GetClientIpAddress()).Returns("203.0.113.10");

        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();
        VnPayCreatePaymentRequest? capturedRequest = null;
        vnPayGatewayMock
            .Setup(x => x.CreatePaymentUrl(It.IsAny<VnPayCreatePaymentRequest>()))
            .Callback<VnPayCreatePaymentRequest>(request => capturedRequest = request)
            .Returns(Result<VnPayCreatePaymentResult>.Success(
                new VnPayCreatePaymentResult("https://sandbox.vnpay.test/pay", now.AddMinutes(15), Guid.NewGuid().ToString("N"))));

        var handler = new CreatePointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreatePointPurchaseCommand(package.PointPackageId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payments.Should().ContainSingle();
        payments[0].PointPackageId.Should().Be(package.PointPackageId);
        payments[0].PaymentUrl.Should().Be("https://sandbox.vnpay.test/pay");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Purpose.Should().Be(VnPayPaymentPurpose.PointPurchase);
        capturedRequest.OrderDescription.Should().Be($"Nap diem {package.Name}");
        capturedRequest.ClientIpAddress.Should().Be("203.0.113.10");
    }

    [Fact]
    public async Task Handle_WhenUserIsPureAdmin_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = "goi_1",
            Name = "Goi 1",
            Points = 500,
            PriceVnd = 59000,
            Currency = "VND",
            IsActive = true
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(new List<User> { new() { UserId = userId, Roles = ["admin"] } }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var requestContextServiceMock = new Mock<IRequestContextService>();
        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();

        var handler = new CreatePointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreatePointPurchaseCommand(package.PointPackageId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task Handle_WhenPackageIsInactive_ReturnsUnavailable()
    {
        var userId = Guid.NewGuid();
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = "goi_1",
            Name = "Goi 1",
            Points = 500,
            PriceVnd = 59000,
            Currency = "VND",
            IsActive = false
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.Users).Returns(new List<User> { new() { UserId = userId, Roles = ["learner"] } }.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var requestContextServiceMock = new Mock<IRequestContextService>();
        var vnPayGatewayMock = new Mock<IVnPayGatewayService>();

        var handler = new CreatePointPurchaseCommandHandler(
            contextMock.Object,
            currentUserServiceMock.Object,
            dateTimeProviderMock.Object,
            requestContextServiceMock.Object,
            vnPayGatewayMock.Object);

        var result = await handler.Handle(new CreatePointPurchaseCommand(package.PointPackageId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POINT_PACKAGE_NOT_AVAILABLE");
    }
}
