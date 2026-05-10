using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Profile.Commands.GenerateDegreeUploadUrl;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Profile;

public class GenerateDegreeUploadUrlCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestIsValid_GeneratesDegreeScopedObjectKey()
    {
        var userId = Guid.NewGuid();
        var currentUserService = new Mock<ICurrentUserService>();
        var objectStorageService = new Mock<IObjectStorageService>();

        currentUserService.Setup(x => x.GetUserId()).Returns(userId);
        objectStorageService
            .Setup(x => x.CreateUploadUrlAsync(It.IsAny<ObjectStorageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectStorageUploadRequest request, CancellationToken _) =>
                new ObjectStorageUploadUrl("https://upload", $"https://cdn.edskill.test/{request.ObjectKey}", request.ObjectKey, request.ExpiresAt));

        var handler = new GenerateDegreeUploadUrlCommandHandler(currentUserService.Object, objectStorageService.Object);

        var result = await handler.Handle(new GenerateDegreeUploadUrlCommand("My Degree.pdf", "application/pdf", 2048), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ObjectKey.Should().StartWith($"degree/{userId:D}/");
        result.Value.ObjectKey.Should().EndWith(".pdf");
        result.Value.PublicUrl.Should().Contain(result.Value.ObjectKey);
    }
}
