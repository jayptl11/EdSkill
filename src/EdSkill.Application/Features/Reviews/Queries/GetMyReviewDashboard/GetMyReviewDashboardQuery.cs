using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Reviews.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Reviews.Queries.GetMyReviewDashboard;

public record GetMyReviewDashboardQuery : IRequest<Result<ReviewDashboardDto>>;
