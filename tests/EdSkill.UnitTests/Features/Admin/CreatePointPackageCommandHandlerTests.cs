using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Features.Admin.Commands.CreatePointPackage;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Admin;

public class CreatePointPackageCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsFailure()
    {
        var packages = new List<PointPackage>
        {
            new()
            {
                PointPackageId = Guid.NewGuid(),
                Code = "goi_1",
                Name = "Gói 1",
                Points = 500,
                PriceVnd = 59000
            }
        };

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.SetupGet(x => x.PointPackages).Returns(packages.BuildMockDbSet().Object);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        var handler = new CreatePointPackageCommandHandler(contextMock.Object, dateTimeProviderMock.Object);

        var result = await handler.Handle(
            new CreatePointPackageCommand(" Gói 1 ", "Gói khác", null, 1000, 0, 99000, null, false, 2, true, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POINT_PACKAGE_CODE_EXISTS");
    }
}
