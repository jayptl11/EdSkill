using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Wallet.Queries.GetMyPayments;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Wallet;

public class GetMyPaymentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStatusFilterAndPaginationApplied_ReturnsExpectedPage()
    {
        var userId = Guid.NewGuid();
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = "goi_1",
            Name = "Gói 1",
            Points = 500,
            PriceVnd = 59000
        };

        var payments = new List<PaymentTransaction>
        {
            new() { PaymentTransactionId = Guid.NewGuid(), UserId = userId, PointPackageId = package.PointPackageId, Provider = PaymentProvider.VnPay, AmountVnd = 59000, Currency = "VND", Status = PaymentStatus.Pending, CreatedAt = new DateTime(2026, 5, 17, 8, 0, 0, DateTimeKind.Utc) },
            new() { PaymentTransactionId = Guid.NewGuid(), UserId = userId, PointPackageId = package.PointPackageId, Provider = PaymentProvider.VnPay, AmountVnd = 59000, Currency = "VND", Status = PaymentStatus.Pending, CreatedAt = new DateTime(2026, 5, 17, 9, 0, 0, DateTimeKind.Utc) },
            new() { PaymentTransactionId = Guid.NewGuid(), UserId = userId, PointPackageId = package.PointPackageId, Provider = PaymentProvider.VnPay, AmountVnd = 59000, Currency = "VND", Status = PaymentStatus.Success, CreatedAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc) }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.PaymentTransactions).Returns(payments.BuildMockDbSet().Object);
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var handler = new GetMyPaymentsQueryHandler(contextMock.Object, currentUserServiceMock.Object);

        var result = await handler.Handle(new GetMyPaymentsQuery("pending", 2, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Data.Should().ContainSingle();
        result.Value.Data.Single().Status.Should().Be(PaymentStatus.Pending);
        result.Value.Data.Single().PackageName.Should().Be("Gói 1");
    }
}
