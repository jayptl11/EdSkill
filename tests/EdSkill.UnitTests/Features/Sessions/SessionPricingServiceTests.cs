using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Services;
using EdSkill.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EdSkill.UnitTests.Features.Sessions;

public class SessionPricingServiceTests
{
    [Theory]
    [InlineData(30, 60, 60, 75, 169)]
    [InlineData(45, 75, 75, 150, 282)]
    [InlineData(60, 100, 100, 250, 438)]
    [InlineData(90, 140, 140, 0, 175)]
    [InlineData(120, 180, 180, 150, 413)]
    public void BuildBookingSnapshot_WhenDurationSupported_ComputesExpectedValues(
        int duration,
        int expectedMultiplierPercent,
        int expectedBaseDurationValue,
        int expectedCredentialBonus,
        int expectedLearnerCharge)
    {
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        var service = new SessionPricingService(systemConfigServiceMock.Object);
        var skill = new Skill { SkillId = Guid.NewGuid(), Name = "Speaking", Slug = "speaking", BasePointCost = 100, IsActive = true };
        var credentialCount = expectedCredentialBonus switch
        {
            0 => 0,
            75 => 1,
            150 => 2,
            _ => 3
        };

        var result = service.BuildBookingSnapshot(skill, credentialCount, duration, 25);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DurationMultiplierPercentSnapshot.Should().Be(expectedMultiplierPercent);
        result.Value.CredentialBonusPointsSnapshot.Should().Be(expectedCredentialBonus);
        result.Value.CompanionPayoutPoints.Should().Be(expectedBaseDurationValue + expectedCredentialBonus);
        result.Value.LearnerChargePoints.Should().Be(expectedLearnerCharge);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 75)]
    [InlineData(2, 150)]
    [InlineData(3, 250)]
    [InlineData(5, 250)]
    public void GetCredentialBonus_WhenCredentialCountVaries_ReturnsExpectedTier(int credentialCount, int expectedBonus)
    {
        SessionPricingService.GetCredentialBonus(credentialCount).Should().Be(expectedBonus);
    }

    [Fact]
    public void BuildOfferPreview_WhenDurationOptionsMixed_NormalizesAndBuildsRange()
    {
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        var service = new SessionPricingService(systemConfigServiceMock.Object);
        var skill = new Skill { SkillId = Guid.NewGuid(), Name = "Speaking", Slug = "speaking", BasePointCost = 100, IsActive = true };

        var result = service.BuildOfferPreview(skill, 1, new[] { 120, 45, 45, 15 }, 25);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MinCompanionPayoutPoints.Should().Be(135);
        result.Value.MaxCompanionPayoutPoints.Should().Be(255);
        result.Value.MinLearnerChargePoints.Should().Be(169);
        result.Value.MaxLearnerChargePoints.Should().Be(319);
        result.Value.MinPlatformFeePoints.Should().Be(34);
        result.Value.MaxPlatformFeePoints.Should().Be(64);
    }

    [Theory]
    [InlineData(new[] { 30 }, new[] { 30 })]
    [InlineData(new[] { 45 }, new[] { 30, 45 })]
    [InlineData(new[] { 60 }, new[] { 30, 45, 60 })]
    [InlineData(new[] { 90 }, new[] { 30, 45, 60, 90 })]
    [InlineData(new[] { 120 }, new[] { 30, 45, 60, 90, 120 })]
    [InlineData(new[] { 45, 120 }, new[] { 30, 45, 60, 90, 120 })]
    public void NormalizeDurations_WhenMaxDurationIsProvided_ExpandsToAllSupportedLowerDurations(
        int[] requestDurations,
        int[] expectedDurations)
    {
        var normalizedDurations = SessionPricingService.NormalizeDurations(requestDurations);

        normalizedDurations.Should().BeEquivalentTo(expectedDurations, options => options.WithStrictOrdering());
    }

    [Fact]
    public void BuildBookingSnapshot_WhenMarkupProducesFraction_RoundsLearnerChargeUp()
    {
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        var service = new SessionPricingService(systemConfigServiceMock.Object);
        var skill = new Skill { SkillId = Guid.NewGuid(), Name = "Design", Slug = "design", BasePointCost = 101, IsActive = true };

        var result = service.BuildBookingSnapshot(skill, 0, 30, 25);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanionPayoutPoints.Should().Be(61);
        result.Value.LearnerChargePoints.Should().Be(77);
        result.Value.PlatformFeePoints.Should().Be(16);
    }

    [Fact]
    public void BuildDurationPricingOptions_WhenFormulaSessionIsAvailable_ReturnsExactPointsPerDuration()
    {
        var systemConfigServiceMock = new Mock<ISystemConfigService>();
        var service = new SessionPricingService(systemConfigServiceMock.Object);
        var skill = new Skill { SkillId = Guid.NewGuid(), Name = "Speaking", Slug = "speaking", BasePointCost = 100, IsActive = true };

        var result = service.BuildDurationPricingOptions(skill, 1, new[] { 60 }, 25);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value!.Select(item => item.DurationMinutes).Should().BeEquivalentTo(new[] { 30, 45, 60 }, options => options.WithStrictOrdering());
        result.Value.Select(item => item.LearnerChargePoints).Should().BeEquivalentTo(new[] { 169, 188, 219 }, options => options.WithStrictOrdering());
    }
}
