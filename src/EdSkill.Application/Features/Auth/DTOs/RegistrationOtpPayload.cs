using EdSkill.Application.Common.Models;

namespace EdSkill.Application.Features.Auth.DTOs;

internal record RegistrationOtpPayload(
    string Username,
    string PasswordHash,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<PolicyAcceptanceInput> AcceptedPolicies
);
