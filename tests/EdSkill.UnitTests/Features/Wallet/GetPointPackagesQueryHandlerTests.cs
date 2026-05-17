using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Wallet.Queries.GetPointPackages;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Wallet;

public class GetPointPackagesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenPackagesHaveDifferentStates_ReturnsOnlySellablePackages()
    {
        var now = new DateTime(2026, 5, 17, 8, 0, 0, DateTimeKind.Utc);
        var packages = new List<PointPackage>
        {
            new() { PointPackageId = Guid.NewGuid(), Code = "a", Name = "A", Points = 500, PriceVnd = 59000, IsActive = true, IsDeleted = false, DisplayOrder = 2 },
            new() { PointPackageId = Guid.NewGuid(), Code = "b", Name = "B", Points = 1000, PriceVnd = 99000, IsActive = false, IsDeleted = false, DisplayOrder = 1 },
            new() { PointPackageId = Guid.NewGuid(), Code = "c", Name = "C", Points = 2000, PriceVnd = 169000, IsActive = true, IsDeleted = true, DisplayOrder = 3 },
            new() { PointPackageId = Guid.NewGuid(), Code = "d", Name = "D", Points = 5000, PriceVnd = 379000, IsActive = true, IsDeleted = false, StartsAt = now.AddDays(1), DisplayOrder = 4 },
            new() { PointPackageId = Guid.NewGuid(), Code = "e", Name = "E", Points = 6000, PriceVnd = 399000, IsActive = true, IsDeleted = false, EndsAt = now.AddDays(-1), DisplayOrder = 5 },
            new() { PointPackageId = Guid.NewGuid(), Code = "f", Name = "F", Points = 7000, PriceVnd = 499000, IsActive = true, IsDeleted = false, DisplayOrder = 1 }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.PointPackages).Returns(packages.BuildMockDbSet().Object);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var handler = new GetPointPackagesQueryHandler(contextMock.Object, dateTimeProviderMock.Object);

        var result = await handler.Handle(new GetPointPackagesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Select(item => item.Code).Should().ContainInOrder("f", "a");
    }
}
