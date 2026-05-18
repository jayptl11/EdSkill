using EdSkill.Application.Features.MySpace.DTOs;
using EdSkill.Domain.Entities;

namespace EdSkill.Application.Features.MySpace;

internal static class MySpaceDtoMapper
{
    public static MySpaceDto Map(
        IReadOnlyCollection<CompanionSpaceCard> companionCards,
        IReadOnlyCollection<LearnerSpaceCard> learnerCards)
    {
        return new MySpaceDto(
            companionCards.Select(MapCompanionCard).ToList(),
            learnerCards.Select(MapLearnerCard).ToList());
    }

    public static CompanionSpaceCardDto MapCompanionCard(CompanionSpaceCard card)
    {
        return new CompanionSpaceCardDto(
            card.CompanionSpaceCardId,
            new MySpaceSkillDto(card.SkillId, card.Skill.Name, card.Skill.IconKey),
            card.Title,
            card.Description,
            card.PricePoints,
            card.DurationMinutes,
            card.DeliveryModes.AsReadOnly(),
            card.Languages.AsReadOnly(),
            card.CoverImageUrl,
            card.CredentialUrls.AsReadOnly(),
            card.IsPublished,
            card.CreatedAt,
            card.UpdatedAt);
    }

    public static LearnerSpaceCardDto MapLearnerCard(LearnerSpaceCard card)
    {
        return new LearnerSpaceCardDto(
            card.LearnerSpaceCardId,
            new MySpaceSkillDto(card.SkillId, card.Skill.Name, card.Skill.IconKey),
            card.Title,
            card.Description,
            card.TargetPoints,
            card.DurationMinutes,
            card.DeliveryModes.AsReadOnly(),
            card.Languages.AsReadOnly(),
            card.CoverImageUrl,
            card.IsPublished,
            card.CreatedAt,
            card.UpdatedAt);
    }
}
