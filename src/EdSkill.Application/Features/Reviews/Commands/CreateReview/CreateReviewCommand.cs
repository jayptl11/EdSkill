using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    Guid SessionId,
    int Rating,
    string? Comment) : IRequest<Result<ReviewDto>>;
