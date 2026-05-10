using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Common.Services;
using EdSkill.Domain.Entities;
using EdSkill.Domain.Enums;
using EdSkill.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Policies;

public class PolicyConsentServiceTests
{
    private const string ActiveVersion = "2026-05-10.v1";

    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly PolicyConsentService _service;

    public PolicyConsentServiceTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.SetupGet(provider => provider.UtcNow).Returns(new DateTime(2026, 5, 10, 1, 0, 0, DateTimeKind.Utc));
        _service = new PolicyConsentService(_contextMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task ValidateRegistrationPolicyAcceptancesAsync_WhenVersionIsInactive_ReturnsFailure()
    {
        SetupPolicyDocuments(BuildRequiredPolicyDocuments());

        var result = await _service.ValidateRegistrationPolicyAcceptancesAsync(
        [
            new PolicyAcceptanceInput("terms", "2026-05-09.v1"),
            new PolicyAcceptanceInput("privacy", ActiveVersion),
            new PolicyAcceptanceInput("points_tokens", ActiveVersion)
        ], CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POLICY_VERSION_INVALID");
    }

    [Fact]
    public async Task AcceptPoliciesAsync_WhenSameVersionAlreadyAccepted_DoesNotAddDuplicate()
    {
        var userId = Guid.NewGuid();
        var consents = new List<PolicyConsent>
        {
            new() { PolicyConsentId = Guid.NewGuid(), UserId = userId, PolicyType = PolicyType.Terms, PolicyVersion = ActiveVersion, AcceptedAt = DateTime.UtcNow }
        };

        SetupPolicyDocuments(BuildRequiredPolicyDocuments());
        SetupPolicyConsents(consents);

        var result = await _service.AcceptPoliciesAsync(
            userId,
            [new PolicyAcceptanceInput("terms", ActiveVersion)],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        consents.Should().HaveCount(1);
    }

    [Fact]
    public async Task AcceptPoliciesAsync_WhenNewVersionAccepted_AddsNewConsent()
    {
        var userId = Guid.NewGuid();
        var consents = new List<PolicyConsent>
        {
            new() { PolicyConsentId = Guid.NewGuid(), UserId = userId, PolicyType = PolicyType.Terms, PolicyVersion = "2026-05-01.v1", AcceptedAt = DateTime.UtcNow }
        };

        SetupPolicyDocuments(BuildRequiredPolicyDocuments());
        SetupPolicyConsents(consents);
        _contextMock.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.AcceptPoliciesAsync(
            userId,
            [new PolicyAcceptanceInput("terms", ActiveVersion)],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        consents.Should().HaveCount(2);
        consents.Should().Contain(consent => consent.PolicyType == PolicyType.Terms && consent.PolicyVersion == ActiveVersion);
    }

    [Fact]
    public async Task GetConsentStatusAsync_WhenRequiredPoliciesMissing_ReturnsMissingTypes()
    {
        var userId = Guid.NewGuid();
        SetupPolicyDocuments(BuildRequiredPolicyDocuments());
        SetupPolicyConsents(
        [
            new PolicyConsent
            {
                PolicyConsentId = Guid.NewGuid(),
                UserId = userId,
                PolicyType = PolicyType.Terms,
                PolicyVersion = ActiveVersion,
                AcceptedAt = DateTime.UtcNow
            }
        ]);

        var result = await _service.GetConsentStatusAsync(userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsUpToDate.Should().BeFalse();
        result.Value.MissingRequiredTypes.Should().BeEquivalentTo(["privacy", "points_tokens"]);
    }

    private static List<PolicyDocument> BuildRequiredPolicyDocuments()
        =>
        [
            new()
            {
                PolicyDocumentId = Guid.NewGuid(),
                Slug = "terms",
                Category = "legal",
                Audience = "all",
                PolicyType = PolicyType.Terms,
                Version = ActiveVersion,
                Title = "Terms",
                Summary = "Terms summary",
                ContentMarkdown = "terms",
                RequiresAcceptance = true,
                IsActive = true,
                EffectiveAt = DateTime.UtcNow
            },
            new()
            {
                PolicyDocumentId = Guid.NewGuid(),
                Slug = "privacy",
                Category = "privacy",
                Audience = "all",
                PolicyType = PolicyType.Privacy,
                Version = ActiveVersion,
                Title = "Privacy",
                Summary = "Privacy summary",
                ContentMarkdown = "privacy",
                RequiresAcceptance = true,
                IsActive = true,
                EffectiveAt = DateTime.UtcNow
            },
            new()
            {
                PolicyDocumentId = Guid.NewGuid(),
                Slug = "points-tokens",
                Category = "wallet",
                Audience = "all",
                PolicyType = PolicyType.PointsTokens,
                Version = ActiveVersion,
                Title = "Points Tokens",
                Summary = "Wallet summary",
                ContentMarkdown = "points tokens",
                RequiresAcceptance = true,
                IsActive = true,
                EffectiveAt = DateTime.UtcNow
            }
        ];

    private void SetupPolicyDocuments(List<PolicyDocument> documents)
    {
        _contextMock.Setup(context => context.PolicyDocuments).Returns(documents.BuildMockDbSet().Object);
    }

    private void SetupPolicyConsents(List<PolicyConsent> consents)
    {
        _contextMock.Setup(context => context.PolicyConsents).Returns(consents.BuildMockDbSet().Object);
    }
}
