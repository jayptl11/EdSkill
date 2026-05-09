using System.Security.Claims;
using System.Text.Json;
using EdSkill.API.Middleware;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Domain.Entities;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EdSkill.UnitTests.Features.Auth;

public class SuspendedUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedUserSuspended_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contextMock = new Mock<IApplicationDbContext>();
        SetupUsersDbSet(contextMock, new List<User>
        {
            new() { UserId = userId, Email = "test@test.com", Username = "test", Status = "suspended" }
        });

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                "TestAuth"))
        };
        httpContext.Response.Body = new MemoryStream();

        var nextCalled = false;
        var middleware = new SuspendedUserMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, contextMock.Object);

        // Assert
        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        httpContext.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<JsonElement>(httpContext.Response.Body);
        payload.GetProperty("errorCode").GetString().Should().Be("ACCOUNT_SUSPENDED");
    }

    private static void SetupUsersDbSet(Mock<IApplicationDbContext> contextMock, List<User> users)
    {
        var queryable = new TestAsyncEnumerable<User>(users);
        var dbSetMock = new Mock<DbSet<User>>();
        dbSetMock.As<IQueryable<User>>().Setup(m => m.Provider).Returns(queryable.AsQueryable().Provider);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryable.AsQueryable().Expression);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryable.AsQueryable().ElementType);
        dbSetMock.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(queryable.AsQueryable().GetEnumerator());
        dbSetMock.As<IAsyncEnumerable<User>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(queryable.GetAsyncEnumerator());

        contextMock.Setup(x => x.Users).Returns(dbSetMock.Object);
    }
}
