using EdSkill.Application.Common.Interfaces;
using EdSkill.Application.Common.Models;
using MediatR;

namespace EdSkill.Application.Features.Policies.Commands.AcceptMyPolicies;

public class AcceptMyPoliciesCommandHandler : IRequestHandler<AcceptMyPoliciesCommand, Result>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPolicyConsentService _policyConsentService;

    public AcceptMyPoliciesCommandHandler(
        ICurrentUserService currentUserService,
        IPolicyConsentService policyConsentService)
    {
        _currentUserService = currentUserService;
        _policyConsentService = policyConsentService;
    }

    public Task<Result> Handle(AcceptMyPoliciesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        return _policyConsentService.AcceptPoliciesAsync(userId, request.AcceptedPolicies, cancellationToken);
    }
}
