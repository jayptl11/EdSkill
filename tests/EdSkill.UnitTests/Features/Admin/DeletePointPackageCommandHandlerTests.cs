using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Admin.Commands.DeletePointPackage;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Admin;

public class DeletePointPackageCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPackageExists_SoftDeletesPackage()
    {
        var package = new PointPackage
        {
            PointPackageId = Guid.NewGuid(),
            Code = "goi_1",
            Name = "Gói 1",
            Points = 500,
            PriceVnd = 59000,
            IsActive = true,
            IsDeleted = false
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.PointPackages).Returns(new List<PointPackage> { package }.BuildMockDbSet().Object);
        contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        var now = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(now);

        var handler = new DeletePointPackageCommandHandler(contextMock.Object, dateTimeProviderMock.Object);

        var result = await handler.Handle(new DeletePointPackageCommand(package.PointPackageId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        package.IsDeleted.Should().BeTrue();
        package.IsActive.Should().BeFalse();
        package.UpdatedAt.Should().Be(now);
    }
}
