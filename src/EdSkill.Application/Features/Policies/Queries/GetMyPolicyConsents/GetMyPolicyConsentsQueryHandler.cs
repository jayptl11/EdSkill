using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using EdSkill.Application.Features.Policies.DTOs;
using MediatR;

namespace EdSkill.Application.Features.Policies.Queries.GetMyPolicyConsents;

public class GetMyPolicyConsentsQueryHandler : IRequestHandler<GetMyPolicyConsentsQuery, Result<PolicyConsentStatusDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPolicyConsentService _policyConsentService;

    public GetMyPolicyConsentsQueryHandler(
        ICurrentUserService currentUserService,
        IPolicyConsentService policyConsentService)
    {
        _currentUserService = currentUserService;
        _policyConsentService = policyConsentService;
    }

    public Task<Result<PolicyConsentStatusDto>> Handle(GetMyPolicyConsentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        return _policyConsentService.GetConsentStatusAsync(userId, cancellationToken);
    }
}
