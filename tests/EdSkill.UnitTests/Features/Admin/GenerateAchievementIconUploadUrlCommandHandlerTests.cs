using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Admin;

public class GenerateAchievementIconUploadUrlCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_GeneratesAchievementScopedObjectKey()
    {
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);

        var objectStorageServiceMock = new Mock<IObjectStorageService>();
        objectStorageServiceMock
            .Setup(x => x.CreateUploadUrlAsync(It.IsAny<ObjectStorageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectStorageUploadRequest request, CancellationToken _) =>
                new ObjectStorageUploadUrl("https://upload", $"https://cdn.edskill.test/{request.ObjectKey}", request.ObjectKey, request.ExpiresAt));

        var handler = new GenerateAchievementIconUploadUrlCommandHandler(currentUserServiceMock.Object, objectStorageServiceMock.Object);

        var result = await handler.Handle(
            new GenerateAchievementIconUploadUrlCommand("Badge Icon.png", "image/png", 1024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ObjectKey.Should().StartWith($"achievement/{userId:D}/");
        result.Value.ObjectKey.Should().EndWith(".png");
    }
}
