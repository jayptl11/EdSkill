using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Achievements.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Admin.Commands.GenerateAchievementIconUploadUrl;

public record GenerateAchievementIconUploadUrlCommand(
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<AchievementIconUploadUrlDto>>;
