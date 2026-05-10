using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.Commands.GenerateAvatarUploadUrl;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class GenerateAvatarUploadUrlCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_GeneratesUserScopedObjectKey()
    {
        var userId = Guid.NewGuid();
        var currentUserService = new Mock<ICurrentUserService>();
        var objectStorageService = new Mock<IObjectStorageService>();

        currentUserService.Setup(x => x.GetUserId()).Returns(userId);
        objectStorageService
            .Setup(x => x.CreateUploadUrlAsync(It.IsAny<ObjectStorageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectStorageUploadRequest request, CancellationToken _) =>
                new ObjectStorageUploadUrl("https://upload", $"https://cdn.edskill.test/{request.ObjectKey}", request.ObjectKey, request.ExpiresAt));

        var handler = new GenerateAvatarUploadUrlCommandHandler(currentUserService.Object, objectStorageService.Object);

        var result = await handler.Handle(new GenerateAvatarUploadUrlCommand("My Avatar.png", "image/png", 2048), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ObjectKey.Should().StartWith($"avatar/{userId:D}/");
        result.Value.ObjectKey.Should().EndWith(".png");
        result.Value.PublicUrl.Should().Contain(result.Value.ObjectKey);
    }
}
